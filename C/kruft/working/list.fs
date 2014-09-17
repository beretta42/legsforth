\ **********************************
\ Doubly Linked Circular Lists
\  handle points to head on list
\  handle = 0 if empty list
\  tail of list points to head
\  head of list points to tail
\ **********************************
\   List Node Structure
\   offset   size     what 
\   0        2        next node
\   2        2        previous node
\   4        ???      node data

{

: empty? ( head -- h 1 f ) \ returns true if list is empty
    dup @ dup @= ;

: empty ( head -- h 1 )   \ helper leaves word if empty list
    empty? if 2drop pull drop exit then ;

public

: l_add ( node handle -- ) \ add node to list
   empty? if ( n h 1 )
      drop over dup 2dup ! \ new.next = new
      cell+ !              \ new.prev = new   
      ! exit		   \ head = new
   then
      dup @ rot push push
      2dup !               \ 1.next = new
      over cell+ !         \ new.prev = 1
      pull 2dup cell+ !    \ 2.prev = new 
      over !               \ new.next = 2
      pull !               \ head = new
;

: l_next ( head -- ) \ move head to next item
    empty @ swap ! ;

: l_prev ( head -- ) \ move head to previous item
    empty cell+ @ swap ! ;  

: l_new ( -- node ) \ allocates space for a node
    here dup dup , , ;

: l_head ( "name" -- ) \ create a list head
    create 0 , ;

: l_data ( h -- a ) \ returns data field address of first node
   @ 2 cells + ;

\ does xt for each node in list until xt returns true
\  xt is this:  ( x a -- f ) where a is address of node data
\  h is set to the node that xt returns true or 0 if no xt return true
\  or list is empty

: l_doeachuntil ( x xt h -- )
   \ test for empty list
   empty? if 3drop drop exit then push 

   \ execute xt
   begin 3dup l_data swap exec
   
   \ return if xt returns true 
   if -rot pull 3drop drop exit then
   
   \ move h to next node
   dup l_next
   
   \ stop loop if we've reached the first node
   dup @ r@ = until

   \ clean up
   pull 3drop drop ;
 
}  


\ prints list of node names
: list ( lh -- )
   [[ >name type cr false ]] swap l_doeachuntil drop ;

\ find node on a list by name, returns data pointer
: lfind ( ca lh -- a ) 

