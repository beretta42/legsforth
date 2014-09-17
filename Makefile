#
# A Makefile
#

all: 
	cd C ; make
	cd bfc ; make
	cd legsOS ; make
#	cd 6809 ; make
#	cd cocoboot ; make

clean:
	cd C ; make clean
	cd bfc ; make clean 
	cd legsOS ; make clean
#	cd 6809 ; make
#	cd cocoboot ; make