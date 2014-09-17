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


\ **************************
\ These are some Basics
\ **************************


: cell+	cell + ;			\ increments by a cell
: char+ char + ;			\ increments by a char
: !+    over ! cell+ ; ( a x -- a )	\ stores and inc. a
: c!+   over c! char+ ;    	     	\ cstores and inc. a
: @+    dup cell+ swap @ ; ( a -- a x ) \ fetches and inc. address
: c@+   dup char+ swap c@ ; 	      	\ cfetches and inc. address
: cr	d emit ;
: type 
    @+ for c@+ emit next drop ;
: slit
    pull dup @+ + push ;
: 0= if 0 else -1 then ;
: 0<> 0= com ;
: 0< mint and 0<> ;
: neg com 1+ ;
: - neg + ;
: < ( n1 n2 -- f ) \ true if n1 is less than n2
   - mint and ;
: > ( n1 n2 -- f ) \ true if n1 is greater than n2
   swap < ;
: = - 0= ;
: <> - ;
: tuck dup push swap pull ;
: nip swap drop ;
: +! tuck @ + swap ! ; 
: dovar pull ;
: jmp pull @ push ;
: 2drop drop drop ;
: 2dup over over ;
: true -1 ;
: false 0 ;
: noop ( -- ) ;
: r@ ( -- x ) rp@ cell+ @ ;
: rot ( a b c -- b c a ) \ rotate 
   push swap pull swap ;
: -rot ( a b c -- c a b ) \ reverse rotate
   rot rot ;

\ 
\ uber-Simple Memory Allocation
\ 
: cp 4 ;
: here cp @ ;
: allot ( u -- ) 
    here + cp ! ;
: alloc ( u -- a )
    here swap allot ;



done
