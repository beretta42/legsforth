\ ------------------------------------------------------------------------
\ CoCoBoot - A Modern way to boot your Tandy Color Computer
\ Copyright(C) 2011 Brett M. Gordon beretta42@gmail.com
\
\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.
\
\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.
\
\ You should have received a copy of the GNU General Public License
\ along with this program.  If not, see <http://www.gnu.org/licenses/>.
\ ------------------------------------------------------------------------


\ *******************************
\ 
\  Very Simple Menu System
\ 
\ ******************************

include in.fs
include out.fs

: uctype ( ca -- ) \ emit string in upper case
   @+ for c@+ dup 61 > if 20 - then emit next drop ;


: 3! ( l h a -- ) \ store a 3 bytes quantity
    swap c!+ ! ;

: temp ( -- a ) \ temporary buffer
   dovar # 0 # 0 # 0 # 0

	    
: obj  ( -- a ) \ object variable
   dovar # 0 ;
: self  ( -- obj ) \ returns ourself
   obj @ ;
: msg ( obj o -- ) \ send message to object
   obj @ push swap dup obj ! + @ exec pull obj ! ;



\ Menu Item obj Struct
\ offset    length   what
\ 0	    2	     retreive data address xt
\ 2	    2	     select xt
\ 4	    2	     draw xt
\ 6	    2	     saved screen location
\ 8	    1	     ascii char to select
\ 9	    ?	     counted string text label

    

\ 
\ External Words
\ 

\ send select message to an object
\ returns true to redraw screen
: select ( obj -- f ) 
   2 msg ;
: draw ( obj -- ) \ send draw message to object
   4 msg ;
: getascii ( obj -- ) \ retrieve item's ascii
   8 + c@ ;

\ 
\ Internal Words
\ 

: data ( -- a ) \ object's data ptr
   self @ exec ;
: pos ( -- a ) \ object's position field
   self 6 + ;
: ascii ( -- c ) \ object's ascii char
   self 8 + c@ ;
: label ( -- ca ) \ object's counted string label
   self 9 + ;
: union ( -- a ) \ object's unioned data
   label @+ + ;

: pos! ( -- ) \ save current screen position
   88 pw@ pos ! ;
: pos@ ( -- ) \ restore object begining screen position
   pos @ 88 pw! ;

: redraw ( -- ) \ redraw method
    pos@ clsline pos@
    self draw
;

\ Boolean Object:
\  data is 16 bits, either true or false

: boolselect ( -- ) \ select method for boolean select
   data @ 0= data ! 
   redraw false
;   

: booldraw ( -- ) \ draw method for boolean select
    pos!
    label type
    data @ if slit str "YES" else slit str "NO" then type
;


\ List Object
\  data is index from 0 to max
\ Data extended from label field:
\ uint16  - number of strings
\ array of strings

: max ( -- u ) \ number of list items
   union @ ;
: list_select ( -- ) \ select method for list select
    data @ 1+ dup max = if drop 0 then data !
    redraw false
;

: list_draw ( -- ) \ select method for list draw
   pos!   
   label type
   union cell+ 
   data @ for @+ + next type
;

\ Large List Object
\ same data as a list object
\ just displayed and selected differently

: llist_draw ( -- ) \ method for llist draw
    list_draw ;

: itemdraw ( u -- ) \ draw item
    \ calc address of draw
;

: draw_pos ( c -- ) \ draw position
    440 data @ shl shl shl shl + p! ;
: llist_select ( -- f ) \ method for llist selection
    cls
    label type cr cr
    \ draw each item
    441 dup 88 pw! union @+ for 
       dup type swap 10 + dup 88 pw! swap @+ + 
    next 2drop 
    hide
    6a draw_pos
    \ process keystrokes
    begin key 60 draw_pos
      dup 3 = over d = or if drop true exit else
      dup 8 = data @ 0 > and if data @ 1- data ! else
      dup 9 = data @ union @ 1- < and if data @ 1+ data !
      then then then
      drop 6a draw_pos
    again
;
 

\ 8 bit number entry
\ data is a uint16

: bentry_draw ( -- ) \ draw method for num8 draw
   pos!
   label type
   data c@ bemit
;

: bentry_select ( -- ) \ draw method
   pos@ clsline pos@ label type
   temp dup 2 naccept >num data c!
   redraw false
;

\ 16 bit number entry
\ data is a uint16

: wentry_draw ( -- ) \ draw method for num8 draw
   pos!
   label type
   data @ wemit
;

: wentry_select ( -- ) \ draw method
   pos@ clsline pos@ label type
   temp dup 4 naccept >num data !
   redraw false
;

\ 24 bit number entry
\ data is a pointer to a 24 bit 
   

: lentry_draw ( -- ) \ draw method for num8 draw
   pos!
   label type
   data c@+ bemit @ wemit
;

: lentry_select ( -- ) \ draw method
   pos@ clsline pos@ label type
   temp dup 6 naccept >lnum data 3!
   redraw false
;


\ Menu Object
\ data field is a vector exec'd when selected
\ data extended from label field:
\  zero terminated array of object pointers

: ll_draw ( -- ) \ low-level draw
   cls label uctype cr cr
   union begin dup @ while @+ draw cr repeat drop
;

: menu_select ( -- ) \ select method
   data
   ll_draw
   begin
     hide
     key 
       dup 3 = if drop true exit then
     push union begin 
         dup @ 
     while 
        @+ dup getascii r@ = if select if ll_draw then else drop then 
     repeat 
        pull 2drop
   again
;	 


: menu_draw ( -- ) \ menu draw method
   pos!
   label type
;

\ Text Widget
\ data field is ptr to CA string
\ union data is a max size

: text_draw ( -- ) \ text draw method
    pos!
    label type
    data type
;

: text_select ( -- f ) \ text select method
    pos@ clsline pos@ 
    label type
    data union @ accept
    false
 ;

\ Button and Confirm Widget
\ data field is executed as vector if y is pressed



: confirm_draw ( -- ) \ confirm draw method
    pos!
    label type
;

: button_select ( -- f ) \ go do something on select
    data true ;

: confirm_select ( -- f ) \ confirm select method
    5e0 88 pw!  \ go to last row
    slit str "REALLY " type
    label uctype 3f emit
    key 59 = if data then 
    true
;


\ A stupid spinner progress indicator

: spintable
    # 5c21 # 2f2d
    
: spin_draw ( -- ) \ spinner draw method
    pos!
    label type 
    lit spintable self @ + c@ emit
;

: spin_select ( -- f ) \ spinner select method
    self @ 1+ 3 and self !
    redraw false
;

\ A simple progress bar


: progress_draw ( -- ) \ draw method
   pos!
   label type cr
   self @ for af emit next
;

: progress_select ( -- f ) \ select method
    union @ 1+ union cell+ @ over = 
    if drop 0 self @ 1+ self ! redraw then
    union !
    false exit

   

done

