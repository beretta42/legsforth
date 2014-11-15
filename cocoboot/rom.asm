; ------------------------------------------------------------------------
; CoCoBoot - A Modern way to boot your Tandy Color Computer
; Copyright(C) 2011 Brett M. Gordon beretta42@gmail.com
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

;;;
;;;  This startup code relocates our booter away from where ever to 0x3800
;;;  this is PIC, but care must be used not put this image (how ever it 
;;;  gets there) too close to 0x3800 (rom addresses is fine, 0x2600 is fine)


	include	cocoboot.def

	org	0xea
DCOPC	rmb     1
DCDRV	rmb     1
DCTRK   rmb     1
DCSEC   rmb     1
DCBPT   rmb     2
DCSTA   rmb     1


FSBASE 	equ	0xc000+18*256

	org	0xc000
	lbra	start		;
	.db	0
	;; Disk con pointers
	.dw	dskcon
	.dw	0xea
	
start	lda	0xff22          ; clear pia interrupt
	ldd	#0x34           ; set pia
	std	0xff23		;   no firq interrupts
	orcc	#0x50		; turn off interrupts
	lds	#0x8000		; setup return stack
	;; Uncompress VM and modules to 3800
	ldx	#SADDR		; x is our destination
	leay	end,pcr		; y is the start of the slz image
	bsr	deslz		; uncompress
	;; uncompress VM's RAM image to 4500
	ldx	#MB		; set destination to 4500
	bsr	deslz		; uncompress
	jmp	SADDR		; jump to uncompressed image

	include	slz.asm		; include unslz routine

	;; Replacement DSKCON: A ROM drive
dskcon	pshs	d,x,u
	clr	0xffde          ; turn rom on
	lda	DCOPC		; load opcode
	cmpa	#2
	bne	ok		; return
	lda	DCTRK		; a = requested drive
	ldb	#18
	mul			; d = track offset
	addb	DCSEC
	decb
	exg	a,b
	clrb
	addd	#FSBASE
	tfr	d,x
	clra			; a is counter
	ldu	DCBPT		; u is desination
	;; move sector
loop	ldb	,x+
	stb	,u+
	deca
	bne	loop
ok	clr	DCSTA		; clear status - ok
	puls	d,x,u,pc	; return


	
end

	includebin run.slz	; include compressed package

	
	END	start 		; specify end for BASIC's loaders
	

