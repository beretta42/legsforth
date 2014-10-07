\ 
\ This is the core of Legs Forth
\ 

\ set our dp to end of lastest definition
latest >name @+ + dp !

c000 cp !  \ compile this forth to e000
\ f000 dp ! \ compile this forth's dict to f000

: noop    ;				 \ a no operation
: cr	  d emit ;			 \ prints a cr
: space   20 emit ;			 \ prints a space
: ba	  here 0 , ;   	      	    	 \ puts a back reference on stack
: imm     1 latest >flag ! ;		 \ mark latest word  immediate         	
: then	  here swap ! ; imm		 \ then	
: '					 \ find a name
          word find 0= 0bra [ ba ] 
	  wnf quit then ;
: pp	  ' , ; imm	                 \ compile word (ignoring imm flag)
: ^       lit lit , pp pp lit , , ; imm	 \ compile word ( later )
: if	  ^ 0bra ba ; imm     	    	 \ if	
: else	  ^ bra ba swap pp then ; imm	 \ else	
: begin   here ; imm	   	  	 \ begin	
: again   ^ bra , ; imm		         \ again
: until   ^ 0bra , ; imm       		 \ until
: while   pp if ; imm			 \ while
: repeat  swap pp again pp then ; imm	 \ repeat
: for	  ^ push here ^ dofor ba ; imm	 \ for
: next	  pp repeat ; imm     	   	 \ next
: r@      rp@ cell+ @ ;         \ copy top of return stack


: cell-	  cell - ;    			 \ decriment TOS by a cell

: cells   shl ;


\ ( a x -- a ) push a software stack 
: -!      swap cell- tuck ! ;            

\ ( a b c -- b a c ) swap stack items 1 and 2
: 12swap  rot swap ;

\ ( a b c -- c b a ) swap stack items 0 and 2
: 02swap  -rot swap ; 

: char? \ returns the ascii code for first letter in next word
  ^ lit word @+ drop c@ , ; imm

: ptib         \ returns address of general parse buffer
   tib 40 + ;  \ locate past the word buffer

: parse \ ( c -- ca ) gets chars from input until char c is found
  	ptib 0 !+ 	
	begin over ekey tuck - while 
      	c!+ 1 ptib +! repeat 3drop ptib ;

: (  \ embedded comment
     char? ) parse drop ; imm

: slit	  pull dup @+ + push ;       	 \ puts a string literal on stack
: "       char? " parse ;                \ makes a immediate mode string
: p"      here " s, ;                    \ compiles a string
: s"	  ^ slit " s, ; imm	         \ comiles a string literal
: n"	  " @+ for c@+ c, next drop ; 	 \ compiles just string bytes

: defer   ^ noop ; imm   	         \ defers a word's definition

: [is] ( new old -- )                    \ old xt's action is replace with new xt
    swap
    !+ lit exit swap ! ;
: is ( xt "name" -- )
     ' [is] ;


: forget ( -- ) \ drops last defined name
  dh @ dup @ dh ! dp ! 
;

: { latest dp @ ;
: export latest dp @ ;
: }
    rot 2dup - push swap 
    dp @ over - mv
    dp @ r@ - dp !
    latest r@ - dh !
    latest begin 2dup @ - while dup @ r@ - tuck swap ! repeat
    pull drop nip !
;


{

: xlit 	pull @+ over + push ;

export

: [[      here ]    ;                    \ starts a quote

: ]]      pp ; ; imm                     \ ends a quote

: {{  \ strart compiled quote
 	^ xlit ba ; imm

: }}  \ resolve compiled quote
	^ exit here cell - over - swap ! ; imm

}

: type
    @+ for c@+ emit next drop ;

: ."	  pp s" ^ type ; imm   		 \ prints a string literal
: .(	  char? ) parse type ; imm	 \ prints a string literal

: utoc ( u -- c ) \ converts digit to ascii
  dup a - 0< if 30 else 57 then + ;

: u.	( x -- ) \ print unsigned number	
  dup f and utoc swap shr shr shr shr ?dup if u. then emit ;


: r0 dovar ;
: s0 dovar ; 

: .       u. space cr ;
: depth	  sp@ s0 @ swap - shr ;


: bemit ff and dup shr shr shr shr utoc emit f and utoc emit ;
: wemit sp@ dup c@ bemit char+ c@ bemit drop ;

: dump	  ( a -- )			 \ dump memory 
   cr dup wemit ." :" cr
     8 for
        8 for
	   dup c@ bemit space char+
        next
        8 -
        8 for
           dup c@ dup bl? if drop 2e emit else emit then char+
        next cr
     next
  cr drop
;

: cls ( -- ) \ clears screen
   1b emit ." [2J" ;

\  
\  Replace the reset vector with
\  something nicer
\


\
\ Replace WNF with a nicer version
\

\ [[ cr ." *** Word Not Found: " type cr bye ]] is wnf

: interact ( -- ) 
\   lit pp wnf {{ cr ." *** Word Not Found: " type cr quit }} [is] ;
\    {{ cr ." *** Word Not Found: " type cr quit }} lit wnf [is] ;
    {{ cr ." *** Word Not Found: " type cr }} lit wnf [is] ;


: =     xor 0= ;  \ compares value to equality
: <>	= com ; \ compares value for inequality
: u<    2dup xor 0< if nip 0< exit then - 0< ;
: u>    swap u< ;

\ Signed Cell Compares
: <    2dup xor 0< if drop 0< exit then - 0< ;
: >    swap < ;
: >=   < com ;
: <=   2dup = -rot < or ;

\
\ for a *slightly* better interactive experience
\ make backspace do something
\ This replaces "word"
\

: bl? 21 u< ;  \ this is used to make key that return -1  work!
\ ( -- ca ) \ get next word from input stream
[[
        tib 0 !+ 
	begin ekey dup bl? while drop repeat
	\ exit with empty buffer if source closed
	dup 0< if 2drop tib exit then 
	begin 
	      dup 8 = here @ and if 
	      	  drop 1- -1 tib +!
	      else	
	          c!+ 1 tib +! 
              then
	      ekey dup dup dup bl? swap 8 <> and swap 0< or
	until 2drop tib 
]] is word

: docreate    \ runs xt ( addr -- ) mem: docreate xt data....
   r@ cell+ pull @ exec ;
    
: create  ( "name" -- ) ( -- a ) \ creates a name
    header ^ docreate ^ exit ;

: does> ( -- ) \ resolves the xt address
   pull latest >xt @ cell+ ! ;

: variable ( x "name" -- ) \ create a variable "name" init'd to x
  create , ;

: constant ( x "name" -- ) \ create a constant "name" init'd to x
   create , does> @ ;

: allot  ( x -- ) \ allots x bytes to 
   here + cp ! ;

: words latest begin dup while dup >name type space @ repeat drop cr ;
: .w latest 10 for dup >name type space @ next drop cr ;

\ ******************************************
\ Structures
\ ******************************************

: field ( u u -- u ) \ defines a field in a structure
   over + swap create , does> @ + ;

: struct ( u -- ) \ defines a structure
   constant ; 


: .d ( -- "depth" ) \ print stack depth
  depth . ;

: .s ( -- "stack" ) \ prints stack
    sp@ push depth dup shl cell- pull + swap
    for dup @ u. space cell- next drop ;

: rfind ( a -- de ) \ find dict entry containing address a
    push latest begin dup >xt @ r@ > while @ repeat pull drop ;


: .st
    dup 0 = if ." RUN   " else
    dup 1 = if ." ZOMBIE" else
    dup 2 = if ." SLEEP " else
    dup 3 = if ." WAIT  " else
    dup 4 = if ." STOP  " else
    dup 5 = if ." MON   " else	    
    then then then then then then
    drop
;

\  Prints task stuff
 : .t  ( o -- )
     oderef
     ." OID   " dup >OID c@ u. cr
     ." PAR   " dup >PAR c@ u. cr
     ." ST    "  dup >ST c@ .st cr
\     ." CST   " dup >CST c@ u. cr
\     ." WAIT  " dup >WAIT c@ u. cr
     ." TIMER " dup >TIMER @ u. cr
     ." SP    " dup >SP @ u. cr
     ." RP    " dup >RP @ u. cr
     ." IP    " dup >IP @ u. cr
     drop
 ;

 : helper ( a -- f )
     dup a2k 0< if drop false exit then
     dup >PAR c@ tp@ >OID c@ - if true else dup a2k then 
     bemit space
     dup >OID c@  bemit space
     dup >PAR c@  bemit space
     dup >ST c@  .st    space
\     dup >CST c@  bemit space
\     dup >WAIT c@ bemit space
     dup >TIMER @ wemit space
     cr drop false
;

    
: ps
    ioff
    lit helper runners @ map drop
    lit helper sleepers @ map drop
    ion
; 



0 variable secs
0 variable mins

: csi ( -- ) \ emits csi
    1b emit ; 

: .c ( -- ) \ prints the clock nicely
    csi ." [s"
    csi ." [H"
    csi ." [1m"
    csi ." [33m"
    ."  "
    mins @ u. ." :" secs @ u.
    ."  "
    csi ." [39m"
    csi ." [0m"
    csi ." [u"
;

: clocker ( -- ) \ This ticks the above variables
    4 sleep drop
    {{ begin 100 sleep drop .c again }} thread drop
    begin
	3c sleep drop
	secs @ 1+ dup 3c = if
	    drop 0 secs ! 1 mins +!
	else
	    secs !
	then
    again
;



: sleepy {{ begin 80 sleep drop again }} thread ; 
: ichy {{ begin 200 sleep drop ." tick!" cr again }} thread ; 


: launch
    {{
    1 tp@ >PAR c!    \ this should be in kinit...
    rp@ r0 !
    sp@ s0 !
    0 0	!
    ." LegsOS kernel started" cr
    begin lit interpret catch
	dup 0< if ." SysErr: " dup .  texit then drop
	r0 @ rp!
    again
    }} kinit
;


6000 cp !
d000 dp !

' launch 0 !

interact

