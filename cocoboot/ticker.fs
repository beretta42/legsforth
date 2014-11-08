
\
\ A progress bar that works outside BASIC
\

: dpPtr ( -- a ) \ pointer to memory
    dovar ;
: dpInit ( -- a ) \ reset value
    dovar ;
: dpStep ( -- a ) \ sub counter
    dovar ;
: dpSetLabel ( a -- ) \ draws bar's label
    5c0 p> 16 for 6060 !+ next drop
    @+ push >p 5c0 pull mv ;
: dpReset ( a u -- ) \ resets progress bar
    5e0 p> 16 for a0a0 !+ next drop
    5e0 dpPtr !
    dpInit ! ;
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


