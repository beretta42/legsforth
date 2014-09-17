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
This is a proglet, quick-and-dirty style, designed to find the
difference between two ROM images and spit out a "diff" patch
file.  The First 0x1a pages of a HDBDOS images is mostly "stock" 
DECB, and the tail 0x7 pages are the meat of the the diffence.
To boot a certain HDB version, well load up lower RAM/ROM with a
reference copy of DECB, and the highest pages with the particular
HDB version, and finally apply a binary patch for the few bytes
that need changing in the lower 0x1a pages

The Format of the patch file is and Zero terminated array of 
this structure:

offset    size    what
0         2       address of difference
2         1       data byte 

We'll error-out if the resultant patch file produces more than
a page worth of memory.  Byte Address 0 must be equal in both
images, or the zero termed array won't work!

*****
And after realizing that with the bin patch file and the 
top roms totaling 35+ files on the bootdisk, which the rofs
system only can hold 33 files, that this utility might as well
cat the two images together:

bytes   0x0-0x699   - upper 7 sectors of ROM image
bytes 0x700-0x799   - the difference of the lower ROM image

total disk used with this scheme:   41,216 bytes
total used with separate roms:     139,264 bytes

*/

#include <stdio.h>
#include <stdlib.h>
#include <getopt.h>
#include <unistd.h>

#define MAXCMP   0x1900  // compare only the bottom of ROM

FILE *ref=NULL;      // the reference image's file
FILE *cmp=NULL;      // the object image's file
FILE *out=NULL;      // the resultant patch file
int addr=0;          // current address of image
int size=0;          // resultant out file size


void printusage(){
  printf("idiff (C) GPLv3 Brett M. Gordon\n");
  printf("makes a binary patch file\n");
  printf("idiff [options] <reference file> <object file> <output file>\n");
  printf("-u,\t\tUsage\n");
}

void cleanup(int stat){
  fclose( out );
  fclose( cmp );
  fclose( ref );
  exit( stat );
}

// spit out a byte to the outfile
void bemit( char byte ){
  int ret = fwrite( &byte, 1, 1, out );
  if( ret != 1 ){
    perror( "Cannot write to output file:" );
    cleanup(-1);
  }
  size++;
}

// spit out a word to the outfile
void wemit( int word ){
  bemit( word >> 8 );
  bemit( word & 0xff );
}

void loop(){
  char a,b;
  int r;
  for( addr=0; addr<MAXCMP; addr++){
    r = fread( &a, 1, 1, ref );
    if( r != 1 ){
      perror( "Cannot read from reference file:");
      cleanup(-1);
    }
    r = fread( &b, 1, 1, cmp );
    if( r != 1 ){
      perror( "Cannot read from object file:");
      cleanup(-1);
    }
    if( a != b ){
      wemit( addr );
      bemit( b );
    }
  } // for
  wemit( 0 );
}
  

int main( int argc, char **argv ){
  int r;

  while(1){
    r=getopt( argc, argv, "u" );
    if( r=='?' ){ printusage(); exit(-1); }
    if( r=='u' ){ printusage(); exit(0); }
    if( r<0 ) break ;
  }

  if( argc - optind < 3 ){
    printf("Not enough image files specified!\n");
    printusage();
    exit(-1);
  }
  
  ref = fopen( argv[optind], "r" );
  if( !ref ){
    perror( argv[optind] );
    exit(-1);
  }
  optind++;

  cmp = fopen( argv[optind], "r" );
  if( !cmp ){
    perror( argv[optind] );
    cleanup(-1);
  }
  optind++;

  out = fopen( argv[optind], "w" );
  if( !out ){
    perror( argv[optind] );
    cleanup(-1);
  }  
  // copy last 7 pages to out file
  fseek( cmp, 0x1900, SEEK_SET );
  while( size < 0x700 ){
    char a;
    int ret=fread( &a, 1, 1, cmp );
    if( ! ret ){ 
      perror( "Unable to read from object file!" );
      cleanup( -1 );
    }
    bemit( a );
  }
  size=0;
  fseek( cmp, 0, SEEK_SET );
  loop();
  if( size > 0x100 ){
    fprintf( stderr, "Resultant patch file too big!\n" );
    cleanup( -1 );
  }
  while( size < 0x100 ) bemit( 0 );
  cleanup(0);
}

