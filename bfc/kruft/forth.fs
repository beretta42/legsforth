\
\  This is an extremly simple 
\


: dovar pull ;				\ variable method
: cp 	dovar ;				\ compilation pointer
: here	cp @ ;				\ current compilation pointer
: allot here + cp ! ;			\ allocates cells 
: , 	here ! cell allot ;		\ appends cell
: c,	here c! char allot ;		\ appends charactor
: 0=	if 0 else -1 then ;		\ tests for 0
: com	-1 xor ;			\ compliment
: neg	com 1+ ;			\ negate
: -	neg + ;				\ subtract
: bl?	lit 33 - 0< ;      		\ tests for whitespace
: rot	push swap pull swap ;		\ rotates top three cells
: 3drop	drop				\ drops three cells
: 2drop drop drop ;			\ drops two cells
: 2dup	over over ;			\ dups two cells
: tuck	swap over ;			\ tuck TOS under NOS
: nip	swap drop ;			\ removes NOS from stack
: cell+	cell + ;			\ increments by a cell
: char+ char + ;			\ increments by a char

: word	( -- ca ) \ gets next word from input stream
	here lit 0 , here
	begin key dup bl? while drop repeat
	begin c, key dup bl? until drop	
	here swap - over !
	dup cp ! ;
		
: c2s ( ca -- a u ) \ converts c-string to stack string
	dup cell+ swap @ ;

: type 
    c2s for dup c@ emit char+ next drop ;

: unloop
    pull pull drop jmp
    
: s=	( ca ca -- f ) \ compares strings for equality
	c2s rot c2s rot over - if 3drop lit 0 ; then
	for 2dup c@ swap c@ - if 2drop lit 0 unloop ; then
	char+ swap char+ next 2drop -1 ;
     
( 
Dictionary Format

offset	size	what
0	2	link
2	2	xt
4	2	flags
6	?	ca of name

)

: dh		dovar ;		\ dictionary head
: latest	dh @ ;		\ latest word
: >name		cell+ 		\ returns ca of entry
: >flag		cell+ 		\ returns address of flags of entry
: >xt		cell+ ;		\ returns address of xt of entry

: find  ( ca -- da )		\ find dictionary struct, or 0 if not found
	latest begin 2dup >name s= if nip ; then @ dup 0= until nip ;

: exec	( xt -- )		\ execute xt
	jmp	

: state		dovar 0		\ state of compiler flag

: [		0 state ! ; immediate

: ]		1 state ! ; 

: ok  		lit 111 emit lit 107 emit lit 10 emit ;

: u.	( x -- ) \ print unsigned number	
  dup lit f and utoc swap shr shr shr shr dup if u. then drop emit ;  


: quit
    begin word dup find dup if 
	    nip dup >xt @ swap >flag @ state @ or if exec else , then
	else
	    drop lit 63 emit lit 32 emit type lit 10 emit
	    0 state ! 
	then
	ok
    again







