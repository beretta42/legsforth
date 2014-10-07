(

Lib OS drivewire commands

)


create dwMon ( -- a ) \ the DW lock/monitor
0 c, 0 ,

: dwLock ( -- ) \ obtain a drivewire lock
    dwMon lock ;
: dwRel ( -- ) \ release a drivewire lock
    dwMon release ;
: dwWait ( -- ) \ release but wait on lock
    dwMon lrel ;

: dw! ( c -- ) \ emits a byte to the becker port
    ff42 p!
;

: dw@ ( -- c ) \ gets byte from the becker port
    ff42 p@ ;

: dw? ( -- f ) \ returns true if there data waiting reading
    ff41 p@ ;


: dwType ( ca -- ) \ prints string to the becker port
    @+ for c@+ dw! next drop ; 
;

: dwKey ( -- c )  \ wait for key and return it 
   begin dw? 0= while yield repeat dw@ ;

: dwAccept ( a u -- ) \ accept u bytes from becker port into buffer
    for dwKey c!+ next drop ;

\
\ DW Transactions 
\

: dwInit ( -- ) \ initializes DW connection
    dwLock 49 dw! dwRel ;

: dwTerm ( -- ) \ terminate DW connection
    dwLock 54 dw! dwRel ;

: dwReset ( -- ) \ resets the DW connection
    dwLock ff dw! dwRel ;

: dwTime ( a -- ) \ get time from DW into buffer
    dwLock 23 dw! 6 dwAccept dwRel ;

: dw!! ( c c -- ) \ emits two bytes to becker port
    dw! dw! ;

: dwVopen ( u -- ) \ opens a virtual serial channel
    dwLock c4 dw!! 29 dw! dwRel ;

: dwVclose ( u -- ) \ closes a virtual serial channel
    dwLock c4 dw!! 2a dw! dwRel ;

: dwVwrite ( a u -- ) \ sends string to vport u
    dwLock 64 dw!! dup @ dw! dwType dwRel ;

: dwVput ( c u -- ) \ sends charactor to vport u
    dwLock 80 or dw!! dwRel ;

: dwVwriteln ( a u -- ) \ send line termed by cr to vport u
    dwLock
    64 dw!!             ( a )
    dup @ 1+ dw! dwType d dw!
    dwRel
;

: dwPoll ( -- b2 b1 ) \ polls dw for input, and gets input.
    dwLock
    43 dw! dwKey dwKey swap
    dwRel
;

: dwVread ( a u vport -- ) \ reads u bytes from DW into buffer a
    dwLock
    63 dw!! dup dw! dwAccept
    dwRel
;


(
DW4 doesn't follow it's published protocol as to
channel number passed to Vopen, Vclose accept accept
args as follows:
0-15  are virtual serial ports
16-31 are nineserver ports
a total of 32 ports.

However, the published protocol for dwPoll only
allows for port nos. 0-14 regular ports and 0-14
nineserver ports, for a total of 30!

)

0
3 field >VPMON
2 field >VPBYTES
struct VP


\ a table of chars received on ports
create vpdata here a0 allot a0 clear

: vpconv ( c -- a ) \ converts channel number to index into tables
    dup 40 and if 10 else -1 then swap f and + dup shl shl + vpdata + ;


: dwReader ( -- ) \ reader thread for dwVports
    {{ ." dwReader termed" cr 0 texit }} setexit
    begin
	\ wait tills there's something to do
	begin dwPoll dup 0= while 2drop 20 sleep drop repeat ( b2 b1 )
	dup 10 - if
	    dup vpconv lock
	    \ wait for empty buffer
	    dup vpconv >VPBYTES begin dup c@ while
		    over vpconv waiton repeat drop
	    \ save data to buffer
	    tuck dup vpconv >VPBYTES swap c!+ c! \ ( b1 )
	    vpconv release
	    3 sleep drop
	else 2drop then
    again
;


: dwAccept ( a vport -- u ) \ returns u bytes of data from vport in a
    dup push dup shl shl + vpdata + push ( r: v va )
    r@ lock
    \ wait till there's data
    r@ >VPBYTES begin dup c@ 0= while r@ waiton repeat ( a @byte1 )
    \ if multibyte then get bytes
    dup c@ 10 and if
	1+ c@ tuck r1@ dwVread
    else
	1+ c@ swap c! 1
    then
    0 r@ >VPBYTES c!
    pull release pull drop
;


create testb 102 allot

1 variable portv
: port portv @ ;

: test
    {{ ." test termed" cr 0 texit }} setexit
    begin testb cell+ port dwAccept testb ! testb type again
;

: test2
    begin
	ekey 
	dup 7e = if drop exit then
	dup port dwVput
	\	d = if exit then
	drop
    again
;

create termerb 102 allot

: termer ( -- ) \ task to setup terminals on port 6809
    1 dwVopen
    {{ 1 dwVclose 0 texit }} setexit
    s" tcp listen 6809" 1 dwVwriteln
    termerb cell+ 1 dwAccept termerb !
    begin
	termerb cell+ 1 dwAccept termerb !
    again
;

: init
    dwReset
    lit dwReader thread .
    lit termer thread .
 ;

: dw
    port dwVopen
    d parse dup type port dwVwriteln
;


