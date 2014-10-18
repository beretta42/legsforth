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
 *****************************
 
  DSKCON - Forth Interface
 
 ****************************

This is a forth interface to the DSKCON interface
Load up "daddr", "lsn" and "drive" variables and call read or write 

)


: daddr ( -- a ) \ address of disk data address variable
    dovar ; 
: lsn ( -- a )   \ address of lsn for oscon routines
    dovar ;
: drive ( -- a ) \ address of drive no for oscon routines
    dovar ;

: geotran ( lsn -- sec trk ) \ convert lsn to DD SS
    0 swap begin swap 1+ swap 12 - dup 0< until
    13 + swap 1- ;

: dskgo ( op -- f ) \ does a dskcon call with op code
    push
    lsn @ geotran     \ translate lsn to drive geometry
    c006 pw@ p>
    pull c!+          \ store opcode
    drive c@ c!+       \ drive no
    swap c!+          \ trk
    swap c!+  	      \ sector
    daddr @ >p !+     \ data address
    c004 pw@ exem c@  \ execute dskcon and get return
;
  

: read ( -- f ) \ load a sector via dskcon
    2 dskgo ;  

: write ( -- f ) \ save a sector via dskcon
    3 dskgo ;

    
done

