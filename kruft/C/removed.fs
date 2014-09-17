\
\  This code has been removed
\


create dtab 4 cells allot
0 variable *latest

\ : newdh  ( ca -- dh ) cell+ c@ shr shr shr shr shr cells dtab + ;


: newdh  ( ca -- dh )  \ hash function for deriving the hash bucker 
   0 swap @+ for c@+ rot + swap next drop 3 and cells dtab + ;


: new:	( "word" -- )  \ Creates a name
  here *latest ! 
  ba ba 0 , word dup s, dh swap pp then tuck @ over ! swap ! ] ;

: add  ( de dh -- ) \ add a dictionary entry to list head
  2dup @ swap ! ! ;

0 variable temp

( This code plays with the outer interpreter so its wrapped in quotes
  so it'll be executed and then removed from the instruction stack
  It should be immediate but the arranging of the dictionary will render 
  immediate mode unusable until all the code changes are complete.
)

[[
  \ clear hash table
  dtab 0 !+ 0 !+ 0 !+ 0 !+ drop
  \ swap order of word list
  latest begin dup @ swap temp add dup 0= until drop
  \ add words to hashing table
  temp @ begin dup @ swap dup dup *latest ! >name newdh add dup 0= until drop
  lit newdh lit dh !
  lit new: lit : !
]] dup exec cp !


[[ *latest @ ]] is latest

: domarker   ( -- ) \ domarker restores the dictionary / CP to a previous state
  pull 
  @+ *latest !         \ restore latest pointer
  @+ dtab 3 cells + !  \ restore fourth hash table head
  @+ dtab 2 cells + !  \ restore third hash table head
  @+ dtab 1 cells + !  \ restore second hash table head
  @+ dtab !            \ restore first hash table head
  @+ cp !		\ restore CP
  push	     	     	\ put return address back
;

: marker ( -- ) \ creates a word to restore the state of the machine 
  here		                 \ push CP
  dtab 4 for @+ swap next drop  \ push bucket 0-3
  latest      	                 \ push latest
  : ^ domarker 6 for , next pp ; 
;

: words ( -- ) \ prints the dictionary
  dtab 4 for
       dup begin @ dup while dup >name type space repeat drop
  cr cr cell+ next drop
;
  
