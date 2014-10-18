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

  This is the CoCoBoot Kernel.  It contains:
    * setup/setting data structures
    * DSKCON loading ability of data structures
    * actions upon these data structure

)


nodict		      \ no dictionary to be compiled to target

include ccbimp.fs
include out.fs
include debug.fs
include dskcon.fs
include rofs.fs


: hello
    slit str "COCOBOOT2" type cr ;


: ?panic ( u -- ) \ issue error and panic
   dup if wemit slit str " PANIC!" type loop else drop then ;

: width40 ( -- ) \ change to 40 column mode
   f65c exem ;

: width80 ( -- ) \ change to 80 column mode
   f679 exem ;

: width32 ( -- ) \ change to 32 column mode
   f652 exem ;


: ticks ( -- a ) \ address of ticks
    16 ;
: bpb ( -- a ) \ boot parameter block data ptr
    18 ;



\ *************************
\ Boottrack loading
\ *************************

: dos ( u -- ) \ chain load from drive u
   drive c!
   2600 p> daddr !
   264 lsn !
   12 for
      read ?panic
      lsn @ 1+ lsn !
      daddr @ 100 + daddr !	       
   next 
   2600 pw@ 4f53 = if 2602 exem then
   slit str "NO DOS FOUND!" type cr key drop
;


\ **************************
\ Key extended and timeouts
\ **************************

: int  ( -- )
    ticks @ 1- ticks ! ;

: bkey? ( -- f ) \ returns true if any key is down
    0 ff02 p!    \ set all column strobes
    ff00 p@      \ read the rows
    ff = 0= ;    

\ waits one sec for keystroke
\ returns true if button was presses, false if not
: waitsec ( -- f ) \ waits one sec for keystroke
    3c ticks !     \ ticks = 60 jiffies
    lit int 2 ! ion \ install timer interrupt
    begin bkey? if -1 exit then ticks @ 0< until ioff 0 ;

\ waits timeout secs for keystroke 
\ returns true if button was presses, false if not
: wait ( u -- f ) 
    cr
    slit str "PRESS ANY KEY FOR MENU" type cr
    slit str "AUTOBOOT IN: " type
    for 
      r@ 1+ bemit
      waitsec if pull drop -1 exit then
      8 8 emit emit
    next cr 0
;



: init_screen ( -- ) \ initializes the screen
     \ 0 ffb0 p!       \ black background
     \ ff ffb8 p!      \ white foreground
     \ width40         \ readable on most: 40 column screen
;

\ **************************
\ Boot Parameter Block
\ **************************

: load_bpb ( -- ) \ load bpb from disk
   bpb @ 0 load ?panic ;

: init_bpb ( -- ) \ initialize the boot parameter block
    100 alloc bpb !    \ allocate space for bpb
    load_bpb
;

: timeout ( -- a ) \ timeout
    bpb @ ;

: sys ( -- a ) \ system type coco2 - 0, coco3 - 1
    bpb @ 2 + ;

: toram? ( -- a ) \ coco2 rom to ram flag
    bpb @ 4 + ;

: defpro ( -- a ) \ default boot profile
    bpb @ 6 + ;

: valid ( -- a ) \ if this bpb is valid
    bpb @ 8 + ;

: ccbnoauto ( -- a ) \ no autoboot flag
    bpb @ a + ;


: profs ( -- a ) \ 1st profile slot
    bpb @ 40 + ;

\ 
\ and the Boot profile structure
\ 

: pro_method ( profile -- a ) \  method varible
    16 + ;  
: pro_drive  ( profile -- a ) \  drive no. variable
    18 + ;  \ only need 1 byte here !!!
: pro_slotno ( profile -- a ) \ slot number for loading
    1a + ;
: pro_mpino ( profile -- a ) \ slot number for mpi
    1c + ;
: pro_sideno ( profile -- a ) \ super IDE flash rom bank no
    1e + ;
: pro_hwaddr ( profile -- a ) \ device base address
    20 + ;
: pro_offset ( profile -- a ) \ HDB offset
    22 + ;  \ 3 bytes !
: pro_defid  ( profile -- a ) \ HDB Default SCSI ID
    25 + ;  \ 1 bytes
: pro_noauto \ ( profile -a ) \ HDB defeat autoboot flag
    26 + ;  \ flag


: .emit ( -- ) \ emits a "."
   2e emit ;    

\ end of bpb structure **************************

\ ************************
\ Changing MPI Switch setting and Super IDE Flash
\ ************************

: mpi ( u -- ) \ change mpi to u
    dup shl shl shl shl + ff7f p! ;
: rommode ( -- ) \ switch to rom map
    0 ffde p! ;
: rammode ( -- ) \ switch to ram map
    0 ffdf p! ;

\ *************************
\ DROM Switching
\   "drom" causes a switch from
\   one HDBDOS to Another HDBDOS in memory
\ *************************


\ wrong address.. there could be something loaded at 1000!!!
: drom ( u -- ) \ load slot u in rom and execute
    sys @ 0= if toram
    else 0 ffdf p! then         \ if coco2 then copy rom to ram
    1000 p> 2 load ?panic
    2900 p> swap load ?panic
    \ patch the decb rom
    3000 p> 
    begin @+ dup while 
	    1000 p> + push c@+ pull c!
    repeat 2drop
    \ mv image from 1000 to c000
    llioff
    1000 c000 2000 mv
;


\ defeating HDB's autoboot: scan for the "AUTOEXEC"
\ string and change its first two chars to "!!"
\ to (hopefully) make the file unfindable to HDB

: defauto ( -- ) \ defeat autoboot
   ddfb begin dup pw@ 4155 = if 2121 swap pw! exit then 1+ again
;


: dexec ( -- ) \ execute drom setup code ( reboots to loaded HDB )
   c002 exem ;


: dromoff ( -- u ) \ slot offset to disk roms
   3 ;

: setup ( -- ) \ execute the setup 
  1 dofile ;  


\ This applies the profile's HDB image/setting
\ it essentially allows swaping of HDB images on
\ bootup.
: HDBSwitch ( profile -- ) \ apply HDB switch
    \ create image and put it in place
    dup pro_slotno @ dromoff + drom
    \ switch the mpi
    dup pro_mpino @ mpi
    \ fixup the HDB HW address 
    dup pro_hwaddr @ d93b pw!
    \ fixup the HDB os9 offset
    dup pro_offset c@+ d938 p! @ d939 pw!
    \ fixup the Default ID
    dup pro_defid c@ d93e p!
    \ defeat autoboot?
    dup pro_noauto @ if defauto then
    drop ioff
;



: bootdrom ( profile -- ) \ boot drom
    dup HDBSwitch
    \ quit bye reiniting HDBDOS
    dexec
;



\ *************************
\ ROM / Super IDE flash boot
\ *************************

: cold ( -- ) \ cold reboot coco
    0 71 p! fffe pw@ exem ;

: bootrom ( profile -- ) \ boot rom
    \ set MPI switch
    dup pro_mpino @ mpi  
    \ set Super IDE flash no
    dup pro_sideno @ over pro_hwaddr @ 9 + p!
    \ Reset GIMME if a coco3 and goto rom mapping
    sys @ if cc ff90 p! then rommode 
    \ do a cold reboot
    cold
;


\ *********


\ ************************
\ Boot new-style os9
\ ************************

: bootos9 ( a u -- ) \ takes profile address and index no
    \ os9 boot will soon be bigger than
    \ this kernel can be, so chain-load image
   15 dofile     
;

\ **********************

\ boot passes in the following to the called boot methods
\    method ( a u -- )
\    a = address of bootprofile
\    u = boot profile number

: boot ( u -- ) \ boot profile number
    push
    0 r@ for 30 + next profs +
    slit str "BOOTING " type
    dup type cr
    dup pro_method @ 
      dup 0=  if over pro_drive c@ dup bemit dos else
      dup 1 = if drop bootdrom else
      dup 2 = if drop bootrom else
      dup 3 = if drop r@ bootos9 else
      then then then
      4 ?panic
;




: menu ( -- )
  cr
  profs
  slit str "0 - " type dup type cr
  30 +
  slit str "1 - " type dup type cr
  30 +
  slit str "2 - " type dup type cr
  30 + 
  slit str "3 - " type dup type cr
  drop
  slit str "S - SETUP" type cr
  begin
    key
    dup 53 = if drop jmp setup else
    dup 30 = if drop 0 jmp boot else
    dup 31 = if drop 1 jmp boot else
    dup 32 = if drop 2 jmp boot else
    dup 33 = if drop 3 jmp boot else
    then then then then then
    drop
  again
;


: main 
    lit .emit sectvec !          \ save sector load xt
    bkey? init_screen hello      \ detect key down and init screen
    c006 pw@ p> 1+ c@ drive c!    \ save boot drive no
    mount  ?panic  		 \ mount rofs filesystem
    init_bpb			 \ load boot parameter block
    valid @ 0= if jmp setup then \ if bpb is invalid then setup
    \ if boot up key is pressed or autoboot disable then goto menu
    ccbnoauto @ or if jmp menu then
    \ wait for autoboot
    timeout @ wait if jmp menu else defpro @ jmp boot then
;






