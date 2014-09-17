\ ******************************
\ Dynamic Memory Allocation
\ ******************************

{

0 
cell field >next
cell field >flags
struct mlist

: size ( a -- u ) \ returns effective size of chunk
   dup @ swap - mlist - ;

: >data ( a -- a ) \ return chunk data field address
   mlist + ;

: >ca ( a -- a ) \ converts a data field address to chunk address
   mlist - ; 

: mark ( a -- ) \ marks chunk as used
   >flags dup @ mint or swap ! ;

: unmark ( a -- ) \ marks chunk as free
   >flags dup @ mint com and swap ! ; 

: used? ( a -- f ) \ returns true if chunk is used
   >flags @ 0< ;

: free? ( a -- f ) \ returns true if chunk is free
   used? 0= ;

: last? ( a -- f ) \ returns true if chunk is last in list
   @ @ 0= ;

: nconsol ( a -- ) \ consolidates chunk with next chunk
   \ test to see if we can consolidate
   dup last? if drop exit then
   dup dup used? swap @ used? or if drop exit then 
   \ actually consolidate
   dup @ @ swap ! ;   

: split ( u a -- ) \ split chunk to u bytes
   tuck + mlist +    \ calc new chunk address ( a n )
   dup unmark        \ mark new chunk as free ( a n )
   over @ over !     \ init new chunks next field ( a n )
   swap !            \ mod parent chunk's next field
;

: find ( u a1 -- a2 ) \ find node big enough for u bytes
   begin 2dup size u< over free? and if nip exit then 
   dup last? 0= while @ repeat 2drop false ;

: compact ( a -- ) \ compacts heap list
   dup last? if drop exit then 
   dup @ compact nconsol ;

public

\ allocate u bytes of data on a heap starting with
\ node address a1
: alloc ( u a1 -- a2 ) 
   2dup find  ( u a a )
   \ if no chunk big enough then compact
   \ and try again
   dup 0= if drop dup compact 
      2dup find ( u a a )
      dup 0= if nip nip exit then 
   then
   \ mark chunk as used
   nip dup mark  
   \ split chunk if big enough
   2dup size swap - mlist cell+ u> if 2dup split then 
   \ return data field
   nip >data
;


\ dallocate dynamic memory a
: free ( a -- ) 
   >ca dup unmark nconsol
;

}

\ allots and inits  named chunk
: dchunk ( u "name" -- ) \ initialize chunks
   create ba false , swap allot here swap ! false , ; 
