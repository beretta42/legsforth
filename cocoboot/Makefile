#------------------------------------------------------------------------
# CoCoBoot - A Modern way to boot your Tandy Color Computer
# Copyright(C) 2013 Brett M. Gordon beretta42@gmail.com
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.
# ------------------------------------------------------------------------


AS=lwasm -I . -9o 
BFC=../bfc/bfc
HDBDIR=~/C/hg
VPATH=images


# These are the HDBDOS overlay images
DIFS= hdbchs.dif     	\
      hdbdw3bc3.dif  	\
      hdbdw3cc3.dif	\
      hdbdw4cc3.dif	\
      hdbtc3.dif	\
      hdbd4n1.dif	\
      hdbdw3bck.dif	\
      hdbdw3jc2.dif	\
      hdbkenton.dif	\
      hdbdhdii.dif	\
      hdbdw3cc1.dif	\
      hdbdw3jc3.dif	\
      hdblba.dif	\
      hdbdw3arduino.dif	\
      hdbdw3cc2.dif	\
      hdbdw4cc2.dif	\
      hdblrtech.dif


ALL: boot.dsk Makefile dw2dsk.wav dw2dsk.cas

boot.dsk: boot.img bpb.img setup.fi decb.rom $(DIFS) hdbutil.fi os9.fi
	tools/rofs/rofs -x "COCOBOOT BOOT DISK" -f boot.dsk -b boot.img \
bpb.img				\
setup.fi 			\
images/decb.rom 		\
images/hdbchs.dif     		\
images/hdbdw3bc3.dif  		\
images/hdbdw3cc3.dif		\
images/hdbdw4cc3.dif		\
images/hdbtc3.dif		\
images/hdbd4n1.dif		\
images/hdbdw3bck.dif		\
images/hdbdw3jc2.dif		\
images/hdbkenton.dif		\
images/hdbdhdii.dif		\
images/hdbdw3cc1.dif		\
images/hdbdw3jc3.dif		\
images/hdblba.dif		\
images/hdbdw3arduino.dif	\
images/hdbdw3cc2.dif		\
images/hdbdw4cc2.dif		\
images/hdblrtech.dif		\
hdbutil.fi			\
os9.fi				\




$(DIFS): 
	cd images
	make

decb.rom:
	cd images
	make


setup.fi: *.fs
	$(BFC) -cso setup.fi cocoboot.fs setup.fs

hdbutil.fi: *.fs
	$(BFC) -cso hdbutil.fi cocoboot.fs hdbutil.fs

os9.fi: *.fs
	$(BFC) -cso os9.fi cocoboot.fs os9.fs

bpb.img: 
	cat /dev/zero | dd bs=256 count=1 of=bpb.img	

boot.img: base.img forth.slz
	cat base.img forth.slz /dev/zero | dd bs=256 count=18 conv=sync of=boot.img

base.img: cocoboot.def slz.asm run.slz rel.asm
	$(AS) base.img -r rel.asm -s -lCCB.ls

run.slz: run.img
	tools/slz/slz P run.img run.slz

run.img: start.a ../6809/legs16.a 
	$(AS) run.img -D RAW -r start.a -s -lrun.ls

forth.slz: forth.img
	tools/slz/slz P forth.img forth.slz

forth.img: *.fs 
	../bfc/bfc -c -o forth.img cocoboot.fs

dw2dsk.cas: dw2dsk.img
	makewav -r -k -nDW2DSK -d0x2600 -e0x2602 -odw2dsk.cas dw2dsk.img

dw2dsk.wav: dw2dsk.img
	makewav -r -nDW2DSK -d0x2600 -e0x2602 -odw2dsk.wav dw2dsk.img

dw2dsk.img: base.img dw2dsk.slz 
	cat base.img dw2dsk.slz > dw2dsk.img

dw2dsk.slz: dw2dsk.fi
	tools/slz/slz P dw2dsk.fi dw2dsk.slz

dw2dsk.fi: *.fs
	../bfc/bfc -c -o dw2dsk.fi dw2dsk.fs

clean:
	rm -f CCB.BIN forth.img *~ run.img *.slz *.dsk *.img *.ls *.fi
