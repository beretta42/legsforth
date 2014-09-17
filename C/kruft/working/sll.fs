\ *******************************
\ Singularly Linked Lists
\ *******************************

{

  \ private structure parts
  0
  cell field >next  \ pointer to next node


  : empty ( head -- h 1 )   \ helper leaves word if empty list
      dup @ dup 0= if 2drop pull drop exit then ;

public

  : l_add ( node head -- ) \ add node to list head
     2dup @ swap ! !
  ;

  : l_rm ( head -- ) \ remove node at head
       empty @ swap ! ;

  : l_top ( head -- ) \ returns node at top of list
       @ ;

  \ Remove and return next node from list
  : l_dequeue ( handle -- node )
      dup @ swap l_rm ;

  : l_head ( "name" -- ) \ create a list head
     0 variable
  ;

  \ The passed xt's is prototyped like follows: ( a -- f )
  : l_dountil ( xt h -- n | 0 ) \ interate through a list
    begin @ dup 
    while 2dup push push swap exec 
      if pull drop pull exit then 
      pull pull 
    repeat nip
  ;

  \ count number of nodes in list
  : l_count ( h -- u )
    0 swap {{ drop 1+ false }} swap l_dountil drop ;

  struct list

} 