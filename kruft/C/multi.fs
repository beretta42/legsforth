\ ************************************
\ Tasks
\ ************************************

dlist
  cell field >rp           \ task's saved return pointer
  cell field >list         \ which task list we're on
  cell field >dsize        \ size of data space
  struct task

  l_head runners           \ list head for running tasks

{ 

  : mvtask ( head task -- )     \ move task to list
     dup >list @ tuck !
     l_dequeue 2dup swap l_queue
     >list !
  ;

  : restore ( -- )         \ restores a task's state
     runners @ dup 0= if ." No Tasks!" cr begin again then 
     >rp @ rp!             \ retore rp
     pull sp!              \ restore sp
     ion
  ;
public 

  : myself ( -- a )       \ puts our task struct on stack
     runners @ ;

  : wait ( head -- )      \ causes current process to sleep
     ioff sp@ cell+ push
     rp@ myself >rp !
     myself mvtask restore
  ;

  : wake ( task -- )       \ wakes another task
     runners swap mvtask ;

  : yield ( -- )           \ start next running task
     ioff
     sp@ push
     rp@ myself >rp ! 
     runners l_next restore
  ;

  : texit ( -- )           \ exit a task
   ioff
   runners l_rm          \ removes our task from running list
   restore
  ;

}


\ install timer interrupt handler
' yield 2 !

\ *****************************************
\ Binary Semaphore Locking
\ *****************************************

{
  0 
  cell field >flag  \ is locked?
  cell field >list  \ is waiting tasks list
  
  : lrel ( sema -- ) \ release lock 
     dup >list @               \ test for waiting tasks
     if >list l_dequeue wake   \ wake task
     else false swap !         \ clear lock
     then ;                    \ return

  public

  struct sema

  : lock ( sema -- ) \ aquires the lock
     dup ioff @          \ test the lock
     if >list wait       \ if locked then wait
     else true swap !    \ else set lock
     then ion ;          \ return

  : release ( sema -- ) \ releases the lock
     ioff lrel ion ;    

  : swait ( sema -- ) \ release lock and go on waiting list
     dup ioff lrel       \ release lock and wake waiter
     >list wait ;         \ place self on sema's waiting list

  : semaphore ( "name" -- ) ( -- sema )
     here 0 , 0 , constant ; 
}


{

  400 dchunk dmem          \ setup system chunks
  semaphore s

  public

  \ salloc - allocate system memory
  : salloc ( u -- a )
     s lock 
     dmem alloc 
     s release
  ;    

}

\ spawn creates a new task - the new task will
\ start executing ( when it's time comes ) xt
\ and will receive new data stack and return stack spaces
: spawn ( xt -- task )
   \ get new stack space
   task 40 cells + salloc
   tuck task 40 cells + + dup 20 cells - swap ( base xt sp rp )
   lit texit -!     \ push a return address of texit
   rot -!           \ push a return address of passed xt
   swap -!          \ push the new sp
   over >rp !       \ store our new stack in task struct 
   runners over >list ! \ put list head in task struct
   dup ioff runners l_queue ion \ add our new task struct to tail of list
;

\ ***************************************
\ Channels
\ one-way asynchronous streaming IPC
\ ***************************************

{
  8       constant BUFZ    \ size of buffer in cells
  BUFZ 1- constant MASK    \ offset mask

  0 ( this might be a dlist later... )
  sema field >sema         \ access semaphore
  cell field >size         \ how many bytes in buffer
  cell field >pos          \ input data pointer
  BUFZ cells field >buffer \ data buffer
  struct chan

  : full? ( channel -- f ) \ flags true on buffer full
     >size @ BUFZ = ;

  : empty? ( channel -- f ) \ flags true on buffer empty
     >size @ 0= ;

  : head ( channel -- a ) \ fetches address to head of buffer
     dup >buffer swap >pos @ MASK and cells + ;

  : tail ( channel -- a ) \ fetches address to tail of buffer
     dup >buffer swap dup >pos @ swap >size @ - MASK and cells + ;  

  : inc ( channel -- ) \ increments buffer
     1 over >size +! 1 swap >pos +! ;

  : dec ( channel -- ) \ decrements buffer
     -1 swap >size +! ;

  public

  : ch! ( x channel -- ) \ send cell to channel
     dup >sema lock
     begin dup full? while dup >sema swait repeat
     tuck head ! dup inc               
     >sema release ;       

  : ch@ ( channel -- x ) \ get cell from channel
     dup >sema lock
     begin dup empty? while dup >sema swait repeat
     dup tail @ swap dup dec 
     >sema release ;
;

  : channel ( "name" -- ) ( -- a )  \ creates an new channel
      chan salloc dup constant 
      chan for false c!+ next drop ;

}



\ ****************************************
\ Debug and Testing Code
\ ****************************************


\ manually make me - the boot task
here task allot runners over >list ! runners l_add

channel test

: task1 ." task1" cr begin test ch@ emit again ;
: task2 ." task2" cr 41 1a for dup test ch! 1+ next ;

: go
   lit task1 spawn . 
   lit task2 spawn .
   quit
;

\ ' go 0 !

bye