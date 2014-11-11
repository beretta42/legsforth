(

And updater utility for CoCoBoot


)

1400 setorg

include menu.fs


: sstab ( -- ) \ save slot vector
    dovar ; 

: append ( a s u -- a ) \ append u bytes from s to a
    push push dup pull swap r@ mv pull + ;

: remove ( a d u -- a ) \ remove ubytes from a to s
    push push dup pull r@ mv pull + ;

: utoa ( u -- a ) \ convert save slot number to struct address
    0 swap for 22 + next sstab @ + ;

: save ( u -- ) \ save hdb context to memory
    \ make block number = u
    dup ffa4 p!
    \ copy ROM to block
    c000 8000 2000 mv
    \ copy state to memory
    utoa >p
    \ append HDB "static memory"
    13f 11 append
    \ append HDB "dynamic memory"
    f3 a append
    \ append disk con variables
    ea 7 append
    drop
    \ restore block number
    3c ffa4 p! 
;

: apply ( u -- ) \ save hdb context to memory
    \ make block number = u
    dup ffa4 p!
    \ copy ROM to block
    8000 c000 2000 mv
    \ copy state to memory
    utoa >p
    \ append HDB "static memory"
    13f 11 remove
    \ append HDB "dynamic memory"
    f3 a remove
    \ append disk con variables
    ea 7 remove
    drop
    \ restore block number
    3c ffa4 p! 
;


\ This word has not been written, it's code is just
\ copied from os9.fs and cocoboot.fs ... with no effort
\ until I figure out the easiest way to update!
: load ( u -- ) \ load a HDB flavor from disk
    \ create image and put it in place
    dup pro_slotno @ dromoff + drom
    \ switch the mpi
    dup pro_mpino @ mpi
    \ fixup the HDB HW address 
    dup pro_hwaddr @ d93b pw!
    \ fixup the HDB os9 offset
    dup pro_offset c@+ d938 p! @ d939 pw!
    \ fixup the Default ID    
    dup pro_defid c@ dup dup  d93e p! 151 p! 14f p!
    \ auto booting
    dup pro_noauto @ 0= if
	\ patch up AUTOEXEC boot name 
	dup pro_hdbname @ if dup patchAuto then
    else
	\ defeat autoboot?
	defauto
    then
    drop ioff
    \
    \ find loc of warm start address in setup routine
    \ and replace with NOP to restrict HDBINIT to RTS to US
    d93f begin dup pw@ a0e2 - while 1+ repeat 2 + p>
    12 c!+ 12 c!+ drop
    \ find call to BEEP that doesn't work without the Standard
    \ IRQ handler, and write a rts instead. cut the init routine short.
    \ that's fine, it just call BASIC to autoboot, anyway.
    d93f begin dup pw@ d934 pw@ - while 1+ repeat 1 -
    39 swap p!

    \ and find and execute HDINIT in HDB
    d93f begin dup pw@ 0900 - while 1+ repeat 1 - exem
;

    
: init ( -- ) \ initialize system with u disks
    66 alloc sstab !
;


: go
    cls
    init
    slit str "GO!" type cr key drop
    
;


: ddev
    dovar # 0 

: dwtype
    ddev
    list_select
    list_draw
    # 0
    #' D
    str "dW DEVICE: "
    # 2
    str "BITBANGER"
    str "BECKER"


: gobutton
    go
    button_select
    menu_draw
    # 0
    #' G
    str "gO"
    

: main
    noop
    menu_select
    menu_draw
    # 0
    #' M
    str "UPDATE UTILITY"
    dwtype
    gobutton
    # 0
    


    : test
    \ save old CP
    here
    \ reset cp to after our image
    1402 @ cp !
    \ run the main menu
    lit main select drop
    \ restore old cp
    cp !
    \ chain to setup prog
    1 dofile
;
  

