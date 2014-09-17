
\ *************************************
\  Block Driver
\    List of Structs:
\    off   size    what
\    0     2       link to next node in list
\    2     2       xt of get sector word
\    4     2       xt of put sector word
\    6     ???     ca of name of driver
\
\   Driver methods:
\    put sector ( addr lowsec hisec drive -- f )
\    get sector ( addr lowsec hisec drive -- f )
\
\ **************************************

l_head bdevs \  list head of block devices    

: dev_>name ( a -- ca ) \ gets a devices name	
  6 + ;  

: dev_get ( a l h d a -- ) \ executes dev's get sector word
   2 + @ exec ;

: dev_put ( a l h d a -- ) \ executes dev's put sector word
   4 + @ exec ;

: dev_by_name ( ca -- a ) \ gets a device struct addr by name
   bdevs @ begin dup while 2dup dev_>name s= if nip exit then @ repeat nip ;

\ 
\ Drivewire Boisy/Becker Iface Block Device
\ 

{

: cksum ( addr -- x ) \ compute a 256 byte checksum
  0 swap 100 for c@+ rot + swap next drop ;

: bkr! ( c -- ) \ send a byte via becker port
  ff42 p! ;

: bkr@ ( -- c ) \ receive a byte via becker port
  begin ff41 p@ until ff42 p@ ;

: getsec ( a l h d - f ) \ get a sector method
  d2 bkr! bkr! bkr! sp@ c@ bkr! bkr! ( a -- )  
  dup 100 for bkr@ c!+ next drop
  cksum sp@ c@ bkr! bkr!
  bkr@
;

: putsec ( a l h d - f )   \ This just returns an error
  2drop 2drop -1 ;    

\ "create" the block device structure for this device
here
0 ,             \ list link
' getsec ,      \ xt to getsec
' putsec ,      \ xt to putsec
" dwbecker" s,  \ string name
bdevs l_tail_add  \ add to block drive list

}


\ ************************************
\  Filesystems
\    off   size   what
\    0     2      list link
\    2     2      xt to mount method 
\    4     2      xt to unmount method
\    6     ??     string name of system
\ ************************************

l_head fsys   \ list head of filesystems

: fsys_>name 6 + ;

: fsys_by_name ( ca -- a ) \ get filesystem struct by name
   fsys @ begin dup while 2dup fsys_>name s= if nip exit then @ repeat nip ; 


\ ***************************
\ RSDOS filesystem
\ ***************************

{ 

133 constant fat_sect     \ the FAT sector
134 constant dir_sect     \ the first DIR sector
20  constant dirent_size  \ number of byte in DIR entry
9   constant sec/track    \ sectors per granule

create dbuff 100 allot
create nbuff 14 allot

: mount ( a -- xt )

here
0 ,		\ list link
' mount , 	\ mount method
' umount , 	\ umount method
" rsdos" s,     \ file system name
fsys l_tail_add  \ add to filesystem list

}


\ ****************************************
\ Cell Buffer
\   This works as a queue a cell
\   based FIFO
\ ****************************************

0
cell field >head    \ head offset of queue
cell field >size    \ size of queue
cell field >msize   \ max size of queue
struct buff

{

  : applymask ( o buff -- o ) \ applies offset mask
     >msize @ 1- and ;      

  : base ( buff - baddr ) \ returns base address of buffer
     buff + ;

  : haddr ( buff -- a ) \ returns real head address
     dup base swap dup >head @ swap applymask cells + ;

  : taddr ( buff -- a ) \ returns real tail address
     dup haddr swap >size @ cells + ;

public

  : full? ( buff -- f ) \ return true if channel is empty
     dup >size @ swap >msize @ = ;

  : empty?  ( buff -- f ) \ return true if channel is full
     >size @ 0= ; 

  : >b ( x buff -- ) \ put cell onto buffer
     tuck taddr ! 1 swap >size +! ;  

  : b> ( buff -- x ) \ get cell from buffer
     dup haddr @ swap dup -1 swap >size +! 1 swap +! ;

}




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
  cell field >writers      \ writers waiting list
  cell field >readers      \ readers waiting list
  BUFZ cells field >buffer \ data buffer
  struct chan

  : full? ( channel -- f ) \ flags true on buffer full
     >size @ BUFZ = ;

  : empty? ( channel -- f ) \ flags true on buffer empty
     >size @ 0= ;

  : fwait ( channel -- ) \ waits till channel is writeable
     dup full? 
        if ioff 
           dup >sema release
           >writers wait exit 
     then drop ;

  : ewait ( channel -- ) \ waits till channel is readable
     dup empty? 
        if ioff 
           dup >sema release
           >readers wait exit 
     then drop ;

  : head ( channel -- a ) \ fetches address to head of buffer
     dup >buffer swap >pos @ MASK and cells + ;

  : tail ( channel -- a ) \ fetches address to tail of buffer
     dup head swap >size @ cells - ;

  : inc ( channel -- ) \ increments buffer
     1 over >size +! 1 swap >pos +! ;

  : dec ( channel -- ) \ decrements buffer
     -1 swap >size +! ;

  : rwake ( channel -- ) \ wake read task
     dup >readers l_dequeue dup 
          if wake drop exit 
          then drop >sema release ;    

  : wwake ( channel -- ) \ wake writer task
     dup >writers l_dequeue dup 
          if wake drop exit 
          then drop >sema release ; 

  public

  : ch! ( x channel -- ) \ send cell to channel
     dup >sema lock      \ lock channel
     dup fwait           \ wait until not full
     tuck head !         \ data add to buffer
     dup inc rwake ;     \ inc pos and wake reader

  : ch@ ( channel -- x ) \ get cell from channel
     dup >sema lock      \ lock channel
     dup ewait           \ wait until not empty
     dup tail @          \ get data
     swap dup dec        \ inc dec pos
     wwake ;             \ wake writers
;

  : channel ( "name" -- ) ( -- a )  \ creates an new channel
      chan salloc dup constant 
      chan for false c!+ next drop ;

}

