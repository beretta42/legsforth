\ **********************************
\ Doubly Linked Circular Lists
\  handle points to head on list
\  handle = 0 if empty list
\  tail of list points to head
\  head of list points to tail
\ **********************************

0
cell field >next  \ pointer to next node
cell field >prev  \ pointer to previous node
struct dlist

{

: empty ( head -- h 1 )   \ helper leaves word if empty list
    dup @ dup 0= if 2drop pull drop exit then ;

public

\ This adds a node to the head of the list
: l_add ( node head -- ) \ add node to list
   dup @ if ( n h )
      2dup @ 2dup >prev @
      2dup ! swap >prev !
      2dup >prev ! swap !
      ! exit
   then 
      over dup dup !+ ! !
;


: l_rm ( head -- ) \ remove node at head
     empty                               \ exit if empty
     dup dup >next @ = if drop 0 else    \ skip if only one node 
       dup >prev @ swap >next @          \ get next and prev 
       2dup >prev !                      \ set next.prev = prev
       tuck swap >next !                 \ set prev.next = next
     then swap !                         \ set head = next

;
 

: l_next ( head -- ) \ move head to next item
    empty @ swap ! ;

\ This adds a node to the tail of the list
: l_queue ( node handle -- )
    tuck l_add l_next ;

\ Remove and return next node from list
: l_dequeue ( handle -- node )
    dup @ swap l_rm ;

: l_prev ( head -- ) \ move head to previous item
    empty >prev @ swap ! ;  

: l_head ( "name" -- ) \ create a list head
    create 0 , ;


\ l_dountil is a word that iterates over a list.
\ it takes an xt and a list head.  xt is executed for every
\ list node until xt returns true. When an xt returns a true flag
\ l_dountil returns a true flag and rotates the list head to point
\ to the affirmed node, else it returns false and leaves the head
\ unmodified.
\ The passed xt's is prototyped like follows: ( a -- f )
\ where  a is the address to the list's data
\ where  f is true to stop iterating, or false to continue


: l_dountil ( xt h -- a | 0 ) \ does xt for head node in h
   @ dup 0= if nip exit then dup push
   begin 

   ( xt 1 -- xt 1 f )
   2dup push push swap exec pull pull rot 

   if nip pull drop exit then
   @ dup r@ = until 2drop pull drop false
;


} 
