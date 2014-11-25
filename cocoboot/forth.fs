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

This is a proglet that creates a mini-forth interpreter at boot

)

1400 setorg

dict
exposeall

include ../include/basics.fs
include ../include/forth.fs
include forth2.fs
include debug.fs

: bye
    ff 11a p!
    1 dofile
;

: wnf'
    3f emit type cr ;

: words
    latest begin dup while dup >name type space @ repeat drop cr ;  
: wds
    latest 20 for dup >name type space @ repeat drop cr ;

: main ( -- ) \ The Main Word
    1402 @ cp !     \ set CP to overlay's CP
    1404 @ dh !     \ set dictionary head
    memz r0 !
    memz 80 - s0 !
    cls
    slit str "Go Forth!" type cr
    0 11a p!
    lit wnf' lit wnf !
    jmp quit
;