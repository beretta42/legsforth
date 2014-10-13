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

This is a HDB-DOS ROM image examiner
program... it's designed to investigate
your external rom image for vital HDB-DOS settings

)

1000 setorg

include menu.fs


: rbank ( -- a ) \ sIDE rom bank setting
    dovar # 0
: mpiv ( -- a ) \ mpi switch setting
    dovar # 3
: side ( -- a ) \ super ide base address
    dovar # 1

: baddr ( -- pa ) \ get primitive base address of sIDE
    side @ 4 + shl shl shl shl ff00 + ;

: rc@+ ( a -- a c )
    sys @ if rommode then
    c@+
    sys	@ if rammode then
;

: r@+ ( pa -- a x )
    sys @ if rommode then
    @+
    sys @ if rammode then
;

: rc@ ( pa -- c )
    rc@+ nip ;

: r@ ( pa -- x )
    r@+ nip ;

: show ( -- ) \ display HDB information
   cr
   sys @ if cc ff90 p! then
   mpiv @ mpi
   rbank @ baddr 9 + p! 
   pos!
   slit str "OFFSET: " type
   d938 p> rc@+ bemit r@+ wemit cr
   slit str "DEVICE BASE: " type
   r@+ wemit cr
   slit str "STARTUP DELAY: " type
   rc@ bemit cr 

   c000 p> r@ 444b = dup

   if slit str "DISK ROM FOUND" else
   slit str "NO DISK ROM FOUND" then type cr 

   if
     ddfb p> begin dup r@ 4155 = 0= over >p e000 < and while 1+ repeat >p
     dup e000 = 
     if drop slit str "HDB NOT FOUND" type cr
     else b + p> begin rc@+ dup while emit repeat 2drop 
     then
   then

;


: w_display
    noop
    false
    show
    # 0
    #' a
    # 0

: newselect
    list_select drop true ;

\ sIDE rom bank widget
: w_rbank
    rbank
    newselect
    list_draw
    # 0
    #' S
    str "sUPER IDE BANK: "
    # 4
    str "0"
    str "1"
    str "2"
    str "3"

\ sIDE base address 
: w_baddr
    side
    list_select
    list_draw
    # 0
    #' A
    str "SUPER IDE aDDRESS: "
    # 4
    str "FF40"
    str "FF50"
    str	"FF60"
    str	"FF70"

\ MPI switch select
: w_mpi
    mpiv
    newselect
    list_draw
    # 0
    #' M
    str "mPI SWITCH: "
    # 4
    str "1"
    str "2"
    str "3"
    str "4"

: boot 
    mpiv @ mpi
    rbank @ ff59 p!
    sys @ if cc ff90 p! then rommode cold
;

: w_boot
    boot
    confirm_select
    confirm_draw
    # 0
    #' B
    str "bOOT THIS ROM"
	

: menu 
   noop
   menu_select
   menu_draw
   # 0
   #' z
   str "HDB ROM EXAM"
   w_rbank
   w_baddr
   w_mpi
   w_boot
   w_display
   # 0 
   

: main
    lit menu select drop 1 dofile
