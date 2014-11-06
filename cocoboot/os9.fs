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

1400 setorg

include 3var.fs
include 2var.fs
include debug.fs
include hdb.fs
include ticker.fs



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


\ 
\ When in HDB, its easiest to dynamically change
\ "The Offset" to get full LSN access, rather
\ then depend on a whole mess of drive geometry changes!
\

: deblockf ( -- a ) \ deblock flag
    dovar # 0

: 9readr ( l h -- ) \ read a sector without deblocking
    HDBOFF p> 3!
    0 lsn !
    read drop ;

: 9readd ( l h -- ) \ read a sector with 512 sector deblocking
    over push
    daddr @ push
    600 p> daddr !
    dup shr push 
    1 and if 8000 else 0 then
    swap shr or pull
    9readr
    pull daddr !
    pull 1 and if 700 else 600 then
    daddr @ >p 100 mv
;


: 9read ( l h -- ) \ read a sector l h from drive
    deblockf @ if 9readd else 9readr then
;


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

This part was written before BFC did file structure
It should be rewritten.



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

\ this is lousy logic here, unclear!!!
: next? ( -- u ) \ 0 if EOF, else bytes in next sector
   lcount @ dup 0= if exit then 
   1 = if odd @ dup 0= if drop 100 then else 100 then  
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
   daddr ! gnext 9read ;

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
   fd daddr ! 9read 
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
      dup c@ if dup 9type cr then 20 +      
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


: lookup ( ca file -- d -1 | 0 ) \ find a name in a directory, -1 is match
    push
    r@ frewind
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
    wdir c + 2@ rot wdir lookup if
	\ if lookup ok:
	wdir fopen wdir fdir?
	\ found and name is a dir
	if 2drop false exit
	\ lookup not found
     else wdir fopen true exit then
   then
   2drop true ;


: mount ( -- f ) \ mount RBF filesystem
    \ read LSN 0
    here daddr !
    0 0 9read
    \ we really should check more of the filesystem
    \ structure  here, to make sure we're really
    \ dealing with a proper OS9 filesystem

    \ print volume name for coolness
    here 1f + 9type cr
    \ end of verify system
    here 8 + 3@ rdir 3!    \ get root dir inode number
    \ open root directory as working dir
    falloc wdir' !
    rdir 3@ wdir fopen
    wdir fdir? 0=
\    wdir dump
;


\ not smaller     nz = error
\ equal     z  = ok
\ bigger       = error


: panic ( f -- "err" )
   if 
      cr
      slit str "PANIC!" type cr
      slit str "ANYKEY TO REBOOT" type key drop cold
   then
;

: gimme ( pa -- ) \ applies gimme setting to primitive address
    p>
    6c00 !+
    0000 !+
    0900 !+
    0000 !+
    0320 !+
    0000 !+
    00 c!+
    ec01 !+
    00 c!+
    drop
;

: main ( a u -- )  \ profile addr and index pass in from chain loader
    \ initialize memory
    meminit
    \ load up the HDB context
    drop dup HDBSwitch cr          ( a )
    \ check for deblock and set local flag
    dup pro_noauto @ deblockf !
    \ Now we further patch HDB to make a proper
    \ init routine.
    
    \ find loc of warm start address in setup routine
    \ and replace with NOP to restrict HDBINIT to RTS to US
    d93f begin dup pw@ a0e2 - while 1+ repeat 2 + p>
    12 c!+ 12 c!+ drop
    \ find call to BEEP that doesn't work without the Standard
    \ IRQ handler, and write a rts instead. cut the init routine short.
    \ that's fine, it just call BASIC to autoboot, anyway.
    d93f begin dup pw@ d934 pw@ - while 1+ repeat 1 -
    39 swap p!

    \ and find and execute HDINIT in HDB
    d93f begin dup pw@ 0900 - while 1+ repeat 1 - exem
    \ set HDB's IDNUM
    dup pro_drive c@ IDNUM p!
    \ reset DSKCON trk/sec, we don't use them
    \ we'll directly mangle HDBOFF instead
    0 drive c!
    \ check for root pause setting in profile
    dup pro_pflags @ if
	slit str "INSRT ROOT, ANY KEY" type key drop cr
    then
    \ try to mount the RBF
    mount
    if slit str "MOUNT FAILED" type cr true panic then

    \ check boot file is in root before loading ccbkrn as
    \ after touch upper memory, DECB and console will not be
    \ available to us.
    \ dup dump drop key drop
    \ dup pro_hdbname dump drop key drop
     dup pro_hdbname wdir lookup 0= if
	 slit str "BOOTFILE NOT FOUND" type cr true panic then
     falloc dup push fopen r@ fsize
     if slit str "BOOTFILE WAY TOO BIG" type cr true panic then
     dup 7000 swap u< if slit str "BOOTFILE TOO BIG" type cr true panic then
     3000 u< if slit str "BOOTFILE TOO SMALL" type cr true panic then
     pull drop

     1 dpReset

    \ Find the ccbkrn file
    
    slit str "ccbkrn" wdir lookup 0= if
	slit str "NO CCBKRN ON ROOT" type cr true panic then
    falloc dup push fopen

    \ load up CCBKRN file
    2600 p>
    begin r@ fnext while dup r@ fget dpTick 100 + repeat drop

   
    llioff rammode
    \ copy ccbkrn to place in memory
    \ we cannot use DECB console routines from here
    2600 f000 f00 mv

    4 dpReset
    
    \ load up the OS9Boot file
    1 ffa4 p!
    2 ffa5 p!
    
\    slit str "OS9Boot" wdir lookup 0= if cold then
    dup pro_hdbname wdir lookup 0= if true panic then
    falloc dup push fopen
    r@ fsize drop dup ff00 and swap ff and if 100 + then ( size )
    dup f000 swap - ( size pstart )
    \ put OS9Boot parts below C000 directly into memory
    p> begin dup >p c000 - while dup r@ fget dpTick 100 + repeat drop ( z )
    \ we can't overwrite disk basic while we're loading so
    \ put OS9Boot parts in C000 - E000 into temp area for copying
    \ to place after finishing up loading
    1000 p> begin dup >p 3000 - while dup r@ fget dpTick 100 + repeat drop ( z )
    \ and put OS9Boot part in E000 - F000 directly into memory
    e000 p> begin dup >p f000 - while dup r@ fget dpTick 100 + repeat drop ( z )
    \ we're done with disk basic now, so copy the c000 block into memory
    3 ffa6 p!
    1000 c000 2000 mv
    
    \ map phys 0 to cpu space 0000
    0 ffa0 p!
    \ clear dp block
    0 p> 100 for 0 c!+ next drop
    \ make screen pointer
    8 6002 pw!
    \ clear screen
    6004 p> 1e0 for 2020 !+ next drop
    \ setup gimme & DP mirror
    ff90 gimme
    90 gimme

    \ jump to ccbkrn ( size arg is on stack already)
    f009 pw@ f000 + exem
    
    cr slit str "OK!" type cr
    begin key emit again ;   

