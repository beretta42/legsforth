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



MB	equ	0x4500		; VM memory image starts at 4500
MEMZ	equ	0x8000-MB	; size of memory image

	org	0x3800
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

	jmp	goforth		; go run forth

        
gmod ; ( -- a ) \ get address of module
	ldd	#mod
	pshu	d
	jmp	next

pwat
	clra
	ldd	[,u]
	std	,u
	jmp 	next

pwbang
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


	include ../6809/legs16.a
dw
	fdb	DWRead
	fdb	DWWrite

BBOUT       equ    $FF20
BBIN        equ    $FF22

	include	dwread.asm
	include dwwrite.asm
	

	end	start