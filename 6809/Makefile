
#legs16.dsk: LEGS.BIN
#	decb dskini legs16.dsk
#	decb copy -2 -b -r LEGS.BIN legs16.dsk,LEGS.BIN

LEGS.BIN: legs16.a forth.img start.a
	lwasm -9bo LEGS.BIN -I . start.a legs16.a 

forth.img: ../C/forth.fs ../C/forth2.fs 
	../bfc/bfc -o forth.img ../C/forth.fs
	cat ../C/forth2.fs | ../C/legs16 -sc


clean:
	rm -f LEGS.BIN legs16.dsk forth.img *~
