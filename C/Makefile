all: legs16 forth.img


../bfc/bfc: ../bfc/bfc.c
	cd ../bfc ; make

forth.img: ../include/forth.fs ../include/forth2.fs ../bfc/bfc
	../bfc/bfc -d 4000 -o forth.img start.fs
	cat ../include/forth2.fs | ./legs16 -sqi forth.img 

legs16: legs16.c
	$(CC) -o legs16 legs16.c

clean:
	rm -f *~ legs16 forth.img

install: legs16 forth.img
	cp legs16 ~/bin
	cp forth.img ~/.legs