\ ******************************
\ Multiply and Divide
\ ******************************

: d+ ( d d -- d ) \ adds two double together
   rot + push tuck + swap over u> pull swap - ;
 
: dneg ( d -- d ) \ negate a double
   com swap neg tuck 0= - ;

: s>d  ( n -- d ) \ converts a single to a double
   dup 0< if -1 else 0 then ;
 
: r@ ( -- x ) \ copies the top of the return stack
   rp@ cell+ @ ;

: r2@ ( -- x ) \ copies the second to the top of return stack
   rp@ cell+ cell+ @ ;

\ do ( limit index -- )
{
 : d2*+ ( ud n -- ud+n c ) \ unsigned mixed double add w/ carry
    over mint and push push 2dup d+ swap pull + swap pull ;
 : /modstep ( ud c u -- ud c u )
    push over r@ u< 0= or if r@ - 1 else 0 then d2*+ pull ;
public
 : um/mod  ( ud u -- m q ) \ unsigned mixed divide
    0 swap lit [ 8 cells 1+ , ] for /modstep next drop swap shr or swap ;
}

: um* ( u u -- d ) \ unsigned mixed multiply
   push 0 0 rot for r2@ 0 d+ next pull drop ;

: m* ( u u -- d ) \ signed mixed multiply
   2dup 0< and push 2dup swap 0< and push um* pull - pull - ;

: * ( u u -- u ) \ signed multiply
   um* drop ;

: fm/mod ( d1 n1 -- m q )
   dup push dup 0< if neg push dneg pull then
   over 0< if tuck + swap then um/mod pull 0< if swap neg swap then ;

: /mod  ( n1 n2 -- m q )
   push s>d pull fm/mod ;

: / ( n1 n2 -- q )
   /mod nip ;

: mod ( n1 n2 -- m )
   /mod drop ;

.( mul.fs Done. ) cr

