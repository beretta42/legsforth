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

These are words that are particular to the CoCoBoot project - 
They are not generic forth

)

include basics.fs


p: 40 mod ( -- a )      \ gets start of module addresses
p: 41 pw@ ( a -- x )    \ gets a word from p-space
p: 42 pw! ( x a -- )    \ writes a word to p-space
p: 43 call ( a -- )     \ calls module routine
p: 44 exem ( pxt -- )   \ executes the primitive xt
p: 45 key? ( -- c | 0 ) \ returns 0 if no key press or c if key
p: 46 bsw  ( x -- x )   \ byte swap TOS
p: 47 llioff ( -- )     \ turn off low-level interrupts
p: 48 toram ( -- )	\ move BASIC ROMS to RAM (for coco2)
p: 49 mv ( s d u -- )	\ move memory
p: 4a dwread ( a u -- chk f )   \ read from dw
p: 4b dwwrite ( a u -- ) \ write to dw
p: 4c modinit ( -- f )   \ init boot module
p: 4d modread ( d a -- ) \ read from boot module
p: 4e modterm ( -- )     \ terminate boot module
p: 4f modoff ( -- )	 \ get blocklock boot module constant
p: 50 adc ( x x -- c x ) \ add cells with carry

: mb ( -- o ) \ offset into memory area
    4500 ;

: >p ( a -- a ) \ convert address to p-space address
    mb + ;

: p> ( a -- a ) \ convert p-space address to vm address
    mb - ;


done

