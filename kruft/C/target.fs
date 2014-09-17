\ ***********************************
\  The Target Compiler
\ ***********************************


create tib 128 allot


: @+ ( a -- a x ) \ fetches a, increments a
   dup @ swap cell+ swap ;

: c@+ ( a -- a c ) \ fetches a, increment a
   dup c@ swap char+ swap ;

: c!+ ( a c -- a ) \ stores a, increments a
   over c! char+ ;

: bl? ( c -- f )   \ tests if C is whitespace
   21 u< ;

: astr  ( x ca -- ) \ append x to ca
    tuck @+ + ! cell swap +! ;

: castr ( c ca -- ) \ append c to ca
    tuck @+ + c! char swap +! ;

: word ( -- ca )
    0 tib !
    begin ekey dup bl? while drop repeat
    begin tib castr ekey dup bl? until drop tib ;

0 variable cpp   \ the code compile pointer pointer
0 variable dpp   \ the dictionary compile pointer pointer

: , ( x -- ) \ compile x to compile area
    cpp @ astr ;

: c, ( c -- ) \ compile c to compile area
    cpp @ castr ;

: s, ( ca -- ) \ compile string to compile area
    @+ dup , for c@+ c, next drop ;

: here cpp @ @+ + ;

: ba ( -- ba )  \ compile a dummy address
    here 0 , ;

: res ( ba -- ) \ resolve a back address
    here swap ! ;

: header ( ca -- da ) \ make a new dictionary pointer
    darea cpp ! \ switch to dictionary compile area
    here swap   \ put dictionary addresss on stack
    latest ,    \ compile link to latest dictionary address
    ba swap     \ put address of xt field on stack
    0 ,         \ compile flags
    swap s,     \ copy string
    carea cpp ! \ switch to code compile area
    res         \ resolve back address
;

: : ( "name" -- da ) \ starts compiling
    word header ] ;

: ; ( da -- ) \ finishes a word definition
    latest ! [ ; 
