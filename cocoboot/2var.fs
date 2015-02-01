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

Double Words
values that are 4 bytes in length
on stack: lowword highword
in mem:   highword lowword

)

: 2@ ( a -- d ) \ fetches a double variable
   @+ swap @ swap ; 

: 2! ( d a -- ) \ stores a double variable
   swap !+ ! ;

: 2inc ( d -- d ) \  increment double by one
   swap 1 adc -rot + ;

: 2+ ( d1 d2 -- d ) \ adds d1 with d2
   rot + push adc swap pull + ;


done 


