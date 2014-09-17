; ------------------------------------------------------------------------
; CoCoBoot - A Modern way to boot your Tandy Color Computer
; Copyright(C) 2013 Brett M. Gordon beretta42@gmail.com
;
; This program is free software: you can redistribute it and/or modify
; it under the terms of the GNU General Public License as published by
; the Free Software Foundation, either version 3 of the License, or
; (at your option) any later version.
;
; This program is distributed in the hope that it will be useful,
; but WITHOUT ANY WARRANTY; without even the implied warranty of
; MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
; GNU General Public License for more details.
;
; You should have received a copy of the GNU General Public License
; along with this program.  If not, see <http://www.gnu.org/licenses/>.
; ------------------------------------------------------------------------


	include	cocoboot.def

	org	SADDR
start
	ldx	#gmod
  	ldd	#0x40
	jsr	rprim		; register "test"

	ldx	#pwat
	ldd	#0x41
	jsr	rprim		; register "pw@"

	ldx	#pwbang
	ldd	#0x42
	jsr	rprim		; register "pw!"

	ldx	#call
	ldd	#0x43
	jsr	rprim		; register "call"

	ldx	#exem
	ldd	#0x44
	jsr	rprim		; register "exem"

	ldx	#keyq		
	ldd	#0x45
	jsr	rprim		; register "key?"

	ldx	#bsw
	ldd	#0x46
	jsr	rprim		; register "bswp"

	ldx	#llioff
	ldd	#0x47
	jsr	rprim	

	ldx	#toram		; register "toram"
	ldd	#0x48
	jsr	rprim
	
	ldx	#mv		; register "mv"
	ldd	#0x49
	jsr	rprim	

	ldx	#dwread		; register "dwread"
	ldd	#0x4a
	jsr	rprim

	ldx	#dwwrite	; register "dwwrite"
	ldd	#0x4b	
	jsr	rprim	

	ldx	#modinit	; register "modinit"
	ldd	#0x4c		
	jsr	rprim

	ldx	#modread	; register "modread"
	ldd	#0x4d		
	jsr	rprim

	ldx	#modterm	; register "modterm"
	ldd	#0x4e		
	jsr	rprim

	ldx	#modoff		; register "modoff"
	ldd	#0x4f
	jsr	rprim

	ldx	#adc		; register "adc"
	ldd	#0x50
	jsr	rprim

	jmp	goforth		; go run forth


       
gmod				; get mod table address
	ldd	#mod
	pshu	d
	jmp	next

pwat				; fetch a word from primitive space
	clra
	ldd	[,u]
	std	,u
	jmp 	next

pwbang				; store a word from primitive space
	pulu	y
	pulu	d
	std	,y
	jmp	next


;;; Calls argument ptr is formatted as follows:
;;; offset   size     what
;;; 0        2        address of function to call (pxt)
;;; 2        2        D register
;;; 4        2        X register
;;; 6        2        Y register
;;; 8        2        U register

call   ; ( @args -- f ) \ a is address of argument struct
        pulu    y
	pshs	x,u    ; save IP,SP
	tfr	y,u
	ldx	#cret   ; create a "ret" address
	pulu	d	; push func address and return onto call stack
	pshs	d,x	; 	
	pulu	d,x,y   ; load up registers
	ldu	,u
	rts		; jmp to routine

cret
	puls	x,u	; restore IP,SP
	clra
	pshu	d
	jmp	next


exem
	pulu	y	; y is pxt
	pshs	x,u	; save register
        jsr     ,y 	; pull exe address from stack
	puls   	x,u	; restore registers
	jmp     next	; move on to next op

keyq   ; ( -- c )
	jsr	0xa1cb  ; call BASIC's keyin routine
	clrb
	exg	a,b
	pshu	d
	jmp 	next


bsw ; ( x -- x ) \ bytes swap high and low bytes of TOS
        pulu    d
	exg	a,b
	pshu	d
	jmp	next

llioff ; ( -- ) \ turn off low level interrupts
        orcc  #0x50
	jmp   next

toram ; ( -- ) \ move basic ROM to RAM
        pshs    x
        ldx     #0x8000          Start of ROM
copyrom sta     >0xffde          Switch to ROM page
        lda     ,x
        sta     >0xffdf          Switch to RAM page
        sta     ,x+
        cmpx    #0xe000          End of ROM
        bne     copyrom
	puls	x
	jmp 	next

mv ; ( s d u -- ) \ move memory
        pshs    x,u
	pulu	d,x,y
	tfr	x,u
	tfr	d,x
	cmpx	#0
	beq	out	
loop	lda	,y+
	sta	,u+
	leax	-1,x
	bne	loop
out	puls	x,u
	leau	6,u
	jmp	next


dwread ; ( a u -- cksum f )
        pshs    x		; save IP
	pulu	y    		; load up registers
	pulu	x		;
	jsr	DWRead 		; call dwread
	pshu	y		; push cksum
	bcs	@err		; error if framing err
	bne	@err		; error if not all bytes received
	ldd	#0		; else no error
	bra	@out
@err	ldd	#-1		
@out	pshu	d	
	puls	x		; restore IP
	jmp 	next		; go back to forth

dwwrite ; ( a u -- )
	pshs    x		; save IP
	pulu	y		; no of bytes
	pulu	x		; address
	jsr	DWWrite		; call write
	puls	x		; restore IP
	jmp	next		; go back to forth

; call module's init routine
modinit ; ( -- f )
	pshs   x		; save the IP
	ldd    mod+9
	addd   #mod
	tfr    d,x
	ldy    #modtab	
	bsr    sub
	bsr    sub
	bsr    sub
	bsr    sub
	ldy    #modtab
	jsr    [,y]
	puls   x
	bcs    err
	ldd    #0
	bra    out1
err	ldd    #-1
out1	pshu   d
	jmp    next
sub	ldd    ,x++		; fetch init
	addd   #mod		; calc abs address
	std    ,y++		; save in table
	rts    	

modread ; ( l h a -- )
	ldy    #modtab
	jsr    [2,y]	
	jmp    next		; go back to forth    

modterm ; ( -- )
	ldy    #modtab
	jsr    [4,y]
	jmp    next		

modoff ; ( -- offset )
        ldy   #modtab
	jsr   [6,y]
	jmp   next

adc ; ( x x -- c x ) \ add with carry
        ldd    ,u
	addd   2,u 
	std    ,u
	ldd    #0
	rolb
	std    2,u
	jmp    next


	include ../6809/legs16.a


BBOUT       equ    $FF20
BBIN        equ    $FF22

	include	dwread.asm
	include dwwrite.asm

;; this table gets filled out by a
;; call to modinit, which precalculates
;; the jmp addresses in the mod
modtab
	rmb	8
	

mod	

	end	start