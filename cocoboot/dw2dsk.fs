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
***************************
 A simple Drivewire to DSKCON disk copier.
 This results in a forth image that should be
 slz'd and copied with the deslzer and relocator
 along with the VM itself in this order:
 
 Relocation Code - rel.asm
 SLZ compressed Virtual Machine
 SLZ compressed Virtual Memory Image

This file was created to be able to be cassette'd
into a CoCo that has DW and FD/IDE hardware,
but no bios that contains software support for both.

***************************
)


include 	menu.fs
include		dskcon.fs
include		dw.fs

\ drivewire drive no variable
: dwdr ( -- a )
   dovar # 0 ;

\ drivewire drive no
: dwdrive ( bentry )
     dwdr   	     \ data method
     bentry_select   \ select method
     bentry_draw     \ draw method
     # 0	     \ screen position 
     #' W	     \ Select key
     str "DRIVE-wIRE DISK NO: "

\ dskcon drive no
: dskdrive ( bentry )
     drive   	     \ data method
     bentry_select   \ select method
     bentry_draw     \ draw method
     # 0	     \ screen position 
     #' D	     \ Select key
     str "dSKCON DRIVE NO: "


\ copy progress and spinner

: spiny ( sector spinner )
     # 0
     spin_select
     spin_draw
     # 0
     #' Z 
     # 0	     \ no label

: progy ( progress bar )
     # 0
     progress_select
     progress_draw
     # 0
     #' Z
     str "COPY PROGRESS:"
     # 0             \ counter
     # 14	     \ increment every 20 ticks

\ handle disk errors
: dskerr ( ca -- "Error message" ) 
     cls
     slit str "DISK ERROR!"
     key drop pull pull 2drop ;

: dwerr ( ca -- "Error Message" )
     cls
     slit str "DRIVEWIRE ERROR!" cr
     key drop pull pull 2drop ;

: go ( -- ) \ go copy disk
     0 lsn ! \ reset lsn
     drive c@ \ save dskcon drive no
     276 for 
 	 dwdr c@ drive c! \ set dw drive
	 dwgetsec if dwerr then 
	 dup drive c! write if dskerr then
	 500 88 pw! dup wemit
     	 lit progy select lit spiny select 2drop
	 lsn @ 1+ lsn ! \ increment lsn
     next 
     drive c!
     0 lit progy ! \ reset pointer
;


: start ( confirm copy )
     go
     confirm_select
     confirm_draw
     # 0
     #' S
     str "sTART COPY"

: main ( menu ) 
     noop	      \ data method
     menu_select      \ select method
     menu_draw	      \ draw method
     # 0              \ screen position storage
     #' M	      \ select key         
     str "DW TO DSK"  \ label
     dwdrive 	      \ union - zero termed lists of widgets
     dskdrive	      
     start
     progy
     spiny
     # 0
       

: start
    0 ffd9 p!     \ set high speed ( for coco3)
    here daddr !  \ set sector buffer
    begin lit main select drop again ;


