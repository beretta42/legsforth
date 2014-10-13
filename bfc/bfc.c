/*
  This is a C based forth compiler for
  the Legs VM

  Sorry for the lack of function declarations.  Years of forth
  programing has beaten a bottom-up, no forward references, type
  of programming, which renders C prototyping of file-scoped 
  functions silly and useless.
*/

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define MEMZ    0x80000    // default target memory size
#define WORDZ   256       // word buffer size 
#define CNTLZ   50        // size of flow control stack
#define EOL     10        // host's end of line charactor
#define SPACE   32        // host's space charactor
#define CELL    2         // width in bytes of a cell
#define CHAR    1         // width in bytes of a char


// lowest memory stores initial state
#define RVECT   0*CELL    // 1 cell - saved IP, or reset vector
#define IVECT   1*CELL    // 1 cell - timer interrupt vector
#define TP0     2*CELL    // 1 cell - Init task pointer
#define IP      3*CELL    // 1 cell - IP instruction pointer
#define RP      4*CELL    // 1 cell - RP stack pointer
#define SP      5*CELL    // 1 cell - SP stack pointer
#define EX      6*CELL    // 1 char - EX ( extra state )
#define MMU     13        // 4 cell - MMU mirror
#define NEXT    21        // 1 cell - next task structure ptr
#define C0      23        // 1 cell - Compile pointer (sbrk) pointer
#define DH      25        // 1 cell - Dictionary Head pointer to dictionary


// Offsets into Image when orgin is set
#define I_XT    0*CELL    // offset to image's execution vector
#define I_C0    1*CELL    // Image's saved Compile Pointer
#define I_DH    2*CELL    // Image's saved Dictionary Head

/* NOTE:
   Whitespace is considered any charactor code less than SPACE.
   This works well for ASCII-based systems. Others might have problems
*/

#define IMMEDIATE     1   // Marks word as immediate
#define HIDDEN        2   // Marks word as hidden
#define CONSTANT      4   // Marks word as a constant

/* Words marked "immediate" have no meaning in this compiler. 
   However, the flag does get compiled into the target's dictionary
   where it might have a meaning.  Usually a VM-native forth 
   compiler will use this flag to force a word to execute immediately
   at compile-time rather then just compile the word.

   Words marked "hidden" will not get a dictionary entry in the target's
   dictionary.  They will in the host's dictionary however, but
   subject to redefining later.
*/


// The Host's dictionary structure
struct de {
  struct de *next;      // Link field to next entry
  char name[WORDZ];     // name of word
  int xt;               // xt/address of word
  int flags;            // flag of word 
  int value;            // if compiler constant this is it's value
};

// Input source structure
struct in_source {
  int lineno;            // source's line no
  FILE *in;              // source's FILE
  char inname[WORDZ];    // source's text filename 
};


struct in_source in_stack[8];  // source stack space
int ip=0;                       // source stack pointer

int stack[8];               //  immediate mode stack space
int sp=0;                   //  immediate mode stack pointer (index)

char wordb[WORDZ];          //  The word buffer
struct de *dict=NULL;       //  pointer to the head of the dictionary 
int cp=256;                 //  Code compilation pointer
int dp=0;                   //  Dictionary compilation pointer
int dh=0;                   //  latest defined header
unsigned char mem[MEMZ];    //  The targets memory
int cntl[CNTLZ];            //  the flow control stack
int cntlp=CNTLZ;            //  the flow control pointer
char *out="a.out";          //  the output filename
int cpstop=0;               //  stop memory write after CP flag
int cpstart=0;              //  only write bytes from last setorg, overlays
int nodict=0;               //  flag for no dictionary setting
int orgin=0;                //  start address of image
int state=0;                //  state of the compiler, 1 in on, 0 is off
int mask=0;                 //  initial value of new def's flags

/*  The Compilation pointer is initialized to 256 -
    anything address less is considered a "primitive" in 
    Legs Forth's VM
*/


// store a byte in target's memory store
void mcstore( char data, int addr ){
  mem[addr]=data&255;
}

// store data in target's memory store
void mstore( int data, int addr ){
  mem[addr]=(data>>8)&255;
  mem[addr+1]=data&255;
}

// compile byte data to target's memory store
void compchar( char data ){
  mcstore( data, cp );
  cp=cp+CHAR;
}

// compile string to target's memory store
void compstr( char *str){
  char x=*str++;
  while( x ){
    compchar( x );
    x=*str++;
  };
}

// Compile cell data to target's memory store
void compnum( int data ){
  mstore( data, cp );
  cp=cp+CELL;
}

// recursive form to put the list in proper order
void mkdict4( struct de *d ){
  int temp;
  if( d==NULL ){
    return;
  }
  mkdict4( d->next );
  if( d->flags & HIDDEN ) return;
  temp=cp;
  compnum( dh );
  compnum( d->xt );
  compnum( d->flags );
  compnum( strlen( d->name) );
  compstr( d->name );
  mstore( temp, DH );
  dh=temp;
} 

void mkdict3(){
  int savedcp=cp;
  cp=dp;
  mkdict4( dict );
  cp=savedcp;
}



// compile dictionary to target's memory store 
void mkdict2(){
  struct de *d=dict;
  int ba;
  int savedcp;
  ba=DH;
  savedcp=cp;
  cp=dp;
  for ( d=dict; d; d=d->next ){
    if( d->flags & HIDDEN ) continue;
    mstore( cp, ba );               // comlete last entry's link
    ba=cp;                          // set our link address
    compnum( 0 );                   // compile link
    compnum( d->xt );               // compile xt of word
    compnum( d->flags );            // compile flags
    compnum( strlen( d->name ) );   // compile length of name
    compstr( d->name );             // compile name
  }
  cp = savedcp ;
}


// compile dictionary to target's memory store 
void mkdict(){
  struct de *d=dict;
  int ba;
  if( dp ) { mkdict3(); return; } 
  if( orgin ) ba=orgin+I_DH;
  else ba=DH;
  for ( d=dict; d; d=d->next ){
    if( d->flags & HIDDEN ) continue;
    mstore( cp, ba );               // comlete last entry's link
    ba=cp;                          // set our link address
    compnum( 0 );                   // compile link
    compnum( d->xt );               // compile xt of word
    compnum( d->flags );            // compile flags
    compnum( strlen( d->name ) );   // compile length of name
    compstr( d->name );             // compile name
  }
}


// Write target's memory to disk
void writemem(){
  FILE *f;
  int x;
  int e=MEMZ;
  f=fopen(out,"w");
  if( !f ){
    fprintf(stderr,"Error: Cannot open image file: %s\n", out );
    return;
  }
  if( cpstop ) e=cp;
  if( cpstart ) x=orgin; else x=0;
  for( ; x<e; x++ ){
    fwrite( mem+x, 1, 1, f);
  }
  fclose(f);
}


// cleans up dynamic data structures (dictionary entries)
void cleanup(){
  struct de *x=dict;
  struct de *n=NULL;
  // free dictionary storage
  while( x ){
    n=x->next;
    free(x);
    x=n;
  }
  // close all source files
  for( ; ip>=0; ip-- ){
    if( in_stack[ip].in ) fclose( in_stack[ip].in );
  }
}

// quits system
void quit( int ret ){
  int x;
  if( !ret ){
    if( !nodict) mkdict();
    if( cpstart ){
      // save latest xt in image
      mstore( dict->xt, orgin+I_XT );
      // save cp in image
      mstore( cp, orgin+I_C0 );
    }
    else{
      // save latest xt in 0 page
      mstore( dict->xt, RVECT );
      mstore( 0, IVECT );
      mstore( 0x0006, TP0 );
      mstore( dict->xt, IP );
      mstore( 0x8000, RP );
      mstore( 0x7f80, SP );
      for( x=0; x<8; x++) mcstore( x,MMU+x);
      mstore( 0, NEXT );
      mstore( cp, C0 );
    }
    writemem();
  }
  else{
    fprintf(stderr,"%s: %d: %s\n", in_stack[ip].inname, in_stack[ip].lineno, wordb );

  }
  cleanup();
  exit( ret );
}


// Gets a key from source input
int key(){
  int k=fgetc(in_stack[ip].in);
  if( k==EOL) in_stack[ip].lineno++ ;
  return( k );
}

// Gets a word from source input
int word(){
  int x=0;
  int k;
  do{
    k=key();
    if( k==EOF ) return 0 ;
  }while( k<=SPACE );
  do{
    wordb[x++]=k;
    k=key(); 
  }while( k>SPACE );
  wordb[x]=0;
  if( k==EOF) return 0; else return -1;
}

// finds a word in the target dictionary
//   returns entry or null on not found
struct de *find( char *word ){
  struct de *x=dict;
  while( x ){
    if ( ! strcmp( word, x->name ) ) return( x );
    x=x->next;
  };
  return( NULL );
}


// make a new target dictionary entry in host's dictionary
void make_entry( char *word, int xt ){
  struct de *new=malloc( sizeof( struct de ) );
  if ( !new ) { // jump to fatal error here 
    fprintf(stderr,"FATAL: cannot allocate memory for dictionary.\n" );
    quit( -1 );
  }
  new->next=dict;
  strcpy( new->name, word );
  new->xt=xt;
  new->flags=mask;
  dict=new;
}

// initialize
void init(){
  int x;
  for( x=0; x<MEMZ; x++ ) mem[x]=0;
  make_entry( "exce", 0);
  make_entry( "push", 1);
  make_entry( "pull", 2);
  make_entry( "drop", 3);
  make_entry( "dup", 4);
  make_entry( "swap", 5);
  make_entry( "over", 6);
  make_entry( "bra", 7);
  make_entry( "0bra", 8);
  make_entry( "dofor", 9);
  make_entry( "exit", 10);
  make_entry( "mint",11 );
  make_entry( "and",12 );
  make_entry( "or", 13);
  make_entry( "xor",14 );
  make_entry( "com",15 );
  make_entry( "+",16 );
  make_entry( "shl",17 );
  make_entry( "shr",18 );
  make_entry( "1+",19 );
  make_entry( "1-",20 );
  make_entry( "sp@",21 );
  make_entry( "sp!",22 );
  make_entry( "rp@",23 );
  make_entry( "rp!",24 );
  make_entry( "exec",25 );
  make_entry( "@",26 );
  make_entry( "!",27 );
  make_entry( "c@",28 );
  make_entry( "c!",29 );
  make_entry( "cell",30 );
  make_entry( "char",31 );
  make_entry( "lit", 32 );
  make_entry( "key", 33 );
  make_entry( "emit", 34 );
  make_entry( "bye", 35 );
  make_entry( "memz", 36 );
  make_entry( "p@", 37 );
  make_entry( "p!", 38 );
  make_entry( "ion", 39 );
  make_entry( "ioff", 40 );
  make_entry( "iwait", 41 );
  make_entry( "key?", 42 );
  in_stack[ip].in=stdin;
}


// exits with error if compiler is on 
void immonly(){
  if( state ){
    fprintf( stderr, "Immediate Mode Only Error: \n" );
    quit(-1);
  }
  return;
}

//
//  The compiler's immediate words are below
//


// create a dictionary entry from next word
void colon(){
  word();
  make_entry( wordb, cp );
  state=1;  // compiler on!
}

// backslash line comment
void back(){
  while( key()!=EOL );
}

// left parenthesis comment
void paren(){
  while( key()!=')' );
}

// Compile word's xt to memory
void compword( char *word ){
  struct de *x=find(word) ;
  if ( !x ){
    fprintf(stderr,"Cannot find word: %s\n", word);
    quit( -1 );
  }
  compnum( x->xt );
}

void semi(){
  compword( "exit" );
  state=0;     // compiler off!
}  


// push data onto flow control stack
void pushc( int data ){
  cntl[--cntlp]=data;
}

// pop data from flow control stack
int popc( ){
  return( cntl[cntlp++] );
}

// compile a "back address" onto control stack 
void ba(){
  pushc( cp );
  compnum( 0 );
}

// "if" - cause jump if TOS is 0
void iif(){
  compword("0bra");
  ba();
}

// "then" - resolve "if"'s jump address
void ithen(){
  mstore( cp, popc() );
}

// "begin" - mark begining of loop
void begin(){
  pushc( cp );
}

// "again" - jump to beginning of loop
void again(){
  compword("bra");
  compnum( popc() );
}

// "until" - jump to beginning of loop if TOS is 0
void until(){
  compword("0bra");
  compnum( popc() );
}

// "while" - break loop if TOS is 0
void iwhile(){
  iif();
}

// swap top two control stack items
void swap(){
  int t=cntl[cntlp+1];
  cntl[cntlp+1] = cntl[cntlp];
  cntl[cntlp] = t ;
}

// "repeat" - resolve a "begin"/"while" loop
void repeat(){
  swap();
  again();
  ithen();
}


// "else" - resolve an "if" and copile a new back address
void ielse(){
  compword("bra");
  ba();
  swap();
  ithen();
}

// "for" - cause for semantics to be compiled
void ifor(){
  compword("push");
  begin();
  compword("dofor");
  ba();
}


// "next" - resolve a "for" loop
void inext(){
  repeat();
}

// compiles an S-string into memory
void str(){
  char d;    // delimiter
  char c;    // new char
  int  x=0;  // no. of chars
  ba();      // compile a back address
  d=key();
  while(1){
    c=key();
    if( c==d ) break;
    compchar( c );
    x++;
  }
  mstore( x, popc() );
}

// mark most recent dict. entry as immediate
void imm(){
  immonly();
  dict->flags=dict->flags|IMMEDIATE;
}

// mark most recent dict. entry as hidden
void hide(){
  immonly();
  dict->flags=dict->flags | HIDDEN;
}

// mark most recent dict entry as not hidden
void expose(){
  immonly();
  dict->flags=dict->flags & ~HIDDEN;
}

// mark all new dict entry as hidden
void hideall(){
  immonly();
  //mask=mask | HIDDEN;
  mask=HIDDEN;
}

// mark all new dict entry as exposed
void exposeall(){
  immonly();
  //  mask=mask & ~HIDDEN;
  mask=0;
}


// tries to convert text in wordb as a number
int tonumber(){
  int x;
  char *c;
  x=strtol( wordb, &c, 16 );
  if( *c!=0 ){
    fprintf(stderr,"Error: hex number expected:\n");
    quit(-1);
  }
  return x;
}

// parses source for a valid hex number
int number(){
  word();
  return tonumber();
}


// makes a new primitive 
void mkp(){
  int x=number();
  immonly();
  word();
  make_entry( wordb, x );
}

// compiles a number ( no "lit" prefix!! )
void pound(){
  compnum( number() );
}

// compiles a ascii charactor ( no "lit" prefix! )
void poundtick(){
  word();
  compchar( wordb[0] );
}


// impliments the setorg immediate word
// which sets the CP
//   ( a -- )
void setorg(){
  immonly();
  orgin=stack[ --sp ];
  if( orgin<=cp ){
    fprintf(stderr,"Error: orgin is less than current cp!\n" );
    fprintf(stderr,"Image Org is %x, CP is %x\n", orgin, cp );
    quit(-1);
  }
  cp=orgin;
  // compile space for saving XT,CP,and DH of image
  compnum( 0 ); // XT
  compnum( 0 ); // CP
  compnum( 0 ); // DH
}


// impliments the "done" immediate word:
// closes the current input source and pops input stack
void done(){
  immonly();
  if( ! ip ) return ; 
  fclose( in_stack[ip].in  );
  in_stack[ip].in=NULL;
  ip--;
}

// impliment the "include" immediate word:
// pushes a new input source onto the input stack
// This is a conditional include if a word by the
// same name as the filename argument doesn't 
// exist in the dictionary, then include the file.
void include(){
  immonly();
  word(); // get a word from current input (the filename)
  // check to see if this word exists
  if( find( wordb ) ) return;
  // word does not exist then make it exist and include
  make_entry( wordb, -1 );
  // check to see if there's room on the input stack 
  if( ip==7 ){
    fprintf( stderr,"Error: input stack is exhausted!\n" );
    quit(-1);
  }
  ip++;   // "push" the input stack pointer
  // initialize the new stack item
  in_stack[ip].lineno=1;
  strcpy( in_stack[ip].inname, wordb );
  in_stack[ip].in=fopen(in_stack[ip].inname,"r");
  if(!in_stack[ip].in){ 
	fprintf(stderr,"Error: Cannot open file:\n" );
	quit(-1);
  }
}


void mkdoer( char *action ){
  immonly();
  word();
  make_entry( wordb, cp );
  compword( "docreate" );
  compword( action );
}

// Our version of Create
void create(){
  mkdoer( "noop" );
}

// make a variable
void variable(){
  create();
  if( ! sp ){ 
    fprintf(stderr,"Stack underflow!:\n" );
    quit(-1);
  }
  compnum( stack[--sp] );
}

void constant(){
  mkdoer( "@" );
  if( ! sp ){
    fprintf(stderr,"Stack underflow!:\n" );
    quit(-1);
  }
  compnum( stack[--sp]);
  dict->value=stack[sp];
  dict->flags=dict->flags|CONSTANT;
}
  

void mult(){
  stack[sp-2]=stack[sp-1] * stack[sp-2];
  sp=sp-1;
}

// Allot some CP space
void allot(){
  immonly();
  if( ! sp ){ 
    fprintf(stderr,"Stack underflow!:\n" );
    quit(-1);
  }
  cp=cp+stack[--sp];
}

// Immediate mode cell and char
void icell(){
  immonly();
  stack[sp++]=CELL;
}

void ichar(){
  immonly();
  stack[sp++]=CHAR;
}


void field(){
  int tmp;
  immonly();
  if( sp<2 ){
    fprintf(stderr,"Stack underflow!: %d\n", sp );
    quit(-1);
  }
  stack[sp-1]=stack[sp-1]+stack[sp-2];
  tmp=stack[sp-1];
  stack[sp-1]=stack[sp-2];
  stack[sp-2]=tmp;
  mkdoer( "dofield" );
  compnum( stack[--sp] );
}



/*
  Basic compiler loop:
*/


void compile_state(){
  struct de *d;
  // first look up word in host's immediate dictionary
  if( !strcmp( wordb, ":" ) ){ colon(); return; }
  if( !strcmp( wordb, ";" ) ){ semi(); return; }
  if( !strcmp( wordb, "\\") ){ back(); return; }
  if( !strcmp( wordb, "(") ) { paren(); return; }
  if( !strcmp( wordb, "if") ){ iif(); return; }
  if( !strcmp( wordb, "then")){ithen(); return; }
  if( !strcmp( wordb, "begin")){ begin(); return; }
  if( !strcmp( wordb, "again")){ again(); return; }
  if( !strcmp( wordb, "until")){ until(); return; }
  if( !strcmp( wordb, "while")){ iwhile(); return; }
  if( !strcmp( wordb, "repeat")){ repeat(); return; }
  if( !strcmp( wordb, "else")) { ielse(); return; }
  if( !strcmp( wordb, "for" )){ ifor(); return; }
  if( !strcmp( wordb, "next")){ inext(); return; }
  if( !strcmp( wordb, "str" )){ str(); return; }
  if( !strcmp( wordb, "#" )){ pound(); return; }
  if( !strcmp( wordb, "#'" )){ poundtick(); return; }
  // Then check our target definitions
  if( d=find( wordb ) ){
    if( d->flags & CONSTANT ){
      compword( "lit" );
      compnum( d->value );
    }
    else compnum( d->xt );
    return ;
  }
  compword("lit");
  compnum( tonumber() );
}

void interp_state(){
  struct de *d;
  if( !strcmp( wordb, "\\") ){ back(); return; }
  if( !strcmp( wordb, "(") ) { paren(); return; }
  if( !strcmp( wordb, ":" ) ){ colon(); return; }
  if( !strcmp( wordb, "immediate")){ imm(); return; }
  if( !strcmp( wordb, "hide" )){ hide(); return; }
  if( !strcmp( wordb, "expose" )){ expose(); return; }
  if( !strcmp( wordb, "hideall" )){ hideall(); return; }
  if( !strcmp( wordb, "exposeall")){ exposeall(); return; }
  if( !strcmp( wordb, "p:" )){ mkp(); return; }
  if( !strcmp( wordb, "nodict")){ nodict=1; return; }
  if( !strcmp( wordb, "setorg" )){ setorg(); return; }
  if( !strcmp( wordb, "done" )){ done(); return; }
  if( !strcmp( wordb, "include" )){ include(); return; }
  if( !strcmp( wordb, "create")){ create(); return; }
  if( !strcmp( wordb, "allot")){ allot(); return; }
  if( !strcmp( wordb, "variable")){ variable(); return; }
  if( !strcmp( wordb, "constant")){ constant(); return; }
  if( !strcmp( wordb, "char")){ ichar(); return; }
  if( !strcmp( wordb, "cell")){ icell(); return; }
  if( !strcmp( wordb, "field")){ field(); return; }
  if( !strcmp( wordb, "struct")){ constant(); return; }
  if( !strcmp( wordb, "*")){ mult(); return; }
  // check for stack exhaustion here!!!!
  if( (d=find( wordb )) && (d->flags & CONSTANT) ){
      stack[ sp++ ]=d->value ;
      return;
  }
  stack[sp++]=tonumber();
}

void loop(){
  while( word() ){
    if( state ) compile_state();
    else interp_state();
  } 
}



int main (int argc, char *argv[]){
  int x;
  int i;
  init();
  for( x=1; x<argc; x++ ){
    if( argv[x][0] == '-' ){
      for( i=1; i<strlen(argv[x]); i++ ){
	switch( argv[x][i] ){
	  //  Set out file
	case 'o':
	  if( x+1 >= argc ){
	    fprintf(stderr,"Error: -o requires an argument\n" );
	    quit(-1);
	  }
	  out=argv[x+1];
	  x++;
	  i=strlen(argv[x]);
	  break;
	  // Set memory dump only to CP
	case 'c':
	  cpstop=1;
	  break;
	  // Set flag to only dump memory starting from last setorg
	case 's':
	  cpstart=1;
	  break;
	case 'd':
          dp=strtol(argv[x+1],NULL, 16);
	  x++;
	  i=strlen(argv[x]);
	  break;
	default:
	  fprintf(stderr,"Error: unknown option: %s\n", argv[x] );
	  quit(-1);
	}
      }
    }
    else{
      in_stack[ip].lineno=1;
      strcpy( in_stack[ip].inname, argv[x] );
      in_stack[ip].in=fopen(in_stack[ip].inname,"r");
      if(!in_stack[ip].in){ 
	fprintf(stderr,"Error: Cannot open file:\n" );
	quit(-1);
      }
      loop(); // go process this file
      fclose( in_stack[ip].in );
      in_stack[ip].in=NULL;
    }
  }
  quit(0);
}
    
