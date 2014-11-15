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
***************************************
Basic Output Words
***************************************
)

include ccbimp.fs

: cr ( -- ) \ cause a carrage return
    88 pw@ 20 + 1f com and 88 pw! ;


: space ( -- "space" ) \ emit a space
     20 emit ;

: bl? ( c -- f )  \ is c whitespace ?
     21 - 0< ;

: utoc ( u -- c ) \ converts digit to ascii
  dup a - 0< if 30 else 37 then + ;

: bemit ( c -- "char" ) \ fixed-width unsigned hex char print
    dup shr shr shr shr utoc emit f and utoc emit ;

: wemit ( u -- "word" ) \ fixed-width unsigned cell print
    sp@ dup c@ bemit char+ c@ bemit drop ;


: cls ( -- ) \ clear screen
    400 p> 100 for 2020 !+ next drop
    400 88 pw!
;

: clsline ( -- ) \ clear current line
   20 for 66 emit next ;

: hide ( -- ) \ hides cursor
   600 88 pw! ;

done 

