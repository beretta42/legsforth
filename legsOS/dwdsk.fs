\ 
\   Some diskroutines
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

: dwInit ( -- ) \ initializes DW connection
    49 bkr! ;

: dwTerm ( -- ) \ terminate DW connection
    54 bkr! ;

: dwReset ( -- ) \ resets the DW connection
    ff bkr! ;


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


create buffer 42 allot \ buffer for recv from client
create dbuffer 256 allot \ disk receive buffer

: readth ( -- ) \ read disk thread
    listen
    dbuffer 4 for recvc drop
    alloc
;
   

: Server ( -- ) \ server thread
    listen
    begin
	buffer recvlstr bkrType
	bkrKey if 0 replyc else           \ return 0 to client on error
	    bkrKey drop bkrKey drop       \ dump chksum
	    lit readth thread dup replyc  \ launch thread
	    waitfor drop                  \ wait for thread to die
	then
    again
;
