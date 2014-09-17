\ 
\ This is the core of Legs Forth
\ 

: true    -1 ;
: false   0 ;
: noop    ;				 \ a no operation
: cr	  d emit ;			 \ prints a cr
: space   20 emit ;			 \ prints a space
: ba	  here 0 , ;   	      	    	 \ puts a back reference on stack
: imm     1 latest >flag ! ;		 \ mark latest word  immediate         	
: then	  here swap ! ; imm		 \ then	
: '					 \ find a name
          word find 0= 0bra [ ba ] 
	  wnf quit then ;
: pp	  ' , ; imm	                 \ compile word (ignoring imm flag)
: ^       lit lit , pp pp lit , , ; imm	 \ compile word ( later )
: if	  ^ 0bra ba ; imm     	    	 \ if	
: else	  ^ bra ba swap pp then ; imm	 \ else	
: begin   here ; imm	   	  	 \ begin	
: again   ^ bra , ; imm		         \ again
: until   ^ 0bra , ; imm       		 \ until
: while   pp if ; imm			 \ while
: repeat  swap pp again pp then ; imm	 \ repeat
: for	  ^ push here ^ dofor ba ; imm	 \ for
: next	  pp repeat ; imm     	   	 \ next
: r@      rp@ cell+ @ ;         \ copy top of return stack


: cell-	  cell - ;    			 \ decriment TOS by a cell


\ ( a x -- a ) push a software stack 
: -!      swap cell- tuck ! ;            

\ ( a b c -- b a c ) swap stack items 1 and 2
: 12swap  rot swap ;

\ ( a b c -- c b a ) swap stack items 0 and 2
: 02swap  -rot swap ; 

: char? \ returns the ascii code for first letter in next word
  ^ lit word @+ drop c@ , ; imm

: ptib         \ returns address of general parse buffer
   tib 40 + ;  \ locate past the word buffer

: parse \ ( c -- ca ) gets chars from input until char c is found
  	ptib 0 !+ 	
	begin over ekey tuck - while 
      	c!+ 1 ptib +! repeat 3drop ptib ;

: (  \ embedded comment
     char? ) parse drop ; imm

: slit	  pull dup @+ + push ;       	 \ puts a string literal on stack
: "       char? " parse ;                \ makes a immediate mode string
: p"      here " s, ;                    \ compiles a string
: s"	  ^ slit " s, ; imm	         \ comiles a string literal
: n"	  " @+ for c@+ c, next drop ; 	 \ compiles just string bytes

\ append c to ca
: astr ( c ca -- ) 
   dup push @+ + c! 1 pull +! ;

: defer   ^ noop ; imm   	         \ defers a word's definition

: [is] ( xt1 xt2 -- )   \ xt1 action is replace with xt2
    !+ lit exit swap ! ;
: is ( xt "name" -- )
     ' swap [is] ;

: do&forget ( xt -- ) \ do xt and reset cp to before xt
     dup exec cp ! ;



: xlit
   pull @+ over + push ;

: [[      here ]    ;                    \ starts a quote

: ]]      pp ; ; imm                     \ ends a quote

: {{  \ strart compiled quote
  ^ xlit ba ; imm

: }}  \ resolve compiled quote
  ^ exit here cell - over - swap ! ; imm

: type
    @+ for c@+ emit next drop ;

: ."	  pp s" ^ type ; imm   		 \ prints a string literal
: .(	  char? ) parse type ; imm	 \ prints a string literal

: utoc ( u -- c ) \ converts digit to ascii
  dup a - 0< if 30 else 57 then + ;

: u.	( x -- ) \ print unsigned number	
  dup f and utoc swap shr shr shr shr ?dup if u. then emit ;

: .       u. space cr ;
: depth	  sp@ 7f80 swap - shr ;


: bemit dup shr shr shr shr utoc emit f and utoc emit ;
: wemit sp@ dup c@ bemit char+ c@ bemit drop ;

: dump	  ( a -- )			 \ dump memory 
   cr dup wemit ." :" cr
     8 for
        8 for
	   dup c@ bemit space char+
        next
        8 -
        8 for
           dup c@ dup bl? if drop 2e emit else emit then char+
        next cr
     next
  cr drop
;

\  
\  Replace the reset vector with
\  something nicer
\

[[ ." Legs Forth - 16 bit" cr ." ok" cr quit ]] 0 !

\
\ Replace WNF with a nicer version
\

[[ cr ." *** Word Not Found: " type cr bye ]] is wnf

: interact ( -- ) 
   lit pp wnf {{ cr ." *** Word Not Found: " type cr quit }} [is] ;

: =     xor 0= ;  \ compares value to equality
: <>	= com ; \ compares value for inequality
: u<    2dup xor 0< if nip 0< exit then - 0< ;
: u>    swap u< ;

\ Signed Cell Compares
: <    2dup xor 0< if drop 0< exit then - 0< ;
: >    swap < ;
: >=   < com ;
: <=   2dup = -rot < or ;

\
\ for a *slightly* better interactive experience
\ make backspace do something
\ This replaces "word"
\

: bl? 21 u< ;  \ this is used to make key that return -1  work!
\ ( -- ca ) \ get next word from input stream
[[
        tib 0 !+ 
	begin ekey dup bl? while drop repeat
	\ exit with empty buffer if source closed
	dup 0< if 2drop tib exit then 
	begin 
	      dup 8 = here @ and if 
	      	  drop 1- -1 tib +!
	      else	
	          c!+ 1 tib +! 
              then
	      ekey dup dup dup bl? swap 8 <> and swap 0< or
	until 2drop tib 
]] is word

: docreate    \ runs xt ( addr -- ) mem: docreate xt data....
   r@ cell+ pull @ exec ;
    
: create  ( "name" -- @xt ) \ creates a word that does something when called :)
    header ^ docreate ^ exit ;

: does> ( -- ) \ resolves the xt address
   pull latest >xt @ cell+ ! ;

: variable ( x "name" -- ) \ create a variable "name" init'd to x
  create , ;

: constant ( x "name" -- ) \ create a constant "name" init'd to x
   create , does> @ ;

\ this is not proper!  *** it is implimentation dependent ***
: cells   shl ;

: allot  ( x -- ) \ allots x bytes to 
   here + cp ! ;

\ : salloc ( u -- a ) \ returns address of alloted bytes 
\    here swap allot ;

\ : free ( a -- ) \ frees alloted bytes
\    drop
\ ;

interact bye


\ ******************************************
\ Structures
\ ******************************************

: field ( u u -- u ) \ defines a field in a structure
   over + swap create , does> @ + ;
: struct ( u -- ) \ defines a structure
   constant ; 


\ *************************************************
\ Vocabularies
\ *************************************************

create vstack 8 cells allot		\ search order stack

latest \ stack last vocab word for breaking list apart

vstack variable vp    			\ vocabulary pointer

: vocabulary ( "name" -- ) \ creates a vocabulary
    create 0 , does> vp @ ! ;

vocabulary legs  \ these are the primitive definitions
vocabulary forth \ these are the base system
vocabulary vocab \ just the vocabulary words

0 variable compile-list \ where to compile new definitions

: also ( -- ) \ duplicates top of search stack
  vp @ dup @ swap cell+ dup vp ! ! ;

: only ( -- ) \ reset vocabulary stack and vocabulary words
   vstack vp ! vocab ;

: previous ( -- ) \ drops top of search stack
   vp @ cell- vp ! ;

: definitions ( -- ) \ sets new definitions to go to top of search stack
   vp @ @ compile-list ! ;

: words
     vp @ begin
     	dup @ @ begin dup while dup >name type space @ repeat drop cr
     cell- dup vstack u< until drop
;


: newfind ( ca -- ca 0 | xt 1 | xt -1 )  
    vp @ begin
     	2dup @ dfind ?dup if 
              \ return found ( ca vp da )
	      nip nip dup >xt @ swap >flag @ if 1 else -1 then exit then
     cell- dup vstack u< until drop 0
; 

: newdh ( -- lh ) \ new dh
    compile-list @ ;     


[[
   \ redefine find
   lit find lit newfind [is]

   \ setup search list
   only legs also forth definitions also vocab

   \ 
   \ split existing dictionary into three vocabularies
   \ 

   \  find first "forth" word
   latest @ begin dup @ >xt @ ff u> while @ repeat 
   \ make it the head to the "legs" vocab
   dup @ vstack @ ! 0 swap !

   over dup @ vstack cell+ @ ! 0 swap !

   \ make latest head to "vocab"
   latest vstack cell+ cell+ @ !

   \ redefine dh 
   lit dh lit newdh [is]

]] do&forget

only vocab also legs also forth definitions


\  ********************************************
\  Now We're gonna split the dictionary
\  and code area up.
\  ********************************************

cp0 constant host

host    variable cpp   \   pointer to code pointer
host    variable dpp   \   pointer to dictionary pointer

\ This is a new CP method
[[  cpp @ ]]

: dp 	 dpp @ ;
: dhere	 dp @ ; 
: d,	 dhere swap !+ dp ! ;
: dc, 	 dhere swap c!+ dp ! ;


: ds, ( ca -- ) \ compile string to dictionary area
  @+ dup d, for c@+ dc, next drop ;	 

\ This is a slighly modified "header" 
\ to work with separate dictionary / compile areas
[[ 
  dhere latest d,  dh !   \ link
  dhere 0 d,              \ xt
  0 d,                    \ compile blank flag
  word ds,		  \ compile word new
  here swap !		  \ resolve xt
]]

\ install new methods

here host ! 		\ setup "host" cp 
is header		\ set new :
is cp			\ set new cp


\  *************************************************
\  Local Names
\  This forth's "locals" doesn't refer to stack data
\  "locals" refers to local word definitions
\  for creating temporily named "helper" words
\  *************************************************

memz 500 - constant localarea  \ The start of the local headers area
localarea variable lp          \ the local header pointer (like CP )
vocabulary localdefs           \ and the list head


\ This starts a "local block" - all definitions headers will be
\ compiled to a separate area, and linked in to the localdefs
\ vocabulary
: {  ( -- lp lh )
     also localdefs definitions  \ add search list and compile vocab
     lp dpp !                    \ compile headers to local area
     lp @                        \ copy of lp for later reversion
     vp @ @ @			 \ copy of list head for later revert
;


\ Pulic changes the compile area back to regular - but leaves the
\ newly defined locals findable
: public ( -- )
    previous definitions also localdefs   \ change compile list to reg
    host dpp !                            \ compile headers back to host area
;


\ and this ends the local block, it earses the headers of the 
\ locals ( not the code ) and resets the list head
: } ( lp lh -- )
   vp @ @ !			\ restore local list head
   lp !                         \ restore localarea pointer
   previous definitions         \ change search list back
   host dpp !			\ compile header back to host area
;



\ ***************************************
\ memory move, compare, fill
\ ***************************************

: mv ( d s u -- ) \ move u bytes at a1 to a2
   for c@+ 12swap c!+ swap next 2drop ;

: cmp ( a1 a2 u -- f ) \ compare u bytes
   for c@+ push swap c@+ pull <> if 2drop false unloop exit then next
   2drop true ;

\ ***************************************
\ Simple run-time debug
\ ***************************************

: de.  ( -- "out" ) \ prints debug and stack depth
   ." debug: " depth . cr sp@ dump ;


\ ******************************
\ Dynamic Memory Allocation
\ ******************************

{

0 
cell field >next
cell field >flags
struct mlist

: size ( a -- u ) \ returns effective size of chunk
   dup @ swap - mlist - ;

: >data ( a -- a ) \ return chunk data field address
   mlist + ;

: >ca ( a -- a ) \ converts a data field address to chunk address
   mlist - ; 

: mark ( a -- ) \ marks chunk as used
   >flags dup @ mint or swap ! ;

: unmark ( a -- ) \ marks chunk as free
   >flags dup @ mint com and swap ! ; 

: used? ( a -- f ) \ returns true if chunk is used
   >flags @ 0< ;

: free? ( a -- f ) \ returns true if chunk is free
   used? 0= ;

: last? ( a -- f ) \ returns true if chunk is last in list
   @ @ 0= ;

: nconsol ( a -- ) \ consolidates chunk with next chunk
   dup @ @ swap ! ;   

: split ( u a -- ) \ split chunk to u bytes
   tuck + mlist +    \ calc new chunk address ( a n )
   dup unmark        \ mark new chunk as free ( a n )
   over @ over !     \ init new chunks next field ( a n )
   swap !            \ mod parent chunk's next field
;

: find ( u a1 -- a2 ) \ find node big enough for u bytes
   begin 2dup size u> 0= over free? and if nip exit then 
   dup last? 0= while @ repeat 2drop false ;

: compact ( a -- ) \ compact heap list
    begin dup last? 0= while   
      dup free? over @ free? and if dup nconsol else @ then
    repeat drop ;

public

\ allocate u bytes of data on a heap starting with
\ node address a1
: alloc ( u a1 -- a2 ) 
   2dup find  ( u a a )
   \ if no chunk big enough then compact
   \ and try again
   dup 0= if drop dup compact 
      2dup find ( u a a )
      dup 0= if nip nip exit then 
   then
   \ mark chunk as used
   nip dup mark  
   \ split chunk if big enough
   2dup size swap - mlist cell+ u> if 2dup split then 
   \ return data field
   nip >data
;


\ dallocate dynamic memory a
: free ( a -- ) 
    dup >ca free? if ." dup free!" cr drop exit then
    ?dup 0= if exit then >ca unmark ;

: .h ( a -- ) \ print heap
   begin
     dup wemit space dup size wemit space dup @ wemit space dup used? wemit cr 
     dup last? 0= while @
   repeat drop ;


\ address size 

}

\ allots and inits  named chunk
: dchunk ( u "name" -- ) \ initialize chunks
   create ba false , swap allot here swap ! false , ; 

600 dchunk heap

: salloc ( u -- a ) \ allocate system heap
   heap alloc ?dup 0= if ." out of memory" cr quit then 
;


: .heap 
    heap .h ;


: debug ( x -- ) \ print debug message
   ." debug " . ;


\ ***********************************
\ objects
\ ***********************************

0 
cell field >csize  \ size of this class structure
cell field >msize  \ size of object's member data
cell field >parent \ ptr to parent's class struct
cell field >vocab  \ class's vocabulary (linked with parent's)
0    field >vmt    \ Method table ( array of xt's )
struct class


create vmt 32 cells allot  \ class assembly area
0 variable obj             \ run-time object pointer
0 variable ovp		   \ saved vp pointer

: calloc ( u -- a ) \ compile alloc
   here swap allot ;

: redef ( xt "name" -- ) \ redefined a dictionary xt
   word
   vp @ begin
    2dup @ dfind ?dup if
       \ if found ( xt ca vp da )
       nip nip >xt ! exit then
    cell- dup vstack u< until  
;

: svvp ( -- ) \ saves vp in ovp var
  vp @ ovp ! ;

[[ svvp : ]] redef :
[[ ovp @ vp ! pp ; ]] redef ;

: self ( -- object ) \ puts self-object on stack
  obj @ ;

: method ( "name" -- ) ( x*i obj -- x*j )
   svvp
   \ play with the class assembly area
   create vmt @ ,                   \ create name to offset
   here vmt dup @ + !               \ set xt in vmt
   cell vmt +!                      \ increment vmt size
   ]                                \ start compiling method
   \ make a run-time action for new method
   does> self push @ swap dup obj ! @ + @ exec pull obj ! ;  

: member ( size "name" -- ) ( -- a )
   create vmt >msize @ ,            \ create name with offset
   vmt >msize +!                    \ incrmenet vmt member size
   \ make a run-time action for new member
   does> @ self + ;

: use ( "class" -- ) \ use a class vocabulary
    pp ' exec also >vocab vp @ ! ; imm

: flz ( -- ) ( -- class ) \ finalizes a class
    previous definitions  
    vmt @ calloc dup vmt vmt @ mv ;

: class ( "name" -- ) ( -- class ) \ finalizes class and names
    flz constant 
;

: extend ( class -- ) \ copies class to class building area
   dup
   vmt swap dup @ mv       \ copy class struct 
   vmt >parent !           \ fill in new class's parant ptr
   also vmt >vocab vp @ !  \ use new vocabulary
   definitions
;


: as ( "name" -- ) \ overrides parent method
    svvp here vmt pp ' cell+ cell+ @ + ! ] ;

: parent ( "name" -- ) \ compiles parent's method
   pull dup cell+ push @
   self dup @ dup push >parent @ swap !
   self swap exec pull self ! ;


\
\ a hand-build base object class
\
    
vmt 
4 cells !+    \ class size
  cell  !+    \ member size
0       !+    \ parent class ( NULL in obj's case )
0       !+    \ vocab head
also vmt >vocab vp @ ! definitions  \ use new vocab
method init true ; ( -- f ) \ initializes object
method deinit true ; ( -- f )      \ deinitializes object
class obj

: new ( i*x class -- object ) \ makes an object 
    use obj
    dup >msize @ salloc tuck ! 
    dup push init if pull exit then
    pull free false
;



: destroy ( object -- ) \ destroys an object
    use obj
    dup deinit if free else drop then
; 


: single ( "name" -- ) \ finalizes class and creates a signalton object
    flz new constant 
;



\ **********************************
\ Abstract Containers
\ **********************************

obj extend
  method add ; ( item -- )      \ add an object to the container
  method giter ; ( -- iter )    \ get an iterater object for the container
  method no ; ( -- u )          \ number of items in container
class container

obj extend
  method next? ; ( -- f )    \ is there another value?
  method inext ; ( -- )      \ position to new value
  method gobj  ; ( -- obj )  \ get item
class iter

\ xt ( item -- f ) 
: iforeach ( iter xt -- f ) \ iterate through container
   use iter
   swap 
   begin dup next? while 
     2dup push push gobj swap exec 
       if pull pull 2drop true exit then
       pull pull dup inext
   repeat 2drop false ;

\ xt ( obj -- f ) 
: foreach ( container xt -- item | 0 ) \ iterate through container
   use container use iter
   swap giter dup push swap iforeach 
   if r@ gobj  
   else false  
   then pull destroy ;

\ ****************************
\ Association List
\ ****************************

obj extend
   as init ( ca -- f )
   ;
   method gkey ( -- ca )            \ gets key value
   ;
   method gval ( -- x )             \ gets value
   ;
class assoc

\ find item by key 
\ returns false if key is not found
\ val may be a double!!
: findbykey ( ca container -- val -1 | 0 ) 
    use assoc
    {{ gkey over s= }} foreach nip 
    dup if gval true then ;

\ list assoc list's key value's
: listkeys ( container -- )
   use assoc
   {{ dup gval wemit space gkey type cr false }} foreach drop ;


\ ********************************* 
\ Simple Linked List Container
\ *********************************


obj extend
   cell member nexti   \ ptr to next list item
   cell member ref     \ ptr to contained object
   as init ( obj head -- f )  
    nexti ! ref ! true ;
   method gnext ( -- a ) \ get ptr to next list item
     nexti @ ;
   method gref  ( -- obj ) \ get ptr to contained object
     ref @ ;
class litem

iter extend
  cell member addr        \ the current addr
  as init ( head -- f )   \ init an iterator
    addr ! true ;
  as next? ( -- f )    \ is there another value?
    addr @ ;
  as inext ( -- )      \ position to new value
    use litem
    addr @ gnext addr ! ;
  as gobj ( -- obj )  \ get item
    use litem
    addr @ gref ;
class liter

container extend
   cell member head  \ head of list
   cell member count \ number of items

   as init ( -- f )
     0 count ! 0 head ! true ;

   as add ( obj -- )
     head @ litem new head ! 1 count +! ;

   as giter ( head -- iter )
     head @ liter new ;

   as no ( -- u ) 
     count @ ;
class list


\ **************************
\ Memory Allocated Assocition List
\ **************************

assoc extend
  cell member key	            \ key value ptr storage
  as init ( list ca -- f )
    use list
    key ! self swap add true 
  ;

  as gkey ( -- ca )            \ gets key value
    key @ 
   ;

  as gval ( -- obj )           \ gets value
    self ;

class massoc


\ ***********************************
\ Block Device Objects
\ ***********************************

0 
cell field >low
cell field >high
cell field >drive
cell field >device
struct baddr


list new constant cdevs

massoc extend
   as init ( ca -- f )   \ init charactor device
     cdevs swap parent init ;
   method get ( -- c ) ; \ get charactor
   method put ( c -- ) ; \ put charactor
class cdev 

\ ****************************************
\ Becker Character Device
\ ****************************************

cdev extend
   as init 
      parent init ;
   as put ( c -- ) \ send a byte via becker port
      ff42 p! ;
   as get ( -- c ) \ receive a byte via becker port
      begin ff41 p@ until ff42 p@ ;
class becker


\ **********************************
\ Block Devices
\ **********************************

list new constant bdevs

massoc extend
   as init ( ca -- f )           \ init block device
     bdevs swap parent init ;
   method get ( baddr a -- f ) ; \ get sector
   method put ( baddr a -- f ) ; \ put sector  
class bdev

\ *********************************
\ DriveWire 4 block device
\ *********************************

bdev extend 
    cell member device     \ which charactor device to use

    as init ( ca_name ca_cdev -- f )
      cdevs findbykey 
         if device ! parent init
         else drop false then
    ;
            

   : bkr! ( c -- )
      use cdev device @ put ;

   : bkr@ ( -- c )
      use cdev device @ get ;

   : cksum ( addr -- x ) \ compute a 256 byte checksum
      0 swap 100 for c@+ rot + swap next drop ;

   : senda  ( baddr op -- ) \ send opcode and address
      bkr!                 \ send opcode
      dup >drive @ bkr!    \ send drive no
      dup >high @ bkr!     \ send hi sector
      >low c@+ bkr!        \ send low sector msb
      c@ bkr!              \ send low sector lsb
   ;

   : sendck   ( a -- f )  \ send cksum
      cksum sp@ c@+ bkr! c@ bkr! drop bkr@ ;

   as get ( baddr a -- f ) \ get a drive sector
      swap d2 senda        \ send readop
      dup 100 for bkr@ c!+ next drop
      sendck
   ;


   as put ( baddr a -- f )   \ put a drive sector
      swap 57 senda         \ send drive write op
      dup 100 for c@+ bkr! next drop
      sendck
   ;
    
class dw4 


\ ************************************
\ Caching
\ ************************************

100 constant sector      \ size of a disk sector
list new constant cache  \ list of cobs

obj extend {
   sector member data    \ databuffer
   baddr  member addr    \ address of cob 
   cell   member count   \ lock count
   cell   member time    \ time of release
   cell   member dflag   \ dirty flag

   0 variable rtimer        \ release timer      

   : fill ( -- )  \ fill cob's data buffer
      use bdev addr data over >device @ get drop ;

   : write ( -- ) \ writes cob's data back 
      use bdev addr data over >device @ put drop ;

   public

   method reassign ( baddr -- ) \ reassign 
      addr swap baddr mv   \ copy addr
      fill                 \ fill data from device driver
      1 count !            \ set count to 1 lock
      0 time !             \ reset time
   ;       

   as init ( baddr -- f )
      self reassign
      use container
      self cache add       \ add self to cache
      true
   ;

   method lock ( -- ) \ obtain lock on cob
      1 count +! ;

   method rel ( -- ) \ release lock on cob
      count @ 1- dup count !
      0= if rtimer @ 1+ dup rtimer ! time ! then
   ;

   method daddr ( -- a ) \ return ptr to data
       data ;

   method rtime ( -- u ) \ return time of release
       time @ ;

   method free? ( -- f ) \ returns true if cob is free
       count @ 0= ;

   method dirty? ( -- f ) \ returns true if cob is dirty
       dflag @ ;

   method ccmp ( baddr -- f ) \ return true if baddr=addr
       addr baddr cmp ;

   method dirty ( -- ) \ mark cob as dirty
       true dflag ! ;

} class cob


{

  : csearch ( baddr -- cob | 0 ) \ search for matching cob
      use cob
      cache {{ over swap ccmp }} foreach nip ;

  : free ( -- cob | 0 ) \ search for free cob
      use cob 
      \ find a free cob
      cache {{ free? }} foreach dup 0= if exit then
      \ search list for a free and older cob x
      cache {{ over rtime over rtime u> over free? and
         if swap then drop false }} foreach drop
  ;


  : 4nip ( dev dri high low x -- x ) \ nip 4 times
      nip nip nip nip ;

public

  4 variable cmax          \ maximum cobs in a cache

  : getc ( dev dri high low -- cob | 0 ) \ get a cob 
      sp@

      use cob use list
      
      \ if cache hit then return 
      dup csearch ?dup if 4nip nip dup lock exit then

      \ if cache is less than max size then return with new cob
      cache no cmax @ u< if cob new 4nip exit then

      \ find an old unuse cob and reuse
      free ?dup if tuck reassign 4nip exit then    

      \ and if cache is full on used cobs:
      ." Error: Cache full!" 4nip drop cr quit

  ;

  : relc ( cob -- ) \ releases a cob
      use cob ?dup if rel then  ;


}


\ ****************************
\ Filesystem Class
\ ****************************

list new constant fses   

massoc extend
  cell member fs  \ ptr to filesystem class
  as init ( fs ca  -- f )
    fses swap parent init drop
    fs !
    true ;
  as gval ( -- fs ) \ get the filesystem
    fs @ ;
class fsitem


list new constant mounts

massoc extend
   cell member drive      \ drive no that the fs sits on
   cell member device     \ device that the fs sits on
   cell member count      \ how many open files

   \ init this filesystem under the name ca on device and drive no
   as init ( device drive ca_mount -- f )
      mounts swap parent init drop
      drive ! device ! 
      0 count !
      true ;

   as deinit ( -- f ) 
      count @ if ." FS is busy!" cr false else true ;

   method fsgetc ( d d -- cob ) \ get a cob 
      swap device @ -rot drive @ -rot getc ;

   method iopen ( d d -- file ) \ open an file by ID
      1 count +! ;
 
   method relf ( file -- )  \ closes a file
      -1 count +! destroy ;
 
   method rdir ( -- d d ) \ returns inode of root dir
       ;

   method dopen ( d d -- dir ) \ open an directory by ID
      ;

class fs

: mount ( ca_mount ca_fs ca_bdev dri -- f ) \ mount a filesystem
   swap bdevs findbykey 0= if 2drop drop false exit then
   swap rot fses findbykey 0= if 2drop drop false exit then
   push rot pull new 
;


\ ***************************
\ File Class
\ ***************************

obj extend 
  cell member _fs                   \ on which filesystem this file sits
  
  method geta ( -- a u )            \ gets next sector buffer from file
  ; 
  
  method close ( -- )               \ close file
    use fs self _fs @ relf ;  
  
  as init ( fs -- f )           
    _fs ! true ;

  method isdir ( -- f )             \ is file a directory?
  ; 

class file



\ **************************************
\ Double cell words
\ **************************************
 
: double 2 cells ;  \ size of a double

: dvar ( d d -- a )
   create , , ;

: d+ ( d d -- d ) \ adds two double together
   rot + push tuck + swap over u> pull swap - ;

: d@ ( a -- d ) \ fetches a double
   @+ swap @ swap ;

: d! ( d a -- ) \ stores a double
   swap !+ ! ;

: s>d  ( n -- d ) \ converts n to a d (signed)
   dup 0< if -1 else 0 then ;

: d= ( d d -- f ) \ test for equality
   12swap = -rot = and ;

: demit ( d d -- ) \ print a double 
    wemit wemit ;


\ ************************************
\ OS9 Read Only filesystem
\ ************************************

: 3@ ( a -- d ) \ get 3 byte type
   c@+ swap @ swap ;


file extend
  cell   member  fd             \ file descriptor cob
  cell   member  bf             \ file data buffer cob
  cell   member  fdpos          \ pointer to segment list
  cell   member  count          \ sector count ( in segment list )
  double member  lcount         \ count of logical sectors

  {

  : getc ( d d -- cob )
      use fs  _fs @ fsgetc ;
      
  : rsn ( -- ud )     \ returns lsn of file pointer
     fdpos @ 3@ count @ 0 d+ ;

  : secno ( -- u )  \ returns number of sectors in segment
     fdpos @ 3 + @ ;

  : fdsect ( -- a ) \ returns address to buffer of fd sector
     use cob fd @ daddr ;

  : bsect ( -- a ) \ returns address to data buffer
     use cob bf @ daddr ;

  : oddbytes ( -- u ) \ returns odd bytes in last sector
     fdsect c + c@ ; 

  : lsecno ( -- ud ) \ returns logical number of sectors from descriptor
     fdsect 9 + 3@ oddbytes if 1 0 d+ then ;

  : incsec ( -- ) \ increments sector/segment 
     \ increment fd-segment position vars
     1 count +!
     count @ secno = if  5 fdpos +! 0 count ! then
     \ increment logical sector vars
     1 0 lcount d@ d+ lcount d!
  ;

  public

  : eof? ( -- f ) \ flags for end of file
     lcount d@ lsecno d= if true exit then false ;

  as geta ( -- a u | 0 )
      eof? if false dup exit then	      \ return on EOF
      bf @ relc                               \ release old cubby
      rsn getc bf !                           \ get new cubby
      bsect                                   \ put data address on stack
      incsec eof? if oddbytes else 100 then   \ number of bytes in sector
  ;

  as init ( ud rbf -- f )
      parent init drop         
      getc fd !                     \ load file decriptor sector
      fdsect 10 + fdpos !           \ set segment position
      0 count !                     \ reset sector count 
      0 0 lcount d!		    \ reset logical sector count
      0 bf !                        \ reset data buffer cubby
      true                          \ init'd OK
  ;			       

  as deinit ( -- f ) 
      fd @ relc        \ release file descriptor sector
      bf @ relc        \ release data buffer cob
      true ;

  as isdir ( -- f )  \ is this file a dir?
      use cob
      fd @ daddr c@ 80 and 
  ;

  }
class os9file


assoc extend
   cell member buff    \ pointer to dirent's array
   20   member name    \ current dirent's name
   
   : cpname ( -- ) \ copy dirrent's name to buffer
      0 name ! buff @ begin c@+ dup 7f and name astr 80 and until drop ;

   as init ( a -- f )
     buff ! cpname true ;

   as gval ( -- d d ) \ gets inode of dirrent
    buff @ 1d + 3@ ;

   as gkey ( -- ca ) \ a  
    name ;

   method reinit ( a -- )
     buff ! cpname ;

class os9dirent


iter extend
   cell member fd      \ file associated with this buffer
   cell member buff    \ curent member buffer 
   cell member count   \ how many bytes left in buffer
   cell member dirobj  \ directory object

   : nextb ( -- ) \ get next buffer
      use file fd @ geta count ! buff ! ;

   as init ( fd -- f )     
     fd ! 0 buff ! 0 count !
     nextb 
     buff @ os9dirent new dirobj !
     true
   ;


   as deinit ( -- f )
      dirobj @ destroy 
      true ;

   as next? ( -- f )
      count @ ;

   as inext ( -- )
      20 buff +! -20 count +!
      count @ 0= if nextb then
      self next? if buff @ dirobj @ init drop
      then
   ;

   as gobj ( -- dirent )
      dirobj @ ;

class os9diter

container extend
    cell member fd        \ ptr to file

    as init ( d d rbf -- f )
      use fs use file
      iopen 
      dup isdir if fd ! true else close false then ;

    as deinit ( -- f )
      use file fd @ close true ;

    as giter ( -- diter )
      fd @ os9diter new  
    ;	     
class os9dir


fs extend
   double member root       \ inode of root directory

   as init ( device drive ca -- f )
     use cob
     parent init drop
     0 0 self fsgetc dup 
     daddr 8 + 3@ root d! 
     relc true 
     ;

   as deinit ( -- f )
     count @ if ." FS is busy" cr false else true then ;

   as iopen ( d d -- file ) \ open an inode by number      
     self os9file new dup if 1 count +! then ; 

   as rdir ( -- d d ) \ returns the root inode
     root d@ ;

   as dopen ( d d -- dir ) \ open an dir by ID
     self os9dir new ; 	    

class rbf


rbf p" rbf" fsitem new drop

\ ***************************
\ Testing and Debug
\ ***************************

: aprint ( file -- ) \ print file to console
    use file
    cr begin dup geta dup while 
       for c@+ emit next drop
    repeat 2drop drop
;


{

0 variable buff
0 variable count
create tmp 20 allot

: init ( ca -- ) \ get filename
    @+ count ! buff ! ;

: getc ( -- c ) \ gets next char from string
    count @ 0= if true exit then
    buff @ c@+ swap buff ! 
    -1 count +! ;

: abs? ( -- f ) \ is this an absolute path?
    buff @ c@ char? / = dup if getc drop then ;

: fill ( -- ) \ fills tmp buffer with name
    0 tmp !
    begin getc 
      dup char? / = over 0< or if drop exit then
      tmp astr 
    again
;


0 variable cfs
0 0 dvar   cdir

public

0 variable wfs
0 0 dvar   wdir

: err ( -- 0 ) \ return with error
    pull drop false ;
: derr ( x -- 0 ) \ return with error
    pull drop drop err ;

: lookup ( ca -- d d fs -1 | 0 )
    init abs? if 
      fill tmp mounts findbykey 0= if err then 
      dup cfs ! use fs rdir cdir d!    
    else
      wfs @ cfs ! wdir d@ cdir d!
    then
    begin fill tmp @ while 
      cdir d@ cfs @ dopen dup 0= if derr then 
      tmp over findbykey
      0= if derr then
       cdir d! destroy
    repeat cdir d@ cfs @ true
;


}

: 3dup ( a b c -- a b c a b c ) \ 3 dups 
   push push r@ over pull r@ -rot pull ;

: isdir ( d d fs -- f ) \ returns true if file is a directory
    use fs use file
    iopen dup isdir swap close ;

: chdir ( ca -- ) \ change working directory
   lookup 0= if ." File Not Found" cr exit then
   3dup isdir 0= if 3drop ." Not a Directory" cr exit then
   wfs ! wdir d! 
;

: open ( ca -- file )  \ opens a file
   lookup 0= if ." File Not Found" cr quit then
   use fs iopen ;

: less ( "file" -- ) \ print a file
   word open dup aprint destroy ;

: cd ( "dir" -- ) \ change working directory
   word chdir ;

: pdir ( d d fs -- ) \ prints a directory
   use fs dopen 
   use assoc
   dup
   {{ dup gval demit space gkey type cr false }} foreach drop 
   destroy
;

: ls ( -- )  \ print working directory
   wdir d@ wfs @ pdir ;

: stat 
   ." Character Devices:" cr cdevs listkeys cr
   ." Block Devices:" cr bdevs listkeys cr
   ." Filesystems:" cr fses listkeys cr
   ." Mounts:" cr mounts listkeys cr
;

: sinit ( -- ) \ initialize the system
   \ init the becker device
   s" becker" becker new drop
   
   \ init the dw4 device
   s" dwbkr" s" becker" dw4 new drop
   
   \ mount the root filesystem
   s" root" s" rbf" s" dwbkr" 0 mount drop 
   s" basic" s" rbf" s" dwbkr" 1 mount drop
  
   stat

   s" /root" chdir

;

: cold ( -- ) \ cold restart
  sinit quit ;

' cold 0 ! 

\ **********************************
\ Source Stack
\ **********************************

\ 
\ Console device
\ 

: skey key ;

cdev extend
   as init ( -- f )
      s" cckb" parent init ;
   as put ( c -- ) \ send a byte via becker port
      emit ;
   as get ( -- c | -1 ) \ receive a byte via becker port
      skey dup emit ;
class cckey

\ 
\ File source - makes a file look like a cdev
\ 
cdev extend
   cell member fd      \ file object ptr
   cell member count   \ no of bytes in buffer
   cell member addr    \ address of current buffer

   : fill ( -- )   \ fills buffer
      use file fd @ geta count ! addr ! ;

   : getb ( -- c ) \ gets byte from buffer
      addr @ c@+ swap addr ! -1 count +! ;

   as init ( ca -- f ) \ ca is filename
    open fd ! fill true ;      

   as deinit ( -- f ) 
     use file fd @ close true ;

   as get ( -- c ) \ get a byte
     count @ if getb exit then
     fill
     count @ if getb exit then
     true
   ;

class f2cdev

\ 
\ The source stack
\ 

cckey new constant stdin

8 cells allot here constant ss0  \ source stack base pointer
ss0 variable sptr                \ source stack pointer

: ssres ( -- ) \ reset source stack
   ss0 stdin -! sptr ! ;

: done ( -- ) \ pops top of source stack
   sptr @ ss0 cell- = if exit then
   sptr @ @+ destroy sptr ! ;

: nkey ( -- c ) \ get key from source
   use cdev
   sptr @ @ get dup 0< if done then ;

: load ( "file" -- ) \ load source file
   sptr @ word f2cdev new -! sptr ! ;

ssres 

' nkey is ekey

interact bye
