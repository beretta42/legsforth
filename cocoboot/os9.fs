\ ------------------------------------------------------------------------
\ CoCoBoot - A Modern way to boot your Tandy Color Computer
\ Copyright(C) 2013 Brett M. Gordon beretta42@gmail.com
\
\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.
\
\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.
\
\ You should have received a copy of the GNU General Public License
\ along with this program.  If not, see <http://www.gnu.org/licenses/>.
\ ------------------------------------------------------------------------


( 

This is the new-style os9 booter overlay file

)

1000 setorg

include 3var.fs
include 2var.fs
include debug.fs

: 9type ( a -- "string" ) \ emit a os9 formatted string	
   begin c@+ dup 7f and emit 80 and until drop ;
: 9sz ( 9a -- u ) \ returns the size of a os9 formatted string
   0 swap begin c@+ 80 and 0= while swap 1+ swap repeat drop 1+ ;

\ * old os9 module based sector reads

\ : moddata ( -- a ) \ pointer to module's data
\   dovar # 0

\ : daddr ( -- a ) \ sector data address variable
\   moddata @ modoff + ;

\ : meminit ( -- ) \ initialize forth memory
\   1002 @ cp !                   \ set cp to overlay's cp
\   100 alloc moddata !           \ allocate modules instance data 
\ ;
\ : init ( -- f ) \ init module
\   moddata @ >p modinit ;
\
\ : read ( l h -- ) \ read a sector
\   moddata @ >p modread ;


: 9read ( l h -- ) \ read a sector
    drop lsn ! read drop ;

: meminit ( -- ) \ initialize forth memory
   1002 @ cp !                   \ set cp to overlay's cp
;


: rdir ( -- 3a ) \ root directory inode of fs
   dovar # 0 # 0


: obj  ( -- a ) \ object variable
   dovar # 0 ;
: self  ( -- obj ) \ returns ourself
   obj @ ;
: msg ( obj o -- ) \ send message to object
   obj @ push swap dup obj ! + @ exec pull obj ! ;
    

(

  File structure
in hex:
offset	size	what
0	2	xt iopen method - open inode by number
2	2	xt fsize method - get file size
4	2	xt fget method - get next sector from file
6	2	xt frewind method - reset file position
8	2	xt fdir? method - is file a directory ?
a	2	xt fnext method - size of next sector
c	4	inode number	
10	100	file descriptor buffer
110	2	number of sectors in file
112	2	odd bytes in last sector 
114	2	segment list ptr
116	2	sector count in segment
118	3	current lsn
	11b	length of data struct
)

: inode ( -- a ) \ inode of file
   self c + ;

: fd ( -- a ) \ file descriptor buffer
   self 10 + ;

: sz ( -- d ) \ file size
   fd 9 + 2@ ;

: lcount ( -- a ) \ logical sector count
   self 110 + ;

: odd ( -- a ) \ odd data address
   self 112 + ;

: segptr ( -- a ) \ ptr to segment list
   self 114 + ;

: count ( -- a ) \ no of sectors remaining in segment
   self 116 + ;

: lsn ( -- a ) \ current lsn
   self 118 + ;

: next? ( -- u ) \ 0 if EOF, else bytes in next sector
   lcount @ dup 0= if exit then 
   1 = if odd @ else 100 then  
;

: gnext ( -- d ) \ get next lsn of file
   \ if count is zero then get next segment
   count @ 0= if 
     segptr @ 3@+ lsn 3! \ get new lsn
     @+ count !          \ get new count
     segptr !            \ update segptr
   then
   lsn 3@
   \ adjust vars
   2dup 2inc lsn 3!	\ inc lsn
   count @ 1- count !   \ dec seg count
   lcount @ 1- lcount ! \ dec logical sector count
;   


: get ( a -- ) \ get next sector of file
   >p daddr ! gnext 9read ;

: dir? ( -- f ) \ returns true if file is a directory
   fd c@ 80 and ;   

: rewind ( -- ) \ reset file's position ( to beginning )
   \ calc lcount and odd bytes in file
   sz drop dup bsw ff and swap ff and 
   dup odd ! if 1+ then lcount !
   \ reset segptr to start of segment list
   self 20 + segptr !
   \ reset segment sector count
   false count !
;

: iopen ( d -- ) \ init file object by inode 
   2dup inode 2! 
   fd >p daddr ! 9read 
   rewind
;


\ external messages

: fopen ( d file -- ) \ open a file
   0 msg ;
: fsize ( file -- d ) \ returns file size
   2 msg ;
: fget ( a file -- ) \ loads next sector from file
   4 msg ;
: frewind ( file -- ) \ resets file's position
   6 msg ;
: fdir? ( file -- f ) \ is file a directory?
   8 msg ;
: fnext ( file -- u ) \ returns size of next sector
   a msg ;

: falloc ( -- file ) \ allocate a file object's memory
   11b alloc dup 
   \ fill out vmt
   lit iopen !+
   lit sz !+
   lit get !+
   lit rewind !+
   lit dir? !+
   lit next? !+
   drop
;

: wdir' ( -- a ) \ working dir file struct ptr
   dovar # 0

: wdir ( -- file ) 
   wdir' @ ;


: pdir ( file -- "dir" ) \ print directory
   push
   begin
   r@ fnext while
     here r@ fnext over r@ fget 
     shr shr shr shr shr for
      dup c@ if dup 9type dup 9sz wemit cr then 20 +      
     next drop
   repeat
   pull drop
;   


: os9cmp ( ca 9a -- f ) \ returns true if ca matches os9str ptr 9a
   dup 9sz rot @+ rot over <> if 2drop drop false exit then
   for 
     c@+ rot c@+ 7f and rot <> if 2drop pull drop false exit then swap
   next 2drop true
;


: lookup ( ca file -- d -1 | 0 ) \ find a name in a directory
   push
   begin
   r@ fnext while
     here r@ fnext over r@ fget 
     shr shr shr shr shr for
      dup c@ if 
          2dup os9cmp if nip 1d + 3@ true pull pull 2drop exit then 
      then 20 +      
     next drop 
   repeat
   drop pull drop false
;

: chdir ( ca -- f ) \ change working dir to ca
   wdir c + 2@ rot wdir frewind wdir lookup if
     wdir fopen wdir fdir? if 2drop true exit 
     else wdir fopen false exit then
   then
   2drop false ;


: mount ( -- f ) \ mount RBF filesystem
   \ mount filesystem
   here >p  daddr !
    0 0 9read
   here 8 + 3@ rdir 3!    \ get root dir inode number

   \ open root directory as working dir
   falloc wdir' !
   rdir 3@ wdir fopen
   wdir fdir? 
;

: panic ( f -- "err" )
   0= if 
      cr
      slit str "PANIC:" type cr
      slit str "ANYKEY TO REBOOT" type key drop cold
   then
;


: main ( a u -- )  \ profile addr and index pass in from chain loader
    \ initialize memory
    meminit
    \ load up the HDB context
    drop HDBSwitch
    \ patch HDB INIT to RTS back to us
    \ find loc of warm start address in setup routine
    \ and replace with NOP
    d93f begin dup pw@ a0e2 - while 1+ repeat 2 + p>
    12 c!+ 12 c!+ drop
    \ and find and execute HDINIT in HDB
    d93f begin dup pw@ 0900 - while 1+ repeat 1 - exem
    \ try to mount the RBF 
   mount panic             
   
   \ change working dir to "/CCB"
   \ slit str "CCB" chdir panic

   cr slit str "OK!" type cr
   begin key emit again ;   


