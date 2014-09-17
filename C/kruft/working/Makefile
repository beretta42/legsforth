all: test2 kernel.txt

legs16: legs16.c
	$(CC) -o legs16 legs16.c

../bfc/bfc: ../bfc/bfc.c
	$(CC) -o ../bfc/bfc ../bfc/bfc.c 

forth.img: ../bfc/bfc forth.fs kernel.fs
	../bfc/bfc -d 4000 -o forth.img forth.fs

test.dsk: test.fs
	cat test.fs /dev/zero | dd bs=256 count=630 > test.dsk conv=sync

test:	legs16 forth.img forth2.fs test.dsk
	cat forth2.fs | legs16 -sc

#test2:  legs16 forth.img forth2.fs test.dsk 
test2:  legs16 forth.img forth2.fs kernel.fs 
	cat forth2.fs | legs16 -sq

kernel.txt: kernel.fs
	grep : kernel.fs > kernel.txt


clean:
	rm -f *~ legs16 forth.img

save:
	rm -fr working/*
	cp -f * working/

zip:
	cd .. ; tar -czf legs16e.tar.gz legs16e