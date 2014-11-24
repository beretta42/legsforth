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

HDBDIR = /home/beretta/C/toolshed-code/hdbdos/
VPATH = $(HDBDIR)

ROMS= hdbchs.rom     	\
      hdbdw3bc3.rom  	\
      hdbdw3cc3.rom	\
      hdbdw4cc3.rom	\
      hdbtc3.rom	\
      hdbd4n1.rom	\
      hdbdw3bck.rom	\
      hdbdw3jc2.rom	\
      hdbkenton.rom	\
      hdbdhdii.rom	\
      hdbdw3cc1.rom	\
      hdbdw3jc3.rom	\
      hdblba.rom	\
      hdbdw3arduino.rom	\
      hdbdw3cc2.rom	\
      hdbdw4cc2.rom	\
      hdblrtech.rom	\
      hdbsdc.rom

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
      hdblrtech.dif	\
      hdbsdc.dif        \


ALL: decb.rom $(OVRS) $(DIFS)

# DECB is built into every HDB image.
# it's duplicated, so well rip the needed pages from one of the 
# full HDB roms
decb.rom: $(ROMS) 
	dd bs=256 count=25 of=decb.rom if=$(HDBDIR)hdbchs.rom

# And our homemade binary patch will create the specific 
# loadable images for each flavor of HDBDOS
%.dif: %.rom
	../tools/idiff/idiff $(HDBDIR)hdbchs.rom $< $@


clean:
	rm -f *.rom *.dif
