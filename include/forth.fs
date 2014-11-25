

\ *******************************
\ A Forth interpreter
\ *******************************

: cp0	17 ;	      			\ starting cp value
: cp'   cp0 ;				\ returns address of reset c0
: cp 	cp' ;  			        \ calls cp xt set above
: here	cp @ ;				\ current compilation pointer
: , 	here swap !+ cp ! ;		\ appends cell
: c,	here swap c!+ cp ! ;		\ appends char


: state dovar # -1 ;		   	\ state of compiler


: tib ( -- a ) \ address of text input buffer
    here 100 + ;
: bl?	21 - 0< ;			\ tests for whitespace
: word' ( -- ca ) \ get next word from input stream
	tib 0 !+ 
	begin ekey dup bl? while drop repeat
	begin c!+ 1 tib +! ekey dup bl? until 2drop tib ; 

: word  word' ; 

: s=	( ca ca -- f ) \ compares string for equality
  	@+ rot @+ rot over - if 3drop 0 exit then
	for c@+ rot c@+ rot - if 2drop 0 unloop exit then
	next 2drop -1 ;

     
( 
Dictionary Format

offset	size	what
0	2	link
2	2	xt
4	2	flags
6	?	ca of name

)

: dh		19 ;		\ dictionary head
: dp		dovar # 4000 ;   \ dictionary area pointer
: latest	dh @ ;		\ latest word
: >name		cell+ 		\ returns ca of entry
: >flag		cell+ 		\ returns address of flags of entry
: >xt		cell+ ; 	\ returns address of xt of entry

: dfind  ( ca lh -- da )	\ find dictionary struct, or 0 if not found
	@ begin dup while 2dup >name s= if nip exit then @ repeat nip ;

: find  ( ca -- ca 0 | xt 1 | xt -1 ) \ find and xt
  	dup dh dfind dup 0= if exit then
	nip dup >xt @ swap >flag @ if 1 else -1 then ;

: [		-1 state ! ; immediate \ turn compiler off

: ]		0 state ! ; \ turn compiler on


: ?dup ( x -- ? ) \ duplicates TOS if TOS is not zero
  dup if dup then ;

: s,  ( ca -- )           \ compile string
  @+ dup , for c@+ c, next drop ;


( There's two versions of header.  both create a
  name. "header'" compiles dictionary entries into
  the CODE space, for tight efficient code. The
  other, "header2" compiles the header to the
  Dictionary Area, set by the variable DP. 
)


: header' ( "name" -- )	    \ makes a header in the code area
    here latest , dh !
    here 0 dup , , 
    word s,
    here swap !
;


: header2 ( "name" -- )     \ makes a header in dictionary area
    here dp @ dup cp ! latest , dh ! 
    dup , 0 ,
    word s,
    here dp !
    cp !
;

\ Quit's default way of making a header, choose one from above
: header header2 ;

: :            ( -- )       \ make a definition
  header ] ;

: ; ( -- ) 
  lit exit , [ ;  immediate

: within ( a b c -- ) \ returns true if a is between b and c
     over - push - pull u< ;


: atou ( c -- x ) \ convert asci to a int - -1 on conversion err
     dup 2f 3a within if 30 - exit then
     dup 60 67 within if 57 - exit then
     drop -1 ;

: >num' ( ca -- n f )
  @+ over c@ 2d - 0= if 1- swap char+ swap -1 else 0 then -rot
  0 swap for shl shl shl shl swap c@+ 
  atou dup 0< if 2drop nip 0 unloop exit then
  rot + next nip swap if neg then -1 ;

: >num  >num' ;

: wnf' ( ca -- )
  3f emit d emit drop begin again ;

: wnf wnf' ;

: \ begin ekey dup d - 0= swap a - 0= or until ; immediate


: interpret ( -- ) \ interprets source until error or out of words
    begin
     	word dup @ 0= if drop exit then
        find ?dup if ( xt 1 | xt -1 )
	    0< 0= state @ or if exec else , then
	else ( ca )
            dup >num if 
	    	 nip state @ 0= if lit lit , , then
	    else
                 drop wnf exit
            then 
        then 
    again

: r0 dovar # 4000 ;
: s0 dovar # 3f00 ;

: quit'
  begin r0 @ rp! interpret again
 
: quit 
  quit' ;

: init
  latest >name @+ + dp ! \ set DP from latest
  quit
;

done
