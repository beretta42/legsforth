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

Read Only File System

A stupid-simple filesystem designed to pack data
a bit tighter on a small volume.

)


: super ( -- a ) \ address of super block
    dovar # 0 ;
: sectvec ( -- a ) \ xt of sector load/save callback
    dovar noop ;

: mount ( -- f ) \ mount filesystem
    100 alloc super !  \ allocate super block copy
    super @ daddr !    \ set data
    0 lsn ! 	       \ set lsn
    read 	       \ read in data
    super @ @ 4247 -   \ compare magic bytes
    or if true else false then ;


: load ( addr u -- f ) \ load slot u to address
    shl shl shl 8 + super @ +
    @+ lsn !
    @ swap daddr !
    for read if pull drop true exit then 
    sectvec @ exec
    lsn @ 1+ lsn !
    daddr @ 100 + daddr !
    next false
;

: save ( addr u -- f ) \ load slot u to address
    shl shl shl 8 + super @ + 
    @+ lsn ! 
    @ swap daddr !
    for write if pull drop true exit then
    lsn @ 1+ lsn !
    sectvec @ exec
    daddr @ 100 + daddr !
    next false
;

\ This is not complete, it should do something
\ if the load fails!
: dofile ( u -- ) \ load slot and execute
    push 1000 dup pull load 0= if @ exec then ;


done 


