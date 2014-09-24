
: <    2dup xor 0< if drop 0< exit then - 0< ;
: ?dup ( x -- ? ) \ duplicates TOS if TOS is not zero
    dup if dup then ;

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
 char field >MID   \ message ID
 char field >CST   \ channel state
 char field >PAR   \ Parent's OID
 char field >OID   \ task's OID
 char field >WAIT  \ OID we're waiting on
 char field >MUT   \ mutex lock on this task
char field >WAKE  \ why we were woken
cell field >EXITV  \ exit vector of task
 char field >EFLAG \ exit flag
\ cell field >MON   \ monitor we're waiting on
struct task

\ Task States:
\  all tasks that are in state other than RUN are on the
\  sleeping list, not just SLEEP.


0 constant RUN
1 constant ZOMBIE
2 constant SLEEP
3 constant WAIT
4 constant STOP
\ 5 constant MON


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
    ram + dup c@ 1+ swap c! ; expose
: free ( u -- ) \ Closes block
    ram + dup c@ 1- swap c!  ; expose
: salloc ( -- u ) \ Get memory block
     ram dup RAMZ ffz dup if swap - dup open else 2drop true then
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
: kderef ( u -- a ) \ find address, a, of kernel object no u.
    tderef c@ dup 0= if EFD throw then oderef ; expose
: kopen ( u -- ) \ Reopens, increment reference count of object
    tderef c@ dup 0= if drop exit then oopen ; expose
: kclose ( u -- ) \ Closes, decrements reference count of object
    tderef dup c@ dup 0= if 2drop exit then
    oclose false swap c! ; expose
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


: zexit ( a code -- ) \ exit the next to words if this task is a zombie
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



: kspawn ( s0 xt -- k ) \ spawn a task
    kalloc dup push kderef
    dup tp@ task mv
    swap !+ over !+ swap 80 - !+ 1 c!+ drop pull  ( k )
    dup kderef runners @ over >NEXT ! runners !
    dup kderef >ST RUN swap c!
    tp@ >OID c@ over kderef >PAR c!
    dup kderef a2o over kderef >OID c!
    \ reopen all kobjs here
    0 10 for dup kopen 1+ next drop
    \ reopen all alloced memory here
    tp@ >MMU tp@ >MZ c@ for dup c@ open 1+ next drop
;



: helper ( u a -- u f )
   >WAIT c@ over = if true else false then ;

: notify ( OID -- )   \ wake all tasks waiting for OID
    begin
	lit helper sleepers @ map dup ( u a )
    while
	    dup WIPC twake       \ wake the task
	    0 swap >WAIT c!      \ reset it's wait status
    repeat
    2drop
;



\ kernel used this task as the
\ idle task -  one that runs when
\ there is no others runnable

6 variable idle   \ idle's tp

: doexit? ( -- ) \ check flag and execute exit vector
    tp@ >EFLAG c@ if tp@ >EXITV @ ?dup if exec then then 
;
    
: kyield ( -- a ) \ goes to next task
    tp@ >NEXT @ dup 0= if
	drop runners @ dup 0= if drop idle @ then
    then yieldto doexit?    
;

: nexts ( -- a ) \ goes to next task while sleeping
    runners @ dup 0= if drop idle @ then yieldto doexit? ;  


: ksleep ( code u -- w ) \ sleep for u jiffies in st state. w is a wake code
    tp@ >TIMER !                             \ set my timer
    tp@ swap tsleep                          \ put me into a sleep state
    nexts                                    \ go to next task
    tp@ >WAKE c@                             \ get wake tag
;



: owait ( OID -- ) \ go to sleep waiting for OID
   tp@ >WAIT c! WAIT 0 ksleep drop ;

: rel ( OID -- ) \ release a lock
   0 swap oderef >MUT c! ;

: lock ( OID ) \ obtain lock on OID
     begin dup oderef >MUT c@ while dup owait repeat
    1 swap oderef >MUT c! ;

: release ( OID ) \ release lock on OID
    dup rel notify  ;

: relwait ( OID ) \ release lock and wait on 
     dup rel dup notify owait  ;

: CSTwait ( c OID -- ) \ wait until OID  value is c, leave locked
    begin dup lock 2dup oderef >CST c@ - while dup relwait repeat 2drop ;

: myid ( -- OID ) \ my Object ID
    tp@ >OID c@ ;



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
    dup >OBJ 10 for c@+ dup if oclose else drop then next drop   \ close all open objects
    dup >MMU over >MZ c@ for c@+           \ close memory resources
    free next drop		
    0 over >TIMER !   		   	   \ turn off timer
    dup >PAR c@ dup if oderef WRIP twake else drop then        \ wake my parent
    dup ZOMBIE tsleep                      \ put on sleeping lists
    drop
;



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
    myid lock                           \ wait for lock
    tp@ kdestroy                        \ destroy me
    myid release                        \ release lock, wake all waiting
    nexts                               
;


: waitfor ( kid -- u ) \ wait for task u to die, returns u with exit status
    ioff
    dup kderef a2o dup lock                    \ wait for thread lock
    \ wait for zombie state
    begin over kderef >ST c@ ZOMBIE - while dup relwait repeat ( k o )
    \ get task's return
    push dup kderef dup >RET @ -rot ( ret k a )
    \ remove tasklisk, unlock task
    unlink kclose pull release
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
    myid
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

: listen ( -- ) \ tell waiting clients we're listening
    ioff
    0 myid CSTwait
    1 tp@ >CST c!
    myid release
    ion
;


: sendc ( c t -- c ) \ connect to task t sending c, and getting c as reply
    ioff
    kderef push
    \ send a char
    1 r@ a2o CSTwait       \ wait till semaphore is 1 (server listening)
    r@ >MID c!             \ set char
    2 r@ >CST c!           \ set CST to 2  ( client connect )
    r@ a2o release         \ release lock
    \ wait for reply
    3 r@ a2o CSTwait       \ wait till semaphore is 3 (server responded )
    r@ >MID c@             \ retrieve server's char
    1 r@ >CST c!           \ reset semaphore ( no client )
    pull a2o release       \ release lock
    ion
;


: recvc ( --  c ) \ receive char from a client
    ioff
    2 myid CSTwait     \ wait till semaphore is 2
    tp@ >MID c@        \ get byte
    myid release       \ release lock
    ion
;

: replyc ( c -- ) \ send reply char back to client
    ioff
    myid lock
    tp@ >MID c!		  \ set data
    3 tp@ >CST c!         \ set CST to 3 (server is responded)
    myid release
    ion
;


( 
kinit doesn't clr key kernel tables... it should.
this only works because the cross compiler compiles zero's
on alloc'ed memory
)
: kinit ( xt -- ) \ inits system
    ioff
    push
    true tp@ >PAR c!                     \ set an impossible parent
    0 open 1 open			 \ set kernel's memory allocation
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

: recvmsg ( -- m ) \ recv a message
    recvc attach ;

: replymsg ( m -- ) \ reply with message
    tderef c@ replyc ;

: sendmsgc ( m t -- c ) \ send a message, get char as reply
    push tderef c@ pull sendc ;

 

: sendstrc ( ca kid -- c ) \ send a string, get char as reply *WATCHING*
    push alloc dup push deref over @ !+ swap @+ mv
    pull dup pull sendmsgc swap close
;

: sendcmsg ( c t -- m ) \ send c to task t, get m as reply
    sendc attach ;

: sendmsg ( m t -- m ) \ send a message, get message as reply
    push tderef c@ pull sendcmsg ;



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

