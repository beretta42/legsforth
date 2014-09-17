\ ****************************************
\  Objects
\ ****************************************

method ( "name" -- ) \ attaches

ovariable move

method speak ( -- ) \ default speak
   ." blah, blah, blah" cr ;

method move ( -- ) 

Class - classes define objects and interfaces
methods: 
  public  - entry in VMT
  over    - overrides super class's VMT 



0 variable O     \ current object pointer


: speak ( o -- )
   O @ 0 + @ exec ;
: move ( o -- )
   O @ 2 + @ exec ; 
: die ( o -- )
   O @ 4 + @ exec ;


cat new lucy
dog new spot

: test
   lucy speak
   spot speak
;