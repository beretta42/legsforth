(
further defined primitives....
)


p: ff debug                    \ toggle debugging
p: 2b save                     \ ( -- ) save state / write to filesystem
p: 2c tp!                      \ ( a -- ) sets task pointer
p: 2d tp@                      \ ( -- a ) retrieves current task pointer
p: 2e yieldto                  \ ( a -- ) saves task state, restore state a


( Notes on tp! and tp@ - Task Pointer

"tp!" doesn't just set the machine register. It causes the machine to
save it's state to the current value of tp *before* setting tp's new
value, and setting the machine state from that.  Fetching just returns
tp, nothing fancy.  Manipulating TP allows for forking, threading, and
simple memory paging. The exact length and structure of TP is yet to
be determined.

)


done

