all: forth.img  kernel.txt

../C/legs16: ../C/legs16.c
	cd ../C/ ; make

../bfc/bfc: ../bfc/bfc.c
	$(CC) -o ../bfc/bfc ../bfc/bfc.c 

forth.img: ../bfc/bfc ../C/legs16 forth.fs kernel.fs forth2.fs
	../bfc/bfc -d 4000 -o forth.img forth.fs
	cat forth2.fs | ../C/legs16 -sq

kernel.txt: kernel.fs
	grep : kernel.fs > kernel.txt

clean:
	rm -f *~ forth.img


