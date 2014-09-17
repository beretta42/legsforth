(
  Some Basics.

  extended runtimes.

)


: noop  ;		                \ no operation
: cell+	cell + ;			\ increments by a cell
: r@      rp@ cell+ @ ;                 \ copy top of return stack
: docreate  r@ cell+ pull @ exec ;      \ the action of a "created" word
: true  -1 ;
: NULL
: false 0 ;
: dovar pull ;	     			\ the variable "doer"
: char+ char + ;			\ increments by a char
: !+    over ! cell+ ; ( a x -- a )	\ stores and inc. a
: c!+   over c! char+ ;    	     	\ cstores and inc. a
: @+    dup cell+ swap @ ; ( a -- a x ) \ fetches and inc. address
: c@+   dup char+ swap c@ ; 	      	\ cfetches and inc. address

: 0=	if 0 else -1 then ;		\ tests for 0
: 0<    mint and if -1 else 0 then ; 	\ tests for negative
: neg	com 1+ ;			\ negate
: -	neg + ;				\ subtract

: rot	push swap pull swap ;	     	\ rotates top three cells
: 3drop	drop 	       	    		\ drops three cells
: 2drop drop drop ;			\ drops two cells
: 2dup	over over ;			\ dups two cells
: tuck	swap over ;			\ tuck TOS under NOS
: nip	swap drop ;			\ removes NOS from stack
: -rot  rot rot ;                       \ bury TOS three deep
: u<	2dup xor 0< if nip 0< exit then - 0< ;

: ekey  key dup emit ;     		\ gets a key and echoes 
: +!	tuck @ + swap ! ; ( x a -- )	\ increment var by x
: c+!   tuck c@ + swap c! ; ( c a -- )  \ increment cvar by x

: unloop pull pull drop push ;          \ unloop for exit
: dofield @ + ;                         \ for structures
: mv ( dest src count -- )              \ move count bytes from src to dest
  for c@+ push swap pull c!+ swap next 2drop ;
: =     - 0= ;                          \ equality test
: clear ( a u ) \ clear u bytes starting at address a
   for false c!+ next drop ;

done

