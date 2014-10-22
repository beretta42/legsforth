\ ------------------------------------------------------------------------
\ CoCoBoot - A Modern way to boot your Tandy Color Computer
\ Copyright(C) 2011 Brett M. Gordon beretta42@gmail.com
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


\ ******************************************
\ 
\ Run-Time Setup Menu for CoCoBoot
\
\ ******************************************

1000 setorg

include menu.fs

\ **************************
\ Objects
\ **************************


\ system type object
: systype 
   sys
   list_select
   list_draw
   # 0
   #' S
   str "sYSTEM: "
   # 2
   str "COCO2"
   str "COCO3"

\ default profile object
: defprofile
   defpro
   list_select
   list_draw
   # 0 
   #' D
   str "dEFAULT PROFILE: "
   # 4
   str "0"
   str "1"
   str "2"
   str "3"


: save_bpb ( -- ) \ save bpb to disk
   true valid ! bpb @ 0 save ?panic ;

: writeconf
    save_bpb
    confirm_select
    confirm_draw
    # 0
    #' W
    str "wRITE CONFIG"

: loadconf
    load_bpb
    confirm_select
    confirm_draw
    # 0
    #' L
    str "lOAD CONFIG"

: timeo
    timeout
    wentry_select
    wentry_draw
    # 0
    #' T
    str "BOOT tIMEOUT: "

: noautof
    ccbnoauto
    boolselect
    booldraw
    # 0
    #' N
    str "nO AUTOBOOT: "

: reboot
    cold
    confirm_select
    confirm_draw
    # 0
    #' R
    str "rEBOOT"


: modprof ( -- a ) \ data address of selected profile
    dovar # 0
: getprof ( -- a ) \ get data address of tag for profile
    modprof @ ;
: getdrive ( -- a ) \ get data address of drive for profile    
    getprof pro_drive ;
: getmeth ( -- a ) \ get data address of boot method for profile
    getprof pro_method ;
: getslot ( -- a ) \ get data address of slot no for profile
    getprof pro_slotno ;
: getmpi ( -- a ) \ get dat address of mpi no for profile
    getprof pro_mpino ;
: getside ( -- a ) \ get data address of sIDE flash no
    getprof pro_sideno ;
: gethwaddr ( -- a ) \ get data address of HDB device
    getprof pro_hwaddr ;
: getoffset ( -- a ) \ get offset
    getprof pro_offset ;
: getdefid ( -- a ) \ get HDB default ID
    getprof pro_defid ;
: getnoauto ( -- a ) \ get no autoboot flag
    getprof pro_noauto ;

: tag 
    getprof
    text_select
    text_draw
    # 0
    #' T
    str "tAG: "
    # 14

: driveno
    getdrive
    bentry_select
    bentry_draw
    # 0
    #' D
    str "dRIVE NO: "

: mpino
    getmpi
    list_select
    list_draw
    # 0
    #' I
    str "MPi SWITCH: "
    # 4
    str "1"
    str "2"
    str "3"
    str "4"


\ this table is dynamically filled with object
\ used to enable forward referencing
: bmeth_table
    dovar
    # 0
    # 0
    # 0
    # 0

: bmeth_select
    list_select drop 
    getmeth @ shl bmeth_table + @ 
    rp@ cell+ !
    true ;

: bmeth
    getmeth
    bmeth_select
    list_draw
    # 0
    #' M
    str "mETHOD: "
    # 4
    str "DOS COMMAND"
    str "HDB LOADER"
    str "EXTERNAL ROM"
    str "OS9"

\
\ chain loading object
\

: chain
    noop
    menu_select
    menu_draw
    # 0
    #' 0
    str "MODIFY PROFILE"
    bmeth
    tag
    driveno    
    # 0


\ 
\ Disk rom loading object
\ 


: slotno
    getslot
    llist_select
    llist_draw
    # 0
    #' D
    str "dISK ROM IMAGE: "
    # 11
    str "CHS"
    str "DW3BC3"
    str "DW3CC3"
    str "DW4CC3"
    str "TC3"
    str "D4N1"
    str "DW3BCK"
    str "DW3JC2"
    str "KENTON"
    str "DHDII"
    str "DW3CC1"
    str "DW3JC3"
    str "LBA"
    str "DW3ARDUINO"
    str "DW3CC2"
    str "DW4CC2"
    str "LRTECH"

: bankno
    getside
    list_select
    list_draw
    # 0
    #' F
    str "fLASH ROM BANK: "
    # 4
    str "0"
    str "1"
    str "2"
    str "3"

: hwaddr
    gethwaddr
    wentry_select
    wentry_draw
    # 0
    #' A
    str "DEVICE aDDR: "

: hdboffset
    getoffset
    lentry_select
    lentry_draw
    # 0
    #' O
    str "OS9 oFFSET: "

: defid
    getdefid
    bentry_select
    bentry_draw
    # 0 
    #' E
    str "DeFID ID: "

: noauto
    getnoauto
    boolselect
    booldraw
    # 0
    #' U
    str "DISABLE AuTOBOOT: "
    
: rom
    noop
    menu_select
    menu_draw
    # 0
    #' 0
    str "MODIFY PROFILE"
    bmeth
    tag
    mpino
    hwaddr
    bankno
    # 0

: drom
    noop
    menu_select
    menu_draw
    # 0
    #' 0
    str "MODIFY PROFILE"
    bmeth
    tag
    slotno
    mpino
    hwaddr
    hdboffset
    defid
    noauto
    # 0

: os9
    noop
    menu_select
    menu_draw
    # 0
    #' 0
    str "MODIFY PROFILE"
    bmeth
    tag
    slotno
    mpino
    hwaddr
    hdboffset
    defid
    driveno
    # 0    

: profile_select
    self @ profs + modprof !
    getmeth @ shl bmeth_table + @ select 
;    

: profile_draw
    pos!
    label type space
    88 pw@ ascii over p! 1+ 88 pw!	
    space
    self @ profs + modprof ! 
    getprof type 
;


\ profile object
: profile0
    # 0
    profile_select
    profile_draw
    # 0
    #' 0
    str " PROFILE"

\ profile object
: profile1
    # 30
    profile_select
    profile_draw
    # 0
    #' 1
    str " PROFILE"

\ profile object
: profile2
    # 60
    profile_select
    profile_draw
    # 0
    #' 2
    str " PROFILE"

\ profile object
: profile3
    # 90
    profile_select
    profile_draw
    # 0
    #' 3
    str " PROFILE"


: l100
   14 dofile ;
\ load up util button
: exam
    l100
    button_select
    confirm_draw
    # 0
    #' X
    str "HDB ExAM"

\ prints some debug info
: dvec
    15 dofile ;
: debug
    noop
    dvec
    confirm_draw
    # 0
    #' E
    str "DeBUG INFO"


\ Main menu object
: main
    noop
    menu_select
    menu_draw
    # 0
    #' M
    str "mAIN MENU"
    systype
    timeo
    noautof
    defprofile
    profile0
    profile1
    profile2
    profile3
    writeconf
    loadconf
    reboot
\   debug
    exam
    # 0

: test
    \ fill out table
    bmeth_table 
    lit chain !+
    lit drom !+
    lit rom !+
    lit os9 !+
    drop
    begin lit main select drop again ;
