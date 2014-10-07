
: <    2dup xor 0< if drop 0< exit then - 0< ;
: ?dup ( x -- ? ) \ duplicates TOS if TOS is not zero
    dup if dup then ;
: sex ( c -- x ) \ sign extend byte to cell
    dup 80 and if ff00 or then ;
: cell- cell - ;
: -! ( a x -- a ) \ push a software stack
    swap cell- tuck ! ;
    
( 
For catch an throw, the saved state frame is as follow:
low mem ->  <handler>  <SP-2>   -> high mem
Theses routines have always worked in any of my forths.
and are pretty much stolen from EFORTH.
)

0  variable handler               \ Most recent exception handler


: catch ( xt -- e )  \ catch a thrown error
   sp@ push               \ Save data stack pointer
   handler @ push         \ Save previous handler
   rp@ handler !          \ Set current handler to this one
   exec                   \ Execute the word passed in on the stack
   pull handler !         \ Restore previous handler
   pull drop              \ Discard saved stack pointer
   0                      \ Signify normal completion
;

: throw ( e -- e ) \ Returns in saved context
   dup 0= if drop exit then  \ throweth naught 0
   handler @ rp!             \ Return to saved return stack context
   pull handler !            \ Restore previous handler
   pull swap push            \ pull sp and put e on return stack
   sp! drop pull             \ adjust sp and put e on stack
; 



(
  A table of throwable system error codes
)
8000 constant EFD      \ Attempt to access invalid FD
8001 constant EMEM     \ Memory Error ( usually out of )
8002 constant EUMEM    \ out of handles
8003 constant EOMEM    \ out of object memory


(
Wake codes
are set by the kernel to indicate why a task was woken
)
1 constant     WTO       \   Task timed-out     
2 constant     WIPC      \   Task's channel woke it
3 constant     WRIP      \   Death of Child task
4 constant     WRST      \   Task has been suspended and restarted again
5 constant     WTERM     \   Task has been asked to exit



: ffz ( a u -- a | 0 ) \ find first zero byte
   for dup c@ 0= if unloop exit then 1+ next drop false ;


\ ******************************************
\ Memory Block Allocation
\ ******************************************

\ ram size in 8k blocks
40 constant RAMZ

\ a table of link counts for then ram blocks
create ram RAMZ allot 



\ **************************************
\ Task Structures
\ **************************************

0
 cell field >IP    \ saved IP
 cell field >RP    \ saved RP
 cell field >SP    \ saved SP
 char field >EX    \ saved other VM state ( interrupts )
 8    field >MMU   \ mmu mirror
 cell field >NEXT  \ next task in list
 cell field >C0    \ compile pointer
 cell field >DH    \ dictionary pointer
 10   field >OBJ   \ object allocation table  
 char field >MZ    \ number of mem blocks allocated in task's mmu
 cell field >RET   \ return value of task
 char field >ST	   \ state of task ( why we're sleeping )
 cell field >TIMER \ kernel timeout
 char field >PAR   \ Parent's OID
 char field >OID   \ task's OID
 char field >WAKE  \ why we were woken
 cell field >EXITV  \ exit vector of task
 char field >EFLAG \ exit flag
 cell field >MON   \ address monitor we're waiting on
 cell field >MNEXT \ next in the list of waiter on this monitor
struct task

\ Task States:
\  all tasks that are in state other than RUN are on the
\  sleeping list, not just SLEEP.


0 constant RUN          \ We're CPU Bound
1 constant ZOMBIE       \ Dead - waiting waiting for "waitfor"
2 constant SLEEP        \ just waiting on jiffy timer
3 constant WAIT         \ waiting for a child task to die
4 constant STOP         \ we've been externally stopped
5 constant MON          \ waiting on a monitor


40 constant KOBJZ \ size of kernel objects
40 constant KNUM \ number of kernel objects


\ some parallel tables: 
\ objs is a table of kernel objects ( tasks and data buffers )
\ oref is a table of kernel objects reference counts

create objs KOBJZ KNUM * allot
create oref KNUM allot


    (
    ******************************************************
    Kernel internel words. RULES:
    1. these are executed in interrupt off state!!
    2. don't turn on interrupts
    3. whatever we do, do fast
    ******************************************************
    )

    

: open ( u -- ) \ (re)Opens block
    1- ram + dup c@ 1+ swap c! ; expose
: free ( u -- ) \ Closes block
    1- ram + dup c@ 1- swap c!  ; expose
: salloc ( -- u ) \ Get memory block ( returns 1-65 )
     ram dup RAMZ ffz dup if swap - 1+ dup open else 2drop true then
     ; expose


0 variable runners   \ linked list of all tasks
0 variable sleepers  \ linked list of stopped tasks


: oderef ( u -- a ) \ dereference obj to an address
   1- shl shl shl shl shl shl objs + ; 
: a2o ( a -- u ) \ changes address to Object ID
   objs - shr shr shr shr shr shr 1+ ;
: oopen ( u -- ) \ opens an object
   1- oref + dup c@ 1+ swap c! ;
: oclose ( u -- ) \ closes an object
   1- oref + dup c@ 1- swap c! ;
: oalloc ( -- u | -1 ) \ allocate a kern
   oref dup RAMZ ffz dup if swap - 1+ dup oopen else 2drop true then ;

: talloc ( -- u | -1 ) \ allocate a task's object table entry, but dont commit.
    tp@ >OBJ dup 10 ffz dup if swap - else 2drop true then ;
: tderef ( u -- a ) \ deference task obj's ref to address
    tp@ >OBJ + ;


: kalloc ( -- u | -1 ) \ allocate kernel object
    talloc dup 0< if EUMEM throw then  \ find task obj desc
    oalloc dup 0< if EOMEM throw then  \ find kernel obj
    over tderef c! \ assign kernel obj to task's obj table
;  expose
: xalloc ( -- k ) \ allocate a ram block
    talloc dup 0< if EUMEM throw then  \ get a task obj desc
    salloc dup 0< if EMEM throw then   \ try to alloc memory
    neg over tderef c! \ assign RAM block to task's obj table
; expose
: kderef ( u -- a ) \ find address, a, of kernel object no u.
    tderef c@ sex
    dup 0= if drop EFD throw then
    dup 0< if drop EFD throw then
    oderef
; expose
: kopen ( u -- ) \ Reopens, increment reference count of object
    tderef c@ sex
    dup 0= if drop exit then
    dup 0< if neg open exit then
    oopen ; expose
: kclose ( u -- ) \ Closes, decrements reference count of object
    tderef dup c@ sex
    dup 0= if 2drop exit then
    dup 0< if neg free else oclose then
    false swap c! ; expose
: kattach ( o -- u ) \ make local reference to kobj u, return handle
   talloc dup 0< if exit then      \ find task obj desc
   tuck tderef c!                  \ assign obj to task's table
   dup tderef c@ oopen 
;


\ applies xt to each element in linked list stating with 1st.
\ if xt returns -1, map stopped mapping and returns list pointer
\ else if no xt returns -1, then map returns 0.
\ MOD: change to make elements removable off list while iterating
\ xt prototype:  xt ( ... a -- ... f )
: map ( xt 1st -- a | 0 )  \ maps xt onto list
    begin dup while dup >NEXT @ push 2dup push push swap exec 
    if pull drop pull pull drop exit then 
    pull pull drop pull repeat 2drop false ;


: helper ( a ta -- a f )
   >NEXT @ over = if true else false then ; hide

: lh ( ta -- lh ) \ puts address of list head for node
   >ST c@ if sleepers else runners then ;

: prev ( ta -- ta ) \ find previous task in list
   dup lh @ lit helper swap map nip ;

: unlink ( a -- )  \ remove task off it's list  
   dup >NEXT @ over prev dup if ( a  n p/f )
     >NEXT !
   else   
     drop over lh !
   then 
   drop ;


: zexit ( a code -- ) \ exit if this task is a zombie
    over >ST c@ ZOMBIE - if exit then 2drop pull drop exit ; hide

: tsleep ( a code -- )    \ send task to sleep list
    zexit
    swap
    dup unlink sleepers @ over >NEXT ! 
    dup sleepers !
    >ST c! 
;

: twake ( a code -- ) \ wake task, a, with wake code
    zexit
    over >WAKE c! 
    dup unlink runners @ over >NEXT ! 
    RUN over >ST c! 
    runners !
;



: kspawn ( i*j s0 xt -- k ) \ spawn a task
    kalloc dup push kderef ( r: k s0 xt a )
    dup tp@ task mv 
    swap !+ over !+ swap 80 - ( a sp )
    sp@ 4 + @ -!
    !+ 1 c!+ drop pull  ( k )
    dup kderef runners @ over >NEXT ! runners !
    dup kderef >ST RUN swap c!
    tp@ >OID c@ over kderef >PAR c!
    dup kderef a2o over kderef >OID c!
    \ reopen all kobjs here
    0 10 for dup kopen 1+ next drop
;



\ kernel used this task as the
\ idle task -  one that runs when
\ there is no others runnable

6 variable idle   \ idle's tp

: doexit? ( -- ) \ check flag and execute exit vector
    tp@ >EFLAG c@ if tp@ >EXITV @ ?dup if exec then then 
;
    
: kyield ( -- ) \ goes to next task
    tp@ >NEXT @ dup 0= if
	drop runners @ dup 0= if drop idle @ then
    then yieldto doexit?    
;

: nexts ( -- ) \ goes to next task while sleeping
    runners @ dup 0= if drop idle @ then yieldto doexit? ;  


: ksleep ( code u -- w ) \ sleep for u jiffies in st state. w is a wake code
    tp@ >TIMER !                             \ set my timer
    tp@ swap tsleep                          \ put me into a sleep state
    nexts                                    \ go to next task
    tp@ >WAKE c@                             \ get wake tag
;



0 variable ticks expose  \ incremented every timer tick

\ This helps "interrupt" decrement sleeping task's timers, and waking them
\  on crossing to 0.
: helper ( a -- f )
   dup >TIMER dup @ dup ( a ta t ) 
   if    1- tuck swap ! if drop else WTO twake then
   else  3drop
   then  false
;

0 variable keyvec  \ This key xt vector, called once a tick


: interrupt ( -- ) \ This will be called once a tick
   ioff
   1 ticks +!                          \ inc ticker var
   keyvec @ dup if exec else drop then \ run the key timer if set
   lit helper sleepers @ map drop      \ dec all timers & wake tasks
   nexts ion
;



: newkey
  begin key? dup 0< while drop SLEEP 4 ksleep drop repeat 
  dup emit ;


: kdestroy ( u a -- ) \ exit task a with return value of u
    swap over >RET !                            \ set return value
    0 10 for dup kclose 1+ next drop            \ close all resources
    0 over >TIMER !   		   	        \ turn off timer
    dup >PAR c@ dup if oderef WRIP twake else drop then        \ wake my parent
    dup ZOMBIE tsleep                           \ put on sleeping lists
    drop
;

\ 
\ Task Locking
\ 

(
Locking requires a shared monitor structure from each task
using the monitor.  It's structure is trivial:

0   1   flag - weather monitor is locked
1   2   head - points to first task waiting on this lock
)


: lwait ( a -- ) \ wait on this monitor until notified
    tp@ MON tsleep                           \ put myself to sleep
    dup char+ @ swap                          \ put old list head on stack
    tp@ swap char+ !                          \ make me the new 1st
    tp@ >MNEXT !                              \ get link of mutex head
    nexts                                     \ run the next task
;

: notify ( a -- ) \ wake up all tasks on monitor
    char+ dup @ swap 0 swap !
    begin ?dup while
	    dup >MNEXT @ swap
	    WIPC twake
    repeat
;

: lrel ( a -- ) \ release a monitor
    0 swap c!
;

: klock ( a -- ) \ lock monitor a
    begin dup c@ while dup lwait repeat
    true swap c!
;

: lock ( a -- ) \ lock monitor a
    ioff klock ion
;

: release ( a -- ) \ release monitor
    ioff dup lrel notify ion ;

: waiton ( a -- ) \ wait on a monitor ( releases lock and waits )
    ioff dup dup dup lrel notify lwait klock ion ;

(
************************************************
These next words are interface to the kernel. They should:
1. turn ints off before calling anything above.
2. cannot call eachother.
3. must turn the interrupts back on before leaving
************************************************
)


: texit ( u -- )  \ exit task with return value of U
    ioff
    tp@ kdestroy                        \ destroy me
    nexts                               
;


: waitfor ( kid -- u ) \ wait for task u to die, returns u with exit status
    ioff
    \ wait for zombie state
    begin dup kderef >ST c@ ZOMBIE - while tp@ SLEEP nexts repeat ( k )
    \ get task's return
    dup kderef >RET @ swap ( u k )
    \ remove tasklisk, unlock task
    dup kderef unlink kclose
    ion
;
 



\ where does this belong?
: a2k ( a -- k ) \ not sure I like this... it re-references an ka
    \ check for out of range
    a2o 40 over - 0< if drop true exit then
    push tp@ >OBJ    \ find our KID for this address
    begin    		\ this prolly should be a for loop
	dup c@ r@ -
    while
	char+
    repeat
    pull drop
    tp@ >OBJ -
;



: helper ( oid a -- oid f ) \ checks task a for match for wait
   dup push
   >PAR c@ over =           \ our we the parent of this task?
   pull >ST c@ ZOMBIE = and \ is the task a zombie?
   if true else false then  \ return result to map
;

: wait ( -- kid ) \ wait for any child task to die
    ioff
    \ scan through sleepers
    tp@ >OID c@
    begin 
	lit helper sleepers @ map dup 0= 
    while 
	drop WAIT 0 ksleep drop
    repeat
    nip a2k
   ion
;



: stop ( kid -- ) \ stop a task from running
    ioff kderef dup STOP tsleep dup 0 swap >TIMER !
    drop
    ion ;


: start ( kid -- ) \ wake a task
    ioff kderef WRST twake ion ;



: malloc ( -- b )  \ Kern iface: allocate block memory
    ioff salloc ion ;
: mfree ( b -- ) \ Kern iface: free memory
    ioff free ion ;
: alloc ( -- k | -1 ) \ Kern iface: allocate kernel object
    ioff kalloc ion ;
: deref ( k -- a ) \ de-references a kernel reference
    ioff kderef ion ;
: attach ( o -- k ) \ attached obj o to task's kernel references
    ioff kattach ion ;
: close ( k -- ) \ Kern iface: close kernel object
    ioff kclose ion ;
: spawn ( s0 xt -- k ) \ Kern iface: create thread
    ioff kspawn ion ;
: yield ( -- ) \ yield to the kernel
    ioff kyield ion ;
: sleep ( u -- w ) \ Kern iface: sleep for u ticks
    ioff SLEEP swap ksleep ion ;
: term ( k -- ) \ ask a task to terminate
    ioff kderef
    dup WTERM twake
    >EFLAG true swap c! ion ;
: kill ( k -- ) \ kills a task
    ioff kderef true swap kdestroy ion ;
: setexit ( xt -- ) \ set task's exit vector
    ioff tp@ >EXITV ! ion ;


( 
kinit doesn't clr key kernel tables... it should.
this only works because the cross compiler compiles zero's
on alloc'ed memory
)
: kinit ( xt -- ) \ inits system
    ioff
    push
    true tp@ >PAR c!                     \ set an impossible parent
    xalloc drop xalloc drop              \ allocate kernel RAM
    tp@ sleepers !			 \ add as sleeper task 		 
    2 tp@ >MZ c!   	   	   	 \ set allocd mmu to 2
    lit interrupt 2 !			 \ set timer interrupt handlers
    lit ekey
    lit newkey !+ lit exit !+ drop        \ replace ekey with a yield happy one
    3f00 sp!
    3e80 pull kspawn drop                 \ thread the given xt
    ion
    begin iwait ioff nexts ion again
;




(
**************************************************************
Kernel extension words. Rules:
    1. from here out don't shut off interrupts. done.
    2. they never should call any thing above the interface words above.
**************************************************************
)


\ stack allocation pointer
\   it points to something!!!!!
\ as "thread" makes new threads, it allocates space
\ top down in memory for the RP and SP stacks of
\ the new task.
 3d80 variable spap

: spallot ( -- a )
    spap @ -100 spap +! ;

 
: thread ( xt -- u ) \ creates new task
    ioff spallot swap spawn ; expose



done

