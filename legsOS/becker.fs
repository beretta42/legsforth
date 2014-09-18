\ 
\ Some routines for the "Becker Port".
\ 



: bkr! ( c -- ) \ emits a byte to the becker port
    ff42 p!
;

: bkrType ( ca -- ) \ prints string to the becker port
    @+ for c@+ bkr! next drop ; 
;

: bkr? ( -- f ) \ returns true if there data waiting reading
    ff41 p@ ;

: bkrKey ( -- c )  \ wait for key and return it 
   begin bkr? until ff42 p@ ;

: bkrAccept ( a u -- ) \ accept u bytes from becker port into buffer
    for bkrKey c!+ next drop ;

: dwTime ( a -- ) \ get time from DW into buffer
    23 bkr! 6 bkrAccept ;

: dwInit ( -- ) \ initializes DW connection
    49 bkr! ;

: dwTerm ( -- ) \ terminate DW connection
    54 bkr! ;

: dwVopen ( u -- ) \ opens a virtual serial channel
    c4 bkr! bkr! 29 bkr! ;

: dwVclose ( u -- ) \ closes a vsc
    c4 bkr! bkr! 2a bkr! ;

: dwVwrite ( a u -- ) \ sends string to vport u
    64 bkr! bkr! dup @ bkr! bkrType ;

: dwVwriteln ( a u -- ) \ send line termed by cr to vport u
    64 bkr! bkr!             ( a )
    dup @ 1+ bkr! bkrType d bkr! ;


: r1@ ( -- a ) \ fetches the second item on return stack
    rp@ cell+ cell+ @ ;

: r2@ ( -- a ) \ fetches the third item on return stack
    rp@ cell+ cell+ cell+ @ ;

: msgcount ( u -- u ) \ finds out many block it will take to this u bytes
    dup 6 for shr next swap 3f and if 1+ then ;

: sendlstr ( ca k -- ) \ send a long string to thread
    push alloc push ( r: t d )
    dup @ msgcount for
    dup r1@ deref swap 40 mv
    r1@ r2@ sendmsgc drop
    40 +
    next pull close pull 2drop ;
;


: recvlstr ( ca -- ) \ receives a long string
    recvmsg 0 replyc 2dup deref 40 mv close dup @
    msgcount 1- for 40 + dup
    recvmsg 0 replyc 2dup deref 40 mv close 
    next drop
;



create buffer here 400 allot 400 clear


: dwTest ( -- )  \ this is a thread
    ." DW test thread started" cr
    dwInit
    1 dwVopen
    listen
    begin
	buffer dup recvlstr 1 dwVwriteln 
    again
;
