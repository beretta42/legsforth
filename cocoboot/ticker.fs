
\
\ A progress bar that works outside BASIC
\

: dpPtr ( -- a ) \ pointer to memory
    dovar ;
: dpInit ( -- a ) \ reset value
    dovar ;
: dpStep ( -- a ) \ sub counter
    dovar ;  
: dpReset ( u -- ) \ resets progress bar
    5e0 p> 16 for a0a0 !+ next drop
    5e0 dpPtr !
    dpInit !
;
: dpInc ( -- )   \ increments progress bar
    af dpPtr @ p!  \ display new finished chunk
    1 dpPtr +!     \ inc ptr to next spot
;
: dpTick ( -- ) \ ticks the progress bar
    dpStep @ 1- dup 0= if
	dpInit @ dpStep !
	dpInc drop
    else
	dpPtr @ dup p@ 1+ swap p!
	dpStep !
    then
;

done


