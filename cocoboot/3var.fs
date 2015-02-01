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

These are some 3 bytes words
mostly used for the 24 bit values in the os9
rbf filesystem

)


: 3@ ( a -- d ) \ fetches a 3var as a double
   c@+ swap @ swap ;

: 3! ( d a -- ) \ stores a double as a 3var
    swap c!+ ! ;

: 3@+ ( a -- a d ) \ fetch a 3var and increments address
   dup 3 + swap 3@ ;


done 

