\ 
\   Some diskroutines
\



: bkr! ( c -- ) \ emits a byte to the becker port
    ff42 p!
;


: bkrSend ( a u -- ) \ sends bytes to becker port
    for c@+ bkr! next drop ;
;

: bkrType ( ca -- ) \ prints string to the becker port
    @+ bkrSend ; 
;

: bkr? ( -- f ) \ returns true if there data waiting reading
    ff41 p@ ;

: bkrKey ( -- c )  \ wait for key and return it 
    \    begin bkr? 0= while 2 sleep drop repeat ff42 p@ ;
    begin bkr? until ff42 p@ ;

: bkrAccept ( a u -- ) \ accept u bytes from becker port into buffer
    for bkrKey c!+ next drop ;

: dwInit ( -- ) \ initializes DW connection
    49 bkr! ;

: dwTerm ( -- ) \ terminate DW connection
    54 bkr! ;

: dwReset ( -- ) \ resets the DW connection
    ff bkr! ;

: msgcount ( u -- u ) \ finds out many block it will take to this u bytes
    dup 6 for shr next swap 3f and if 1+ then ;

: r1@ ( -- a ) \ fetches the second item on return stack
    rp@ cell+ cell+ @ ;

: r2@ ( -- a ) \ fetches the third item on return stack
    rp@ cell+ cell+ cell+ @ ;


create dargs 4 allot
0 variable cksum

: dwRead ( a -- f ) \ read sector into buffer	 
    d2 bkr! dargs 4 bkrSend
    dup 100 bkrAccept
    0 cksum !
    100 for c@+ cksum +! next drop
    cksum dup c@ bkr! 1+ c@ bkr!
    bkrKey
;


: dwWrite ( a -- f ) \ write buffer to disk
    57 bkr! dargs 4 bkrSend
    0 cksum !
    100 for c@+ dup cksum +! bkr! next drop
    cksum c@+ bkr! c@+ bkr! drop
    bkrKey
;


create testb 100 allot

