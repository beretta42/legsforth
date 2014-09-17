#include <stdio.h>
#include <stdlib.h>
#include <termios.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <netdb.h>
#include <fcntl.h>
#include <signal.h>
#include <sys/time.h>

#define CELL 2           // width of cell in bytes
#define MEMZ 0x8000       // Memory Store size
#define RZ      0x80     // Return stack size
#define RVECT   0*CELL  
#define IVECT   1*CELL
#define C0      2*CELL
#define DH      3*CELL

typedef unsigned short int cell ;
typedef void prim() ;

unsigned char m[MEMZ];   // The Memory Store

cell ip=0;          // The Instruction Pointer
cell rp=MEMZ ;      // The Return Stack Pointer
cell sp=MEMZ-RZ ;   // The Data Stack Pointer
cell t=0 ;          // top popped working reg
cell n=0 ;          // next on stack working reg

prim *p[256];       // primitive function pointer table

struct termios tsave;  // saved terminal settings

int debugf=0;       // debug flag
int dlevel=0;       // debug level
int savek=0;        // save kernel upon exit flag
int cpsave=0;       // save image only to C0

int sock=0;         // DW communication socket
int sockf=0;        // if socket is connected
unsigned char beck_buff=0;  // a one byte read buffer for becker port
int beck_flag=0;            // a flag if the the buffer is ready

volatile int timer=0;       // the timer interrupt flag
int ienable=0;              // timer interrupt enable flag

// The timer signal handler
void thandler( int sig ){
  timer=1;
}

void install_timer(){
  struct itimerval t;
  signal( SIGALRM, thandler );
  t.it_interval.tv_sec=0;
  t.it_interval.tv_usec=16666;
  t.it_value.tv_sec=0;
  t.it_value.tv_usec=16666;
  setitimer( ITIMER_REAL, &t, NULL);
}

// Fetches a Cell from Memory Store
cell mfetch( cell addr ){
  // fprintf(stderr,"fetch addr: %x = %x\n", addr, (cell)m[addr]<<8+m[addr+1] );
  return m[addr]*256 + m[addr+1] ;
}

// Stores a Cell to Memory Store
void mstore( cell data, cell addr ){
  m[addr]=data >> 8;
  m[addr+1]=data & 0xff ;
}

// Drop cell off stack
void drop(){
  sp=sp+CELL;
}

// Pops data off the data stack
cell mpop(){
  cell t=mfetch( sp );
  drop();
  return t;
}

// Pops 2 cells off data stack
void m2pop(){
  t=mpop();
  n=mpop();
}

// Pushes data onto the data stack
void mpush( cell data ){
  sp=sp-CELL;
  mstore( data, sp );
}

// quit the system
void quit( int ret ){
  tcsetattr( 0, TCSANOW, &tsave );
  FILE *f=NULL;
  int x;
  if( savek ){
    // save the memory store
    f=fopen("forth.img","w");
    if( !f ){
      fprintf(stderr," Error saving memory image.\n" );
      exit( -1);
    }
    x=MEMZ;
    if( cpsave ) x=mfetch( C0 ) ;
    fwrite( m, 1, x, f );
    fclose( f );
  }
  exit( ret);
}

//
//  VM Primitives
//

void push(){
  rp=rp-CELL;
  mstore(mpop(),rp);
}

void pull(){
  mpush(mfetch(rp));
  rp=rp+CELL;
}


void dup(){
  cell t=mpop();
  mpush(t);
  mpush(t);
}

void swap(){
  m2pop();
  mpush( t );
  mpush( n );
}

void over(){
  push();
  dup();
  pull();
  swap();
}

void bra(){
  ip=mfetch( ip );
}

void zbra(){
  if( mpop() ) ip=ip+CELL;
  else bra();
}

void dofor(){
  if( mfetch(rp) ){
    mstore( mfetch(rp)-1, rp );
    ip=ip+CELL;
    return;
  }
  rp=rp+CELL;
  bra();
}

void fexit(){
  ip=mfetch(rp);
  rp=rp+CELL;
}

void mint(){
  mpush( 0x8000 );
}

void and(){
  m2pop();
  mpush( t & n );
}

void or(){
  m2pop();
  mpush( t | n );
}

void xor(){
  m2pop();
  mpush( t ^ n );
}

void com(){
  mpush( ~mpop() );
}

void plus(){
  m2pop();
  mpush( t + n );
}

void shl(){
  mpush( mpop()<<1 );
}

void shr(){
  mpush( mpop()>>1 );
}

void oneplus(){
  mpush( mpop()+1 );
}

void oneminus(){
  mpush( mpop()-1 );
}

void spat(){
  mpush(sp);
}

void spbang(){
  sp=mpop();
}

void rpat(){
  mpush(rp);
}

void rpbang(){
  rp=mpop();
}

// this doesn't belong here 
void pexec( cell xt ){
  if( xt<256 ) p[xt]();
  else{
    mpush( ip );
    push();
    ip=xt;
  }
}

void exec(){
  pexec( mpop() );
}

void at(){
  mpush( mfetch( mpop() ) );
}

void bang(){
  m2pop();
  mstore( n, t );
}

void cat(){
  mpush( m[mpop()] );
}

void cbang(){
  m2pop();
  m[t]=n&255;
}

void fcell(){
  mpush( CELL );
}

void fchar(){
  mpush( 1 );
}

void lit(){
  mpush( mfetch( ip ) );
  ip+=CELL;
}

void key(){
  int c=getchar();
  if( c<0 ) quit(0);
  if( c=='\n' ) c='\r' ;
  if( c==0x7f ) c=0x8 ;
  mpush( c );
}

void emit(){
  char c=mpop();
  if( c=='\r' ) c='\n' ;
  putchar( c );
}

void bye(){
  quit(0);
}

void memz(){
  mpush(MEMZ);
}

void noop(){
  printf("noop!\n" );
  printf("@ip: 0x%x\n", ip-CELL );
  quit(-1);
}

void pat(){
  unsigned int a=mpop();
  int ret;
  if( a==0xff41 ){
    if( beck_flag ){ mpush(1); return; }
    if( read( sock, &beck_buff, 1 )==1 ){
      mpush(1);
      beck_flag=1;
      return;
    }
    mpush(0);
    return;
  }
  if( a==0xff42 ){
    if( beck_flag ){
      mpush( beck_buff );
      beck_flag=0;
      return;
    }
  }
  mpush(0);
}

void pbang(){
  unsigned int a;
  unsigned char d;
  a=mpop();
  d=mpop()&0xff;
  if( a==0xff42 && sockf ) write( sock, &d, 1 );
}

void ion(){
  ienable=1;
}

void ioff(){
  ienable=0;
}

void iwait(){
  select( 0, NULL, NULL, NULL, NULL );
}

void keyq(){
  char c=0;
  int r=0;
  fcntl( 0, F_SETFL, O_NONBLOCK );
  r=read( 0, &c, 1 );
  printf( "%d ", r );
  if (r<0) mpush(-1);
  else{
    if( c=='\n' ) c='\r' ;
    if( c==0x7f ) c=0x8 ;
    mpush( c );
  }
}

void debugon(){
  debugf=~debugf;
  dlevel=rp;
}

void init_socket(){
  int ret;
  struct sockaddr_in addr;
  struct hostent *server;
  sock=socket( AF_INET, SOCK_STREAM, 0 );
  if (sock<0){
    fprintf(stderr, "Cannot create socket\n" );
    return;
  }
  server=gethostbyname("localhost");
  if (server == NULL ){
    fprintf(stderr, "Cannot find localhost?\n" );
    return;
  }
  bzero((char *) &addr, sizeof(addr) );
  addr.sin_family = AF_INET;
  bcopy((char *)server->h_addr, 
	(char *)&addr.sin_addr.s_addr,
	server->h_length);
  addr.sin_port = htons(65504);
  ret=connect( sock, (struct sockaddr *)&addr, sizeof(addr));
  if( ret<0 ){
    sockf=0;
    fprintf(stderr, "Cannot connect to DW server\n" );
    return;
  }
  else sockf=1;
  fprintf(stderr, "Connected to DW server\n");
  fcntl( sock, F_SETFL, O_NONBLOCK );
}

void init(){
  int x;
  FILE *f;
  struct termios t;
  // put terminal into raw mode
  tcgetattr( 0, &t );
  tcgetattr( 0, &tsave );
  t.c_lflag=t.c_lflag&~ECHO;
  t.c_lflag=t.c_lflag&~ICANON;
  tcsetattr( 0, TCSANOW, &t );
  // and turn stdin FILE to unbuffered
  setvbuf( stdin, NULL, _IONBF, 0 );
  init_socket();
  // clear memory store
  for( x=0; x<MEMZ; x++){
    m[x]=0;
  }
  // load the memory store
  f=fopen("forth.img","r");
  if( !f ){
    fprintf(stderr,"Fatal error loading memory image.\n" );
    quit(-1);
  }
  fread( m, 1, MEMZ, f );
  fclose( f );
  // setup the primitive function table 
  for( x=0; x<256; x++) p[x]=noop;
  p[1]=push;        // 1
  p[2]=pull;
  p[3]=drop;
  p[4]=dup;
  p[5]=swap;
  p[6]=over;
  p[7]=bra;
  p[8]=zbra;        // 8
  p[9]=dofor;
  p[10]=fexit;
  p[11]=mint;
  p[12]=and;
  p[13]=or;
  p[14]=xor;
  p[15]=com;
  p[16]=plus;       // 10
  p[17]=shl;
  p[18]=shr;
  p[19]=oneplus;
  p[20]=oneminus;
  p[21]=spat;
  p[22]=spbang;
  p[23]=rpat;
  p[24]=rpbang;     // 18
  p[25]=exec;
  p[26]=at;
  p[27]=bang;
  p[28]=cat;
  p[29]=cbang;
  p[30]=fcell;
  p[31]=fchar;
  p[32]=lit;        // 20
  p[33]=key;
  p[34]=emit;
  p[35]=bye;
  p[36]=memz;
  p[37]=pat;
  p[38]=pbang;      // 26
  p[39]=ion;
  p[40]=ioff;
  p[41]=iwait;
  p[42]=keyq;
  p[255]=debugon;   // ff
  ip=mfetch( RVECT );
}

void rfind(cell xt){
  cell x=mfetch( DH );
  while(1){
    if (!x){ 
      fprintf(stderr,"???\n" );
      return;
    }
    if (mfetch(x+2)==xt){
      cell i;
      for( i=x+8; i<x+8+mfetch(x+6); i++ ){
	fprintf(stderr, "%c", m[i] );
      }
      fprintf(stderr, "\n" );
      return;
    }
    x=mfetch(x);
  }
}

void debug(){
  int x;
  fprintf(stderr,"ip: %x next: %x  ", ip, mfetch(ip) );
  rfind( mfetch(ip) );
  fprintf(stderr,"data %x: ", sp );
  for( x=MEMZ-RZ-CELL; x>=sp; x=x-CELL ){
    fprintf( stderr,"%x ", mfetch(x) );
  }
  fprintf( stderr, "\n" );
  fprintf(stderr,"ret: " );
  for( x=MEMZ-CELL; x>=rp; x=x-CELL ){
    fprintf( stderr,"%x ", mfetch(x) );
  }
  fprintf( stderr, "\n>" );
  x=getchar();
  if( x=='i' ) dlevel--;
  if( x=='o' ) dlevel++;
  if( x=='s' ) debugf=~debugf;
  if( x=='q' ) quit(0);
  if( x=='m' ){
    int i;
    int x;
    fscanf( stdin, "%x", &x );
    for( i=0; i<8; i++ ){
      fprintf(stderr,"%2x ", m[ x+i ] );
    }
    fprintf(stderr,"\n");
  }
}

void interrupt(){
  pexec( mfetch( IVECT ) );
  timer=0;
}


void loop(){
  cell t;
  while(1){
    if (debugf && rp==dlevel) debug();
    if( timer && ienable ) interrupt();
    t=mfetch(ip);
    ip+=CELL;
    pexec(t);
  }
}



int main( int argc, char *argv[] ){
  int x,i;
  for( x=1; x<argc; x++ ){
    if( argv[x][0]=='-' ){
      for( i=1; i<strlen(argv[x]); i++ ){
	switch( argv[x][i] ){
	case 's':
	  savek=1;
	  break;
	case 'c':
	  cpsave=1;
	  break;
	default:
	  fprintf(stderr, "Error: bad command line switch.\n" );
	  exit(-1);
	}
      }
    }
  }
  init();
  install_timer();
  loop();
}
