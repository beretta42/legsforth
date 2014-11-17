;;;
;;;  This is a asm patch to HDBDOS
;;;  to autoboot a .bin file rather than .bas
;;;


LINBUF	equ	0x02dc
CHRAD	equ	0x00a6
NAMBUF	equ	0x0000


;;; This code here gets patched to "DOAUTO" in HDBDOS
	
	org	0

	ldx	#NAMBUF		; copy filename to line buffer
	ldu	#LINBUF		; reset BASIC's line buffer
	stu	<CHRAD		; save after M as start
	ldb	#'M		; store a dummy M
	stb	,u+
	ldb	#'"		; store a quote 
	stb	,u+		;
	lda	#8		; copy 8 bytes
loop@	ldb	,x+		; load a byte
	stb	,u+		; move a byte
	deca			; bump counter
	bne	loop@		; loop till done
	clr	,u		; add zero for end of line
	lda	#'M		; make RUNM treat this as ML file
	jmp	[0xc113+(18*2)]	; jump to HDB's "RUNM" 
