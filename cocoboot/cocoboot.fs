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
hideall

include ccbimp.fs
include out.fs hideall
\ include debug.fs   \ commented out for size :(
include dskcon.fs
include rofs.fs
include ticker.fs


: splash dovar
    \ Splash image data curtesy of Simon's SG editor!
	# 20b6 # 86a6 # 030f # 030f # 020f # 0f14 # 2000

: hello
    400 p> 100 for 2020 !+ repeat drop 
    splash >p 48a d mv
;

: ?panic ( u -- ) \ issue error and panic
   dup if wemit slit str " PANIC!" type else drop then ;


: ticks ( -- a ) \ address of ticks
    1b ; expose
: bpb ( -- a ) \ boot parameter block data ptr
    1d ; expose



\ **************************
\ Key extended and timeouts
\ **************************

: int  ( -- )
    -1 ticks +! ;

: bkey? ( -- f ) \ returns true if any key is down
    0 ff02 p!    \ set all column strobes
    ff00 p@      \ read the rows
    ff = 0= ;  expose  

\ waits one sec for keystroke
\ returns true if button was presses, false if not
: waitsec ( -- f ) \ waits one sec for keystroke
    3c ticks !     \ ticks = 60 jiffies
    lit int 2 ! ion \ install timer interrupt
    begin bkey? if -1 exit then ticks @ 0< until ioff 0 ;

: clsstatus ( -- ) \ clear status line
    5e0 p> 10 for 2020 !+ next drop 5e0 88 pw!
;

\ waits timeout secs for keystroke 
\ returns true if button was presses, false if not
: wait ( u -- f )
    5e0 88 pw!
    slit str "AUTOBOOT IN: " type 
    for
      r@ 1+ 5ee 88 pw! bemit
      waitsec if pull drop clsstatus -1 exit then
    next clsstatus  0
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
    200 alloc bpb !    \ allocate space for bpb
    load_bpb
;

exposeall

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
    bpb @ 30 + ;

\ 
\ and the Boot profile structure
\

16  \ this should be word that gets size of static area
2    field pro_method  \ which boot method to use
2    field pro_drive   \ drive no
2    field pro_slotno  \ which flavor of HDB to use
2    field pro_mpino   \ mpi switch setting
2    field pro_sideno  \ flash bank to use
2    field pro_hwaddr  \ HDB hardware base address
3    field pro_offset  \ HDB partition offset
1    field pro_defid   \ HDB default ID
2    field pro_noauto  \ HDB autoboot defeat flag
2    field pro_pflags  \ Pause for Boot / Flags
a    field pro_hdbname \ HDB autoexec file name
struct profileZ        \ size of this structure

hideall

    
\ end of bpb structure **************************
    
: prof2a ( u -- a ) \ get data address for profile struct u
    0 swap for profileZ + next profs + ;

    
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
\ HDBDROM Switching
\   "drom" causes a switch from
\   one HDBDOS to Another HDBDOS in memory
\ *************************


: drom ( u -- ) \ load slot u in te
    1 dpReset
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
   ddd0 begin dup pw@ 4155 = if 2121 swap pw! exit then 1+ again
;

: patchAuto ( profile -- ) \ patch in autoexec filename in to hdb
    \ find start of "AUTOEXEC" in HDB image
    ddd0 begin dup pw@ 4155 = if ( pro pa )
	    \ blank out old string
	    dup p> 2020 !+ 2020 !+ 2020 !+ 2020 !+ drop
	    \ copy string new string
	    push pro_hdbname @+ push >p pull pull swap mv
	    exit
	then 1+ again
;



: dpatchdata ( -- a ) \ See aloadbin.asm for this code
    dovar
    # 8e00 # 00ce # 02dc # dfa6 # c64d # e7c0 # c622 # e7c0  
    # 8608 # e680 # e7c0 # 4a26 # f96f # c486 # 4d6e # 9fc1
    # 3700  
;

: dbinpatch ( -- ) \ batch HDB to allow for autoloading BIN files
    \ 400 88 pw!
    \ find autoexe string address
    ddfb begin dup pw@ 4155 - while 1+ repeat  push    
    \ find destination
    d930 begin dup pw@ r@ - while 1+ repeat 1- dup  \ dup wemit key drop
    dpatchdata >p swap 21 mv 1+ pull swap pw!   
;


: dexec ( -- ) \ execute drom setup code ( reboots to loaded HDB )
    \ patch HDB to stop from clearing IDNUM
    d93f begin dup pw@ 0900 - while 1+ repeat 08 swap p!
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
    dup pro_defid c@ dup dup  d93e p! 151 p! 14f p!
    \ auto booting
    dup pro_noauto @ 0= if
	\ patch for bin autoboot
	dup pro_pflags @ if dbinpatch then
	\ patch up AUTOEXEC boot name 
	dup pro_hdbname @ if dup patchAuto then
    else
	\ defeat autoboot?
	defauto
    then
    drop ioff
; expose



: bootdrom ( profile -- ) \ boot drom
    \ switch into new hdbdos
    dup HDBSwitch
    \ quit bye reiniting HDBDOS
    dexec
;


\ *************************
\ Boottrack loading
\ *************************

: dos ( a u -- ) \ chain load from drive u
    drop                             ( prof )
    \ swap in choosen HDB and init
    dup HDBSwitch
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
    dup pro_drive c@ 151 p!

    
    slit str "rdy?" type key drop
    dup pro_drive c@ dup bemit drive c!
    2600 p> daddr !
    264 lsn !
    12 for 2e emit
      read ?panic
      lsn @ 1+ lsn !
      daddr @ 100 + daddr !	       
   next 
   2600 pw@ 4f53 = if slit str "found!" type key drop 2602 exem then
   slit str "NO DOS FOUND!" type cr key drop
;





\ *************************
\ ROM / Super IDE flash boot
\ *************************

: cold ( -- ) \ cold reboot coco
    0 mpi
    0 71 p! fffe pw@ exem ; expose

: bootrom ( profile -- ) \ boot rom
    \ set MPI switch
    dup pro_mpino @ mpi
    \ set ROM bank
    dup pro_hdbname @ dup if
	push dup pro_sideno @ over pro_hwaddr @ pull 1 = if 9 else 3 then
	+ p!
    else drop then
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
    \ this kernel can be, so chain-load the os9 boot forth image
    2 dpReset
   15 dofile     
;

\ **********************

\ boot passes in the following to the called boot methods
\    method ( a u -- )
\    a = address of bootprofile
\    u = boot profile number

: boot ( u -- f ) \ boot profile number
    dup push
    prof2a
    slit str "BOOTING " type
    dup type cr
    dup pro_method @
      dup 0 = if 2drop slit str "VOID PROFILE!" type key drop else
      dup 1 = if drop r@ dos else
      dup 2 = if drop bootdrom else
      dup 3 = if drop bootrom else
      dup 4 = if drop r@ bootos9 else
      then then then then then
      true
;


: menu ( -- )
    begin
    4a0 88 pw!
    profs
    slit str "0 - " type dup type cr
    profileZ +
    slit str "1 - " type dup type cr
    profileZ +
    slit str "2 - " type dup type cr
    profileZ + 
    slit str "3 - " type dup type cr
    profileZ +
    slit str "4 - " type dup type cr
    profileZ +
    slit str "5 - " type dup type cr
    profileZ + 
    slit str "6 - " type dup type cr
    profileZ +
    slit str "7 - " type dup type cr
    drop
    slit str "S - SETUP" type cr
    slit str "X - DEFAULT" type cr
    hide
    key
    dup 53 = if drop jmp setup else
    dup 30 = if drop 0 boot else
    dup 31 = if drop 1 boot else
    dup 32 = if drop 2 boot else
    dup 33 = if drop 3 boot else
    dup 34 = if drop 4 boot else
    dup 35 = if drop 5 boot else
    dup 36 = if drop 6 boot else
    dup 37 = if drop 7 boot else
    dup 58 = if drop defpro @  boot else
    drop
    then then then then then then then then then then
    again
;

: init
    lit dpTick sectvec !
    bkey? init_screen hello       \ detect key down and init screen
    c006 pw@ p> 1+ c@ drive c!    \ save boot drive no
    mount  ?panic  	    	  \ mount rofs filesystem
    init_bpb			  \ load boot parameter block
    valid @ 0= if jmp setup then  \ if bpb is invalid then setup
    \ if boot up key is pressed or autoboot disable then goto menu
    ccbnoauto @ or if jmp menu then
    \ wait for autoboot
    timeout @ wait if jmp menu else 
	defpro @ boot drop jmp setup then
;






