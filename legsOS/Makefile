all: forth.img  kernel.txt

../C/legs16: ../C/legs16.c
	cd ../C/ ; make

../bfc/bfc: ../bfc/bfc.c
	$(CC) -o ../bfc/bfc ../bfc/bfc.c 

forth.img: ../bfc/bfc ../C/legs16 kernel.fs forth2.fs start.fs dw.fs
	../bfc/bfc -d 4000 -o forth.img start.fs 
	cat forth2.fs dw.fs | ../C/legs16 -si forth.img

kernel.txt: kernel.fs
	grep : kernel.fs > kernel.txt

clean:
	rm -f *~ forth.img


