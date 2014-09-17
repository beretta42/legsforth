(
This is an attempt to create a widget system for the CoCo2 diplay.
This should help cocoboot ?

     widget struct:
     off   size    what
     0	   2	   clicked ( -- ) 
     2	   2       selected ( -- )
     4	   2	   draw ( -- )
     6	   2	   keyed ( c -- )
     8	   2	   x pos
     a	   2	   y pos
     c	   2	   text
)



/ **********************************
/  Drawing Primitives
/ **********************************

400 constant scr_off   \ The screen offset

: putstring ( ca x y -- )  \ Print String to Screen
   20 * + scr_off + @+ for c!, next drop ;

