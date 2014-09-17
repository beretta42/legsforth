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
#include <unistd.h>

#define CELL 2            // width of cell in bytes
#define MEMZ 0x80000      // Memory Store size
#define MSPL 0x8000       // User - System split (initial stacks)
#define RZ      0x80      // Return stack size
// lowest memory stores initial state 
#define RVECT   0*CELL    // 1 cell - saved IP, or reset vector  
#define IVECT   1*CELL    // 1 cell - timer interrupt vector
#define TP0     2*CELL    // 1 cell - Init task pointer
#define IP      3*CELL    // 1 cell - IP instruction pointer
#define RP      4*CELL    // 1 cell - RP stack pointer
#define SP      5*CELL    // 1 cell - SP stack pointer
#define EX      6*CELL    // 1 char - EXtra processor state 
#define MMU     13        // 4 cell - MMU mirror
#define NEXT    21        // 1 cell - next task structure ptr
#define C0      23        // 1 cell - Compile pointer (sbrk) pointer
#define DH      25        // 1 cell - Dictionary Head pointer to dictionary


void noop();


typedef unsigned short int cell ;
typedef void prim() ;

void rfind(cell xt);

unsigned char m[MEMZ];   // The Memory Store

cell ip=0;          // The Instruction Pointer
cell rp=MSPL ;      // The Return Stack Pointer
cell sp=MSPL-RZ ;   // The Data Stack Pointer
// cell tp=IP ;        // The Task Pointer
cell tp=6 ;
cell t=0 ;          // top popped working reg
cell n=0 ;          // next on stack working reg

unsigned char mmu[8];  // holds mmu values


prim *p[256];       // primitive function pointer table

struct termios tsave;  // saved terminal settings

volatile int debugf=0;       // debug flag
int sdebug=0;
int dlevel=0;       // debug level
int savek=0;        // save kernel upon exit flag
int cpsave=0;       // save image only to C0
int noemit=0;       // control echo'ing

int sock=0;         // DW communication socket
int sockf=0;        // if socket is connected
unsigned char beck_buff=0;  // a one byte read buffer for becker port
int beck_flag=0;            // a flag if the the buffer is ready

volatile int timer=0;       // the timer interrupt flag
int ienable=0;              // timer interrupt enable flag

char defImage[]="/.legs/forth.img" ;

char inImgFile[1024];
char outImgFile[1024];

// print state of machine
void pstate( ){
  int x;
  fprintf( stderr, "IP = %x in ", ip);
  rfind( ip ); fprintf( stderr, "\n" );
  fprintf( stderr, "RP = %x\n", rp );
  fprintf( stderr, "SP = %x\n", sp );
  fprintf( stderr, "TP = %x\n", tp );
  fprintf( stderr, "IE = %x\n", ienable );
  fprintf( stderr, "TI = %x\n", timer );
  fprintf( stderr, "MMU = " );
  for( x=0; x<8; x++ ) fprintf( stderr, "%x ", mmu[x] ) ;
  fprintf( stderr, "\n" );
}

// The timer signal handler
void thandler( int sig ){
  timer=1;
}

// The debug break handler
void dhandler( int sig ){
  debugf=1;
  dlevel=rp;
}

// Installs system timer for 1/60th of sec, repeating
void install_timer(){
  struct itimerval t;
  signal( SIGALRM, thandler );
  t.it_interval.tv_sec=0;
  t.it_interval.tv_usec=16666;
  t.it_value.tv_sec=0;
  t.it_value.tv_usec=16666;
  setitimer( ITIMER_REAL, &t, NULL);
}

// Installs interrupt for debuging
void install_debug(){
  signal( SIGQUIT, dhandler);
}


// Converts a CPU address to real address via mmu registers
int tommu( cell addr){
  int o=addr & 0x1fff ;
  int b=addr >> 13 ;
//fprintf( stderr, "fetch addr: %x = %x:%x\n", addr, (int)mmu[b]*0x2000,o );
  return( (int)mmu[b]*0x2000 + o ) ;
}

// Fetches a Cell from Memory Store
cell mfetch( cell addr ){
  // fprintf(stderr,"fetch addr: %x = %x\n", addr, (cell)m[addr]<<8+m[addr+1] );
  return m[tommu(addr)]*256 + m[tommu(addr+1)] ;
}

// Fetches a byte from Memory Store
unsigned char mcfetch( cell addr ){
  return m[tommu(addr)] ;
}


// Stores a Cell to Memory Store
void mstore( cell data, cell addr ){
  m[tommu(addr)]=data >> 8;
  m[tommu(addr+1)]=data & 0xff ;
}

// Store a Char to memory store
void mcstore( cell data, cell addr ){
  m[tommu(addr)]=data & 0xff ;
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

// Saves state of system
void save( ){
  FILE *f=NULL;
  int x;
  mstore( ip, tp+0 );
  //  mstore( ip, RVECT );
  mstore( rp, tp+2 );
  mstore( sp, tp+4 );
  mcstore( ienable, tp+6 );
  mstore( tp, TP0 );
  // save the memory store
  f=fopen(outImgFile,"w");
  if( !f ){
    fprintf(stderr," Error saving memory image.\n" );
    exit( -1);
  }
  x=MEMZ;
  if( cpsave ) x=mfetch( C0 ) ;
  fwrite( m, 1, x, f );
  fclose( f );
}

// Set's the task pointer, loads new task  ( a -- )
void tpstore(){
  int x;
  cell tmp=mpop();
  // Save state of current task
  //  mstore( ip, tp );
  //  mstore( rp, tp+2 );
  //  mstore( sp, tp+4 );
  //  mcstore( ienable, tp+6 );
  // Restore state of new task
  tp=tmp;
  ip=mfetch( tp );
  rp=mfetch( tp+2 );
  sp=mfetch( tp+4 );
  ienable=mcfetch( tp+6 );
  for( x=0; x<8; x++ ) mmu[x]=mcfetch( tp+7+x ) ;
}

void yieldto(){
  int x;
  cell addr=mpop();
  // Save state of current task
  mstore( ip, tp );
  mstore( rp, tp+2 );
  mstore( sp, tp+4 );
  mcstore( ienable, tp+6 );
  // Restore state of new task
  tp=addr;
  ip=mfetch( tp );
  rp=mfetch( tp+2 );
  sp=mfetch( tp+4 );
  ienable=mcfetch( tp+6 );
  for( x=0; x<8; x++ ) mmu[x]=mcfetch( tp+7+x ) ;
}

// fetches the task pointer ( -- a )
void tpfetch(){
  mpush(tp);
}


// quit the system
void quit( int ret ){
  tcsetattr( 0, TCSANOW, &tsave );
  if( savek ) save();
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


void fdup(){
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
  fdup();
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
  mpush( mcfetch(mpop()) );
}

void cbang(){
  m2pop();
  m[tommu(t)]=n&255;
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
  if( !noemit ) write( 1, &c, 1);
}

void bye(){
  quit(0);
}

void memz(){
  mpush(MSPL);
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
  struct timeval t;
  fd_set r;
  fd_set w;
  int ret;
  FD_ZERO( &r );
  FD_SET( 0, &r);
  FD_ZERO( &r );
  FD_SET( 1, &w);
  t.tv_sec=0;
  t.tv_usec=8000;
  // ret=select( 2, &r, NULL, NULL, &t );
  ret=select( 2, &r, NULL, NULL, NULL );
  
}

void keyq(){
  char c=0;
  int r=0;
  int temp= fcntl( 0, F_GETFL, NULL );
  fcntl( 0, F_SETFL, O_NONBLOCK );
  r=read( 0, &c, 1 );
  fcntl( 0, F_SETFL, temp );
  if (r<1) mpush(0xffff);
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
  for( x=0; x<MEMZ; x++) m[x]=0;
  // load the memory store
  f=fopen(inImgFile,"r");
  if( !f ){
    fprintf(stderr,"Fatal error loading memory image: %s\n", defImage );
    quit(-1);
  }
  fread( m, 1, MEMZ, f );
  fclose( f );
  // set the default MMU map
  for( x=0; x<8; x++ ) mmu[x]=x;
  // setup the primitive function table 
  for( x=0; x<256; x++) p[x]=noop;
  p[1]=push;        // 1
  p[2]=pull;
  p[3]=drop;
  p[4]=fdup;
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
  // end of basics...
  p[43]=save;
  p[44]=tpstore;
  p[45]=tpfetch;
  p[46]=yieldto;
  p[255]=debugon;   // ff

  {
  tp=mfetch( TP0 );
  ip=mfetch( tp+0 ); // load IP
  rp=mfetch( tp+2 );   // load RP
  sp=mfetch( tp+4 );   // load RP
  ienable=mcfetch( tp+6 ); // load EX
  // if the reset vector is not clear then do it
  // instead of tp's ip!
  if( mfetch( RVECT ) ) ip=mfetch( RVECT );
  }
}

void rfind(cell xt){
  cell x=mfetch( DH );
  while(1){
    if (!x){ 
      fprintf(stderr,"???\n" );
      return;
    }
    if ( xt<0x100 && mfetch(x+2)==xt){
      cell i;
      for( i=x+8; i<x+8+mfetch(x+6); i++ ){
	fprintf( stderr, "%c", m[i] );
      }
      fprintf( stderr, " " );
      return;
    }
    if (mfetch(x+2)<=xt){
      cell i;
      for( i=x+8; i<x+8+mfetch(x+6); i++ ){
	fprintf(stderr, "%c", m[i] );
      }
      fprintf( stderr, " " );
      return;
    }
    x=mfetch(x);
  }
}


void debug(){
  int x;
  fprintf( stderr, "\n" );
  pstate();
  fprintf(stderr,"next: %x  ", mfetch(ip) );
  rfind( mfetch(ip) );
  fprintf( stderr, "\n" );
  fprintf(stderr,"sp: " );
  for( x=sp; x<sp+16; x=x+CELL ) fprintf( stderr,"%x ", mfetch(x) );
  fprintf(stderr,"\nrp: " );
  for( x=rp; x<rp+16; x=x+CELL ) fprintf( stderr,"%x ", mfetch(x) );
  fprintf(stderr,"| " );
  for( x=rp; x<rp+16; x=x+CELL ) rfind( mfetch(x) ) ;
  //}
  fprintf( stderr, "\ndebug>" );
  x=getchar();
  if( x=='i' ) dlevel=dlevel-2;
  if( x=='o' ) dlevel=dlevel+2;
  if( x=='s' ) debugf=~debugf;
  if( x=='q' ) quit(0);
  if( x=='m' ){
    int i;
    int x;
    fscanf( stdin, "%x", &x );
    for( i=0; i<8; i++ ){
      fprintf(stderr,"%2x ", m[ tommu(x+i) ] );
    }
    fprintf(stderr,"\n");
  }
}

void noop(){
  printf("noop!\n" );
  debug();
  quit(-1);
}



void interrupt(){
  // Save state of current task
  mstore( ip, tp );
  mstore( rp, tp+2 );
  mstore( sp, tp+4 );
  mcstore( ienable, tp+6 );
  pexec( mfetch( IVECT ) );
  timer=0;
  ienable=0;
}



void loop(){
  cell t;
  while(1){
    if ( debugf && rp==dlevel || sdebug ) debug();
    if( timer && ienable ) interrupt();
    t=mfetch(ip);
    ip+=CELL;
    pexec(t);
  }
}



int main( int argc, char *argv[] ){
  int s;

  strncat( inImgFile, getenv( "HOME" ), 512 );
  strncat( inImgFile, defImage, 1024 );
  strncat( outImgFile, getenv( "HOME" ), 512 );
  strncat( outImgFile, defImage, 1024 );

  
  while ( (s=getopt( argc, argv, "scqdi:" ))>=0  ){
    switch( s ){
    case 'i':
      strncpy ( inImgFile, optarg, 1024 );
      strncpy ( outImgFile, optarg, 1024 );
    case 's':
      savek=1;
      break;
    case 'c':
      cpsave=1;
      break;
    case 'q':
      noemit=1;
      break;
    case 'd':
      sdebug=1;
      break;
    default:
      fprintf(stderr, "Error: bad command line switch.\n" );
      exit(-1);
    }
  }
  init();
  install_timer();
  install_debug();
  loop();
}
