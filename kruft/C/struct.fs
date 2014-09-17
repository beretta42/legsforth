\
\ Structures
\

: struct ( x -- ) constant ;
: field ( x x -- x ) 
     over + swap create , does> @ + ; 


0
cell field >next
struct list


: l_add ( na ha -- ) \ add node to list 
   2dup @ swap ! ! ;
: l_rm  ( ha -- ) \ remove node from list
   dup @ @ swap ! ; 
: l_find ( xt ha -- na ) \ interate through list until xt returns true
   begin @ dup while 2dup push push swap exec 
   if pull drop pull exit then
   pull pull repeat nip exit ;


list
cell field >prev
struct dlist

: dl_add ( na ha -- ) \ add node to list
   2dup @ >prev @ swap >prev !
   

  


single add:
   head = n
   n.next = 1

double add:
   head = n
   n.next = 1
   n.prev = 1.prev
   1.prev = n
   1.prev.next = n


queue - 
  add to head
  remove from tail
stack - 
  add to head
  remove from head




names 
   single list
dynmem
   single list
vocabs
   single list
compile areas
   extends
local names
   single list
filesystems
   double list
mounts
   double list
files
   double list
input stack
   single list
processes
   double list
