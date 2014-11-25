
: ba   here 0 , ;  ( -- a ) \ compiles 0 and leave address on stack
: 'then here swap ! ; ( a -- ) \ complete a ba
: 'if   lit 0bra , ba ; ( -- a )
: 'again lit bra , , ; ( a -- )
: 'repeat swap 'again 'then ;

: if 'if ; immediate ( a -- )     \ immediate version of above
: then 'then ; immediate ( a -- ) \ immediate version of above 
: else lit bra , ba swap 'then ; immediate ( -- a ) \ compiles a "else"
: begin here ; immediate ( -- a ) \ compiles a begin
: again 'again ; immediate ( a -- ) \ completes a begin
: until lit 0bra , , ; immediate ( a -- ) \ complete a begin
: while 'if ; immediate ( a -- a ) \ starts a while loop 
: repeat swap 'again 'then ; immediate ( a -- ) \ ends a while loop
: for   lit push , here lit dofor , ba ; immediate ( x -- ) \ for loop
: next  'repeat ; immediate ( -- ) \ end for loop


: ptib         \ returns address of general parse buffer
   tib 40 + ;  \ locate past the word buffer
: parse \ ( c -- ca ) gets chars from input until char c is found
  	ptib 0 !+ 	
	begin over ekey tuck - while 
      	c!+ 1 ptib +! repeat 3drop ptib ;
: (  \ embedded comment
     lit 29 parse drop ; immediate
: delim ( -- c ) \ drops white space until a delimiter is found
    begin ekey dup 20 < while drop repeat ;
: 'str ( -- ca )
    delim parse ; 
: str ( -- ca )
    lit slit , 'str s, ; immediate
: .str lit slit , 'str s, lit type , ; immediate

: forget ( -- ) \ forgets last entry
    latest dup @ dh ! dp ! ;


done

