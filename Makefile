#
# A Makefile
#

all: 
	cd bfc ; make
	cd C ; make
	cd legsOS ; make
#	cd 6809 ; make
#	cd cocoboot ; make

clean:
	cd bfc ; make clean 	
	cd C ; make clean
	cd legsOS ; make clean
#	cd 6809 ; make
#	cd cocoboot ; make

install:
	cd C ; make install