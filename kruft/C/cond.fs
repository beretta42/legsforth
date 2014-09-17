\ **********************************
\ Conditional Compiling
\ **********************************

: IF 0= if begin 
      word dup s" IF" s= if drop 0 dup pp IF else 
      dup s" THEN" s= swap s" ELSE" s= or then until then ; imm 
: ELSE begin
      word dup s" IF" s= if drop 0 dup pp IF else
      s" THEN" s= then until ; imm
: THEN ; imm

