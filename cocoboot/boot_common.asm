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


;;;  This is the "common" boot code replacement
;;;  for CoCoBoot.  

;;; "start" used to load os9.
;;; cocoboot just uses the modules' start or exec, address
;;; as a table of methods ( a VMT )

;;; Unfortunatley Nitros9 Boot modules are coded wrong -
;;; High level code reaches in to change module memory.
	
	
start
	fdb    CCBinit
	fdb    CCBread
	fdb    CCBterm
	fdb    CCBoffset

	;; Initialize Object
CCBinit	; ( a -- f )
	pshs	x
	tfr	u,y
	ldu	,y++
	pshs	y
	ldy    	Address,pcr
	bsr	HWInit
	;; This Next Code Doesn't belong here -
	;; the floppy module's HWINIT should do it.
	;; More Nitros9 insanity.
	IFNE	FLOPPY		
	;; read lsn0
	clrb
	ldx	#0		; lsn0
	bsr	CCBread
	bcs	CCBerr
	;; copy format data
	lda	DD.TKS,x	; from disk 
	ldb	DD.FMT,x
	std	ddtks,u		; to object memory
	clra
	clrb			; clear return
CCBerr
	ENDC
	puls	u
	puls	x
	rts

	;; Destroy Object
CCBterm	; ( -- )
	ldy	Address,pcr
	bsr	HWTerm
	rts

	;; Read Sector method
CCBread	; ( d a -- ) \ d is sector, a is 256 bytes buffer
	pshs	x
	tfr	u,y
	ldu	,y++
	ldd	,y++
	ldx	,y++
	pshs	y
	ldy	Address,pcr
	bsr	HWRead
	puls	u
	puls	x
	rts

	;; get this module's bootloc "constant"
	;; because os9's boot modules are incestuous
	;; bastard spawn of real drivers,
	;; the constant blockloc is different for
	;; each module (yeah!)
CCBoffset
	ldd	#blockloc
	pshu	d
	rts