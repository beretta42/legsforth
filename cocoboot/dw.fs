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

Drive Wire 4 Interface

)

include basics.fs
include ccbimp.fs

: dwemit ( c -- ) \ emits one char to dw
   sp@ >p 1+ 1 dwwrite drop ;
: dwwemit ( x -- ) \ emits one char to dw
   sp@ >p 2 dwwrite drop ;
: dwbget ( -- c ) \ get one char fr dw
   false sp@ 1+ p> 1 dwread 2drop ;
: dwwget ( -- x ) \ get one word fr dw
   false sp@ p> 2 dwread 2drop ;

: dwgetsec ( -- f ) \ gets sector
    d2 dwemit               \ send opcode
    drive c@ dwemit          \ send drive no
    0 dwemit	            \ send high byte lsn
    lsn >p 2 dwwrite        \ send low word lsn
    daddr @ >p 100 dwread   \ read sector data
    swap dwwemit            \ send chksum
    dwbget or               \ get result
;

done

