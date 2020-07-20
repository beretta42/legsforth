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
;;; This is a slz compression module
;;; slz is a very simple windowing compression format based off lz77.
;;; slz was created by Adisak Pochanayon
;;; http://www.ggdn.net/GameDev/General/article294.asp.htm


	;; this function uncompresses slz data
	;; takes X - address to decode image to
	;; takes Y - address of compressed image
	;; clobbers everything
deslz	
	leas	-1,s		; need for temporary
gtag	ldb	,y+		; get next compression tag
	stb	,s		; save tag
	lda	#8		; repeat for each bit of tag
	
rtag	rol	,s		; rotate in fix byte
	bcs	dict		; if set off to dictionary lookup
	ldb	,y+		; copy one byte
	stb	,x+		; to image
itag	deca			; bump counter
	bne	rtag		; rinse & repeat
	bra	gtag		; go get next tag
	
dict	pshs	a		; save counter
	ldd	,y++		; D=length/offset
	tsta
	beq	exit		; is zero, so end of image, quit unpacking
	lsra
	lsra
	lsra
	lsra			; D=offset
	nega
	negb
	sbca	#0		; D=-offset
	leau	d,x		; y=dict
	lda	-2,y		; get back length
	anda	#15		; mask off extraneous offset bits
	adda	#2		; add count bias
cpy	ldb	,u+		; copy byte from dict
	stb	,x+		; to dest
	deca			; bump counter
	bne	cpy		; repeat
	puls	a		; get back tag counter
	bra	itag		; go back to main loop
exit	puls	d,pc		; remove temporaries from stack and return

;; Notes on the above from William Astle (lost@l-w.ca) about my changes
;;
;; the puls d,pc bit works because the tag counter and the tag byte stack
;; variable are present on the stack; puls d,pc removes both of those to
;; a and b respectively (clobbers them) and then does the expected rts
;;
;; the use of U as the temporary pointer when doing the dictionary
;; decode avoids spilling another register to the stack but it does
;; clobber U in addition to X and Y
;;
;; the original negation for D was coma, comb, addd #1 which, while correct
;; is one byte longer and a couple cycles slower than nega, negb, sbca #0
;;
;; by moving the length calculation below the offset calculation, the need
;; for a temporary to save the length is removed. The stack usage for A
;; in this case was also present prior to modification
;;
;; by using the stack as a temporary to save the current tag byte, 2 bytes
;; are used to set up the stack space and two bytes are used to access it
;; with no additional bytes being needed to clean it up as it combines into
;; the already present puls instruction. However, depending on the assembler
;; and linker, the bss variable previously used might have been accessed
;; using a 16 bit address which gives us a net neutral result but eliminates
;; the need for a bss variable making the routine completely self contained.
;;
;; It is possible to save two more bytes in this routine if (and only if)
;; it can be assumed that DP points somewhere useful that will not clobber
;; anything and there is a free byte somewhere at that location. Assuming
;; DECB is present, one can use 0xFF in the direct page which turns into
;; 0x00FF for a real memory address. In that case, one can remove the
;; leas -1,s and replace both ",s" accesses with "<$FF" (or whatever
;; prefix is needed for direct page access). Then replace the final
;; "puls d,pc" with "puls a,pc".
;;
;; As it stands, this routine assembles to 59 bytes. With the direct page
;; trick above, it can assemble to 57 bytes.

;; 7/20/20 - Routine nows assembles to 57 bytes with an optimization that
;; substitutes a "pshs a" and "puls a" pair with a single "lda -2,y".
;; This is also 7 clock cyles quicker. Doug Masten
