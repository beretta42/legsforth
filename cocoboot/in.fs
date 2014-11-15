\ ------------------------------------------------------------------------
\ CoCoBoot - A Modern way to boot your Tandy Color Computer
\ Copyright(C) 2013 Brett M. Gordon beretta42@gmail.com
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


(

Basic Input words

)

include basics.fs


: astr ( c ca -- ) \ apends c to string
   tuck @+ + c! 1 swap +! ;

: bsstr ( ca -- )  \ removes char from string
   dup @ 1- swap ! ; 

: istr ( ca -- )   \ empties string
   0 swap ! ;

: ?exitf
    if drop false pull drop then ;
: digit? ( c -- f ) \ returns true if c is a valid hex digit
    dup 30 < ?exitf
    dup 46 > ?exitf
    dup 39 > over 41 < and ?exitf
    drop true ;

: cton ( c -- u ) \ ascii to number conversion
    dup 39 > if 37 else 30 then - ;

: nkey ( -- c ) \ gets numeric and control keys only
   begin key dup d = over 8 = or over digit? or 0= while drop repeat ;

: keyvec ( -- a ) \ key vector variable
   dovar key ;

: vkey ( -- c ) \ gets vectored key
   keyvec @ exec ;

: laccept ( ca u -- ) \ fills string from keyboard
   push dup istr
   begin 
     vkey 
     dup d = if 2drop pull drop 20 emit exit then
     dup 8 = if 
        over @ if emit dup bsstr else drop then
     else
	over @ r@ = if drop else
        dup emit over astr   
     then then
   again
;


: accept ( ca u -- ) \ fills string from keyboard with any charactors
   lit key keyvec ! laccept ;

: naccept ( ca u -- ) \ fills string from keyboard with only numeric chars
   lit nkey keyvec ! laccept ;

: >num ( ca -- u ) \ convert a string to a unsigned number
   @+ 0 swap for 
     shl shl shl shl 
     swap c@+ cton rot + 
   next nip ;


: dshl ( d -- ) \ shifts a double number one bit left
    shl over 0< if 1+ then swap shl swap ;
: d+ ( d u -- ) \ adds unsigned number to double
    push swap pull + swap ;
: >lnum ( ca -- d ) \ convert a string to a 24 bit unsigned number
    push 0 0 pull @+ for
      c@+ cton swap push push
      dshl dshl dshl dshl pull d+ pull
    next drop ;

done

