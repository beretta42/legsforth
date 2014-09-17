/* ------------------------------------------------------------------------
   CoCoBoot - A Modern way to boot your Tandy Color Computer              
   Copyright(C) 2013 Brett M. Gordon beretta42@gmail.com                  

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
   ------------------------------------------------------------------------
*/



/*
This is a Quick and Dirty Read-Only
filesystem image maker
*/


#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>

int fopt=0;          // format option flag
FILE *img=NULL;      // fs image file
char super[256];     // fs super image
char data[256];      // copy buffer
int start=0;         // first unused lsn in image
char *bfile;         // boot file
char *RSlabel=NULL;  // bastard RSDOS label pointer
FILE *dir=NULL;      // forth source directory file

// Experimental RSDOS FAT sector
unsigned char RSfat[68]={
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0xC1, 0x00, 0x00, 0x00, 0x00, 0x00, 
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00 
};

unsigned char RSdir[33]={
  0x41, 0x55, 0x54, 0x4f, 0x45, 0x58, 0x45, 0x43, 
  0x42, 0x41, 0x53, 0x00, 0xff, 0x22, 0x00, 0x07,
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
  0xff
};

unsigned char *autoexe="10 DOS\r" ;


// store a charactor to the super image
void cstore( int address, int data ){
  super[address]=data & 0xff;
}

// store a word to the super image
void store( int address, int data ){
  super[address]=data / 0x100;
  super[address+1]=data & 0xff;
}

// fetch a charactor from super image
unsigned int cfetch( int address ){
  return( super[address] );
}

// fetch a word from super image
unsigned int fetch( int address ){
  return( super[address]*0x100 + super[address+1] );
}

// returns the slot index data from super
unsigned int ifetch( ){
  return( cfetch( 4 ) );
}

// increments the slot index data in super
void iinc( ){
  cstore( 4, ifetch()+1 ); 
}

// increments count in current file descriptor
void slotinc(){
  int addr=ifetch()*8+8+2;
  store( addr, fetch(addr)+1 );
}


// print usage information
void printusage(){
  printf("rofs (c) GPLv3 Brett M. Gordon\n" );
  printf("Read-Only File System\n" );
  printf("rofs [options] <image> [files]\n");
  printf("-f,\t\tFormat Image\n");
  printf("-u,\t\tUsage\n");
  printf("-b <file>,\tCopy boot image\n");
  printf("-x <vol label>,\tExperimental RSDOS format\n");
  printf("-d <file>,\tMake a forth readable directory\n");
}

// initialization
void init(){
  int x;
  for( x=0; x<0x100; x++ ) cstore( x, 0 );
}

// cleanup
void cleanup(){
  if( img ){
    fseek( img, 0, SEEK_SET);
    fwrite( super, 0x100, 1, img );
    fclose(img);
  }
}

// Impliments Format option
void format( char *imgname ){
  img=fopen( imgname, "w" );
  if( !img ) {
    perror( "Cannot open image file" );
    exit( -1 );
  }
  init();                           // initialize super data copy
  // write the super data
  store( 0, 0x4247 );              // Magic
  store( 2, 0x0001 );              // Version
  cstore( 4, 0x00 );                // unused index
  fwrite( super, 0x100, 1, img );   // write super block to file
  fseek( img, 0x275ff, SEEK_SET );  // seek to end of DD SS disk
  fputc( 0, img );                  // write a 0 to force padding
  fclose( img );
}

void mount( char *imgname ){
  int ret;
  img=fopen( imgname, "r+" );
  if( !img) {
    perror( "Cannot open image file" );
    exit(-1);
  }
  ret=fread( super, 0x100, 1, img );
  if( ret<0x1){
    perror( "Error reading super block" );
    exit(-2);
  }
  // calcucate "start"
  if( ifetch()==0 ) start=1;
  else {
    int base=(ifetch()-1)*8+8;
    start= fetch( base )+fetch( base+2 ) ;
  }
  fseek( img, 0x100*start, SEEK_SET );
}

// appends a file to the image
void addfile( char *file ){
  FILE *f=fopen(file, "r" );
  if( !f){
    fprintf(stderr,"%s ", file );
    perror( "" );
    exit(-3);
  }
  // check for bastard FS and adjust
  // start if needed
  if( RSlabel && start < 0x145 )
  {
    long z=0;
    int s=0;
    fseek( f, 0, SEEK_END ); 
    z=ftell( f );
    s=z/256;
    if( z%256 ) s++;
    if( start+s >= 0x133 ) start=0x145;
    fseek( f, 0, SEEK_SET );
  }
  store( ifetch()*8+8, start );
  while(1){
    int ret;
    ret=fread( data, 1, 0x100, f);
    if( ret==0 ) break;
    ret=fwrite( data, 1, 0x100, img );
    if( ret==0 ){
      perror( "Error writing to image" );
      exit(-4);
    }
    start++;
    slotinc(); 
  }
  iinc();
  fclose(f);
}

void boot(){
  FILE *f=fopen( bfile, "r" );
  if( !f) {
    perror( "Error reading bootfile" );
    exit(-5);
  }
  fseek( img, 0x26400, SEEK_SET);
  while(1){
    int ret;
    ret=fread( data, 1, 0x100, f);
    if( ret==0 ) break;
    ret=fwrite( data, 1, 0x100, img );
    if( ret==0 ){
      perror( "Error writing to image" );
      exit(-4);
    }
  }
  fclose(f);
}

void dobastard(){
  int ret;
  // write fat
  fseek( img, 0x13300, SEEK_SET);
  ret=fwrite( RSfat, 1, 68, img );
  if( ret==0 ){
    perror( "Error writing to image" );
    exit(-5);
  }
  // write dir
  fseek( img, 0x13400, SEEK_SET);
  ret=fwrite( RSdir, 1, 33, img );
  if( ret==0 ){
    perror( "Error writing to image" );
    exit(-6);
  }
  // write labal
  fseek( img, 0x14200, SEEK_SET );
  ret=fwrite( RSlabel, 1, strlen(RSlabel)+1, img);
  if( ret==0 ){
    perror( "Error writing label to image");
    exit(-7);
  }
  // write file
  fseek( img, 0x14400, SEEK_SET);
  ret=fwrite( autoexe, 1, 7, img );
  if( ret==0 ){
    perror( "Error writing to image" );
    exit(-8);
  }
}

int main( int argc, char **argv ){
  int r;
  while(1){
    r=getopt( argc, argv, "x:fub:" );
    if( r=='f' ){  fopt=1; }
    if( r=='b' ){  bfile=optarg; }
    if( r=='u' ){  printusage(); exit(0); }
    if( r=='?' ){  printusage(); exit(-1); }
    if( r=='x' ){  RSlabel=optarg; }
    if( r<0   ) break ;
  }
  if( !argv[optind]){ 
    printf("Disk image required!\n" );
    printusage();
    exit(-1);
  }
  if( fopt) format( argv[optind] );
  mount( argv[optind] );
  optind++;
  while( optind<argc ) addfile( argv[optind++] ) ;
  if( bfile ) boot();
  if( RSlabel ) dobastard();
  cleanup();
  exit(0);
}


