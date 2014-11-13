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

1400 setorg

include menu.fs

: clit ( -- c ) \ a charactor size literal
    r@ c@
    pull 1+ push
;


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

: defprofile_select
    bentry_select
    defpro @ 7 > if
	0 defpro ! drop true	
    then
;
: defpro1+
    defpro 1+
;
\ default profile object
: defprofile
   defpro1+
   defprofile_select
   bentry_draw
   # 0 
   #' D
   str "dEFAULT PROFILE: "


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
: getpflags ( -- a ) \ get flag addr for profile
    getprof pro_pflags ;
: gethdbname ( -- a ) \ get AUTOEXEC name field
    getprof pro_hdbname ;

: tag
    getprof
    text_select
    text_draw
    # 0
    #' T
    str "tAG: "
    # 14

: hdbname
    gethdbname
    text_select
    text_draw
    # 0
    #' N
    str "AUTOEXEC nAME: "
    # 8

: bootname
    gethdbname
    text_select
    text_draw
    # 0
    #' B
    str "bOOTFILE: "
    # 8

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

: pause
    getpflags
    boolselect
    booldraw
    # 0
    #' P
    str "pAUSE FOR ROOT: "


\ this table is dynamically filled with object
\ used to enable forward referencing
: bmeth_table
    dovar
    # 0
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
    # 5
    str "VOID"
    str "DOS COMMAND"
    str "HDB LOADER"
    str "EXTERNAL ROM"
    str "OS9"


\ 
\ Disk rom loading object
\ 


: slotno
    getslot
    llist_select
    llist_draw
    # 0
    #' R
    str "DISK rOM IMAGE: "
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
    # 8
    str "0"
    str "1"
    str "2"
    str "3"
    str "4 SDC ONLY"
    str "5 SDC ONLY"
    str "6 SDC ONLY"
    str "7 SDC ONLY"


    \ This big-time interfers with
    \ the setup program itself - reusing the
    \ hdbname profile field is dumb.
: rombanker
    gethdbname    \ <--- change this
    list_select
    list_draw
    # 0
    #' B
    str "bANKER TYPE: "
    # 3
    str "NONE"
    str "SUPER IDE"
    str "SDC"

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
    str "HDB oFFSET: "

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

: 9noauto
    getnoauto
    boolselect
    booldraw
    # 0
    #' K
    str "DEBLOCk HD: "


: void
    noop
    menu_select
    menu_draw
    # 0
    #' 0
    str "MODIFY PROFILE"
    bmeth
    # 0
    
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
    rombanker
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
    hdbname
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
\    hdboffset
    defid
    driveno
    9noauto
    bootname
    pause
    # 0

\
\ chain loading object
\   this does a cross-device DOS command
\   yes this will load os9 the old fashioned way,
\   but the OS9 must be setup for /DD to be
\   where ever it's at!!! (if not drive 0 )
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
    slotno
    mpino
    hwaddr
    hdboffset
    defid
    driveno    
    # 0


: delprof
    self @ profs + profileZ for 0 c!+ next drop
;

: delete_select
    5a0 88 pw!
    slit str "REALLY DELETE? " type key 59 = if
	delprof
    else
	drop
    then
    true
;


 
: cpprof_select
    5a0 88 pw!
    self @ profs + >p     \ source - selected profile
\    curInd @
    slit str "DST: " type dup dup sp@ dup 2 naccept >num nip nip cr 
    \ check for valid profile number here
    dup 7 > if 
	5e0 88 pw!
	slit str "INV DEST, ANYKEY" type
	key drop 2drop true exit
    then
    prof2a >p
    slit str "REALLY? (y)" type key 59 = if ( s d )
	profileZ mv
    else
	2drop
    then
    true \ redraw parent menu
;

: boot_select
    0 self @ begin dup while profileZ - swap 1+ swap repeat drop
    boot 
;


: move_select
    cpprof_select drop
    delprof
;

    
: edit_select
    self @ profs + modprof !
    getmeth @ shl bmeth_table + @ select 
;    

: profile_draw
    pos!
    label type
    88 pw@ ascii over p! 1+ 88 pw!	
    space
    self @ profs + modprof ! 
    getprof type 
;


: profile_select
    5e0 88 pw!
    slit str "eDIT,cOPY,dELETE,mOVE,bOOT?" type
    key
    dup clit #' E = if drop edit_select else
    dup clit #' C = if drop cpprof_select else
    dup clit #' D = if drop delete_select else
    dup clit #' M = if drop move_select else 
    dup clit #' B = if drop boot_select else
    then then then then then
    drop
    true
;




\ profile object
: profile0
    # 0
    profile_select
    profile_draw
    # 0
    #' 0
    str ""

\ profile object
: profile1
    # 34
    profile_select
    profile_draw
    # 0
    #' 1
    str ""

\ profile object
: profile2
    # 68
    profile_select
    profile_draw
    # 0
    #' 2
    str ""

\ profile object
: profile3
    # 9c
    profile_select
    profile_draw
    # 0
    #' 3
    str ""

    \ profile object
: profile4
    # d0
    profile_select
    profile_draw
    # 0
    #' 4
    str ""

: profile5
    # 104
    profile_select
    profile_draw
    # 0
    #' 5
    str ""

: profile6
    # 138
    profile_select
    profile_draw
    # 0
    #' 6
    str ""

: profile7
    # 16c
    profile_select
    profile_draw
    # 0
    #' 7
    str ""

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


: lupdate
    16 dofile ;
: update
    lupdate
    button_select
    confirm_draw
    # 0
    #' U
    str "uPDATE"

    
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


: delprof
    noop
    delete_select
    confirm_draw
    # 0
    #' Z
    str "zERO PROFILE"
    
\ copy profile
: cpprof
    noop
    cpprof_select
    confirm_draw
    # 0
    #' C
    str "cOPY"
    
\ Edit profiles sub menu    
: editprofiles
    noop
    menu_select
    menu_draw
    # 0
    #' E
    str "PROFILE eDITOR"
    profile0
    profile1
    profile2
    profile3
    profile4
    profile5
    profile6
    profile7
\    cpprof
\    delprof
    # 0

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
    editprofiles
    writeconf
    loadconf
    reboot
    update
    exam
    # 0

: test
    \ fill out table
    bmeth_table
    lit void !+
    lit chain !+
    lit drom !+
    lit rom !+
    lit os9 !+
    drop
    begin lit main select drop again ;
