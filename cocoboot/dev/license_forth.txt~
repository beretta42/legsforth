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


\ 
\ Debuging Words
\ 


: dump	  ( a -- a+0x40 )			 \ dump memory 
   cr dup wemit space 3a emit cr
     8 for
        8 for
	   dup c@ bemit char+
        next
        8 - space
        8 for
           dup c@ dup bl? if drop 2e emit else emit then char+
        next 
     cr
     next
  cr 
;

: pdump	  ( a -- )			 \ dump memory 
   cr dup wemit space 3a emit cr
     8 for
        8 for
	   dup p@ bemit char+
        next
        8 - space
        8 for
           dup p@ dup bl? if drop 2e emit else emit then char+
        next 
     cr
     next
  cr 
;
: depth ( -- u )
   sp@ memz 80 - swap - shr ;

: .d ( -- ) \ print depth
   depth wemit ;

: loop ( -- ) \ wait forever
   begin key emit again

done


