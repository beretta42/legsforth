                      (                         start.a):00001 ;;; 
                      (                         start.a):00002 ;;;
                      (                         start.a):00003 ;;;
                      (                         start.a):00004 ;;;
                      (                         start.a):00005 
                      (                         start.a):00006         org     0x2600
                      (                         start.a):00007 start
2600 8E260C           (                         start.a):00008         ldx     #test
2603 CC0040           (                         start.a):00009         ldd     #0x40
2606 BD2746           (                         start.a):00010         jsr     rprim           ; register "test"
2609 7E274E           (                         start.a):00011         jmp     goforth         ; go run forth
                      (                         start.a):00012         
                      (                         start.a):00013 test
260C CC2614           (                         start.a):00014         ldd     #mod
260F 3606             (                         start.a):00015         pshu    d
2611 1601D6           (                         start.a):00016         lbra    next
                      (                         start.a):00017 
                      (                         start.a):00018 
                      (                         start.a):00019 mod
2614 87CD0132000DC180 (                         start.a):00020         includebin "boot_dw3_becker"
     CA0012000D426F6F
     F4011A5032731F43
     8E05003440AF4210
     AE8D01081700765F
     8E00001700712524
     AF4BEC88182720ED
     49308815C300FFA7
     046F056F066F0783
     00FF201B17004EAE
     445FEC49326F39E6
     8815AE881617003F
     EC0BED4930881010
     3F2825E81F30EEE4
     ED42ED44AFC4E684
     AE0126035D27CD17
     001D6C42AEC46A04
     270FEC01C30001ED
     01E684C900E78420
     DF300520D75F3934
     1786D2E68D0087ED
     E430E4108E000517
     006BAE42108E0100
     8D34252D26293420
     30E4108E00021700
     54108E00018D1F32
     62250E2612E6E427
     08C1F3260C86F220
     C23265AE425F39C6
     F432651A01394F4A
     1FA9345E33848E00
     001A50F6FF41C502
     27F9F6FF42E7C03A
     31A226EF1F12C600
     860332614CAAE4A7
     E4318435D934071A
     50A680B7FF42313F
     26F73587000000F0
     3F32
                      (                        legs16.a):00001 ;;;
                      (                        legs16.a):00002 ;;;  Legs16 VM for 6809
                      (                        legs16.a):00003 ;;;     - Flat memory model
                      (                        legs16.a):00004 ;;;     - no stack checking
                      (                        legs16.a):00005 ;;;     - no opcode security
                      (                        legs16.a):00006 ;;;     - no stack or memory bounds checking    
                      (                        legs16.a):00007 
     3000             (                        legs16.a):00008 MB       equ   0x3000           ; base address of memory store
                      (                        legs16.a):00009                                 ; MB must be set to a page boundary
     4000             (                        legs16.a):00010 MEMZ     equ   0x4000           ; 16 k worth of virtual memory
                      (                        legs16.a):00011 
                      (                        legs16.a):00012 ;;; Registers:
                      (                        legs16.a):00013 ;;;    D  - scratch pad
                      (                        legs16.a):00014 ;;;    X  - IP
                      (                        legs16.a):00015 ;;;    Y  - scratch pad
                      (                        legs16.a):00016 ;;;    U  - SP 
                      (                        legs16.a):00017 ;;;    S  - RP 
                      (                        legs16.a):00018 ;;;
                      (                        legs16.a):00019 ;;; X,U,S hold an adjusted memory value - the values
                      (                        legs16.a):00020 ;;; are adjusted with the offset to the base of the
                      (                        legs16.a):00021 ;;; VM's memory store.
                      (                        legs16.a):00022 ;;; 
                      (                        legs16.a):00023 
                      (                        legs16.a):00024 
                      (                        legs16.a):00025 
                      (                        legs16.a):00026 
                      (                        legs16.a):00027 ;;; Register a extension Primitive
                      (                        legs16.a):00028 ;;; entry: X is address of routine
                      (                        legs16.a):00029 ;;; exit:  none
                      (                        legs16.a):00030 ;;; mods:  all
                      (                        legs16.a):00031 rprim
2746 58               (                        legs16.a):00032         lslb                    ; convert op code into byte offset
2747 108E28EF         (                        legs16.a):00033         ldy     #table          ; y is base of table
274B AFAB             (                        legs16.a):00034         stx     d,y             ; store address in table
274D 39               (                        legs16.a):00035         rts                     ; return
                      (                        legs16.a):00036 
                      (                        legs16.a):00037 ;;; This routine inits the Forth VM
                      (                        legs16.a):00038 ;;; and starts the inner-interpreter
                      (                        legs16.a):00039 ;;; entry: none
                      (                        legs16.a):00040 ;;; exit:  doesn't
                      (                        legs16.a):00041 goforth
                      (                        legs16.a):00042         ;; we should clr the VM's memory....
274E 1A50             (                        legs16.a):00043         orcc    #0x50           ; shut off interrupts 
2750 10CE7000         (                        legs16.a):00044         lds     #MEMZ+MB        ; setup return pointer
2754 BE3000           (                        legs16.a):00045         ldx     MB              ; setup IP tp point to VM's reset vector
2757 30893000         (                        legs16.a):00046         leax    MB,x            ; add memory store base to x
275B CE6F80           (                        legs16.a):00047         ldu     #MEMZ+MB-0x80   ; setup SP to point to end of memory store
275E 7F011A           (                        legs16.a):00048         clr     0x11a           ; switch BASIC to lower case mode
2761 CC28DF           (                        legs16.a):00049         ldd     #int            ; interrupt handler vector
2764 FD010D           (                        legs16.a):00050         std     0x10d           ; store in basic's irq routine
2767 1CAF             (                        legs16.a):00051         andcc   #0xaf           ; turn interrupts on
2769 7E27EA           (                        legs16.a):00052         jmp     next            ; and jump to inner loop
                      (                        legs16.a):00053         
                      (                        legs16.a):00054 push
276C 3706             (                        legs16.a):00055         pulu    d               
276E 3406             (                        legs16.a):00056         pshs    d
2770 2078             (                        legs16.a):00057         bra     next
                      (                        legs16.a):00058 
                      (                        legs16.a):00059 pull
2772 3506             (                        legs16.a):00060         puls    d               
2774 3606             (                        legs16.a):00061         pshu    d
2776 2072             (                        legs16.a):00062         bra     next
                      (                        legs16.a):00063 
                      (                        legs16.a):00064 drop                            
2778 3342             (                        legs16.a):00065         leau    2,u
277A 206E             (                        legs16.a):00066         bra     next
                      (                        legs16.a):00067 
                      (                        legs16.a):00068 dup                             
277C ECC4             (                        legs16.a):00069         ldd     ,u
277E 3606             (                        legs16.a):00070         pshu    d
2780 2068             (                        legs16.a):00071         bra     next
                      (                        legs16.a):00072 
                      (                        legs16.a):00073 swap
2782 3706             (                        legs16.a):00074         pulu    d
2784 3720             (                        legs16.a):00075         pulu    y
2786 3606             (                        legs16.a):00076         pshu    d
2788 3620             (                        legs16.a):00077         pshu    y
278A 205E             (                        legs16.a):00078         bra     next
                      (                        legs16.a):00079 
                      (                        legs16.a):00080 over
278C EC42             (                        legs16.a):00081         ldd     2,u
278E 3606             (                        legs16.a):00082         pshu    d
2790 2058             (                        legs16.a):00083         bra     next
                      (                        legs16.a):00084 
                      (                        legs16.a):00085 bra
2792 AE84             (                        legs16.a):00086         ldx     ,x
2794 30893000         (                        legs16.a):00087         leax    MB,x
2798 2050             (                        legs16.a):00088         bra     next
                      (                        legs16.a):00089 
                      (                        legs16.a):00090 zbra
279A ECC1             (                        legs16.a):00091         ldd     ,u++
279C 27F4             (                        legs16.a):00092         beq     bra
279E 3002             (                        legs16.a):00093         leax    2,x
27A0 2048             (                        legs16.a):00094         bra     next
                      (                        legs16.a):00095 
                      (                        legs16.a):00096 dofor
27A2 ECE1             (                        legs16.a):00097         ldd     ,s++
27A4 27EC             (                        legs16.a):00098         beq     bra
27A6 C3FFFF           (                        legs16.a):00099         addd    #-1
27A9 3406             (                        legs16.a):00100         pshs    d
27AB 3002             (                        legs16.a):00101         leax    2,x
27AD 203B             (                        legs16.a):00102         bra     next
                      (                        legs16.a):00103 
                      (                        legs16.a):00104 exit
27AF 3510             (                        legs16.a):00105         puls    x
27B1 30893000         (                        legs16.a):00106         leax    MB,x
27B5 2033             (                        legs16.a):00107         bra     next
                      (                        legs16.a):00108 
                      (                        legs16.a):00109 mint
27B7 CC8000           (                        legs16.a):00110         ldd     #0x8000
27BA 3606             (                        legs16.a):00111         pshu    d
27BC 202C             (                        legs16.a):00112         bra     next
                      (                        legs16.a):00113 
                      (                        legs16.a):00114 and
27BE 3706             (                        legs16.a):00115         pulu    d
27C0 A4C4             (                        legs16.a):00116         anda    ,u
27C2 E441             (                        legs16.a):00117         andb    1,u
27C4 EDC4             (                        legs16.a):00118         std     ,u
27C6 2022             (                        legs16.a):00119         bra     next
                      (                        legs16.a):00120 
                      (                        legs16.a):00121 or
27C8 3706             (                        legs16.a):00122         pulu    d
27CA AAC4             (                        legs16.a):00123         ora     ,u
27CC EA41             (                        legs16.a):00124         orb     1,u
27CE EDC4             (                        legs16.a):00125         std     ,u
27D0 2018             (                        legs16.a):00126         bra     next
                      (                        legs16.a):00127 
                      (                        legs16.a):00128 xor
27D2 3706             (                        legs16.a):00129         pulu    d
27D4 A8C4             (                        legs16.a):00130         eora    ,u
27D6 E841             (                        legs16.a):00131         eorb    1,u
27D8 EDC4             (                        legs16.a):00132         std     ,u
27DA 200E             (                        legs16.a):00133         bra     next
                      (                        legs16.a):00134 
                      (                        legs16.a):00135 com
27DC 3706             (                        legs16.a):00136         pulu    d
27DE 43               (                        legs16.a):00137         coma
27DF 53               (                        legs16.a):00138         comb
27E0 3606             (                        legs16.a):00139         pshu    d
27E2 2006             (                        legs16.a):00140         bra     next
                      (                        legs16.a):00141 
                      (                        legs16.a):00142 plus
27E4 3706             (                        legs16.a):00143         pulu    d
27E6 E3C4             (                        legs16.a):00144         addd    ,u
27E8 EDC4             (                        legs16.a):00145         std     ,u
                      (                        legs16.a):00146 ;       bra     next            ; fall-through to next
                      (                        legs16.a):00147 
                      (                        legs16.a):00148 next
27EA 7D28EE           (                        legs16.a):00149         tst     iflag           ; is there an interrupt pending?
27ED 2708             (                        legs16.a):00150         beq     next0           ; nope 
27EF 7F28EE           (                        legs16.a):00151         clr     iflag           ; reset flag
27F2 FC3002           (                        legs16.a):00152         ldd     MB+2            ; load d with interrupt vector
27F5 2002             (                        legs16.a):00153         bra     next1           ; go processes vector
27F7 EC81             (                        legs16.a):00154 next0   ldd     ,x++            ; load next op code and inc IP
27F9 4D               (                        legs16.a):00155 next1   tsta                    ; is a primitive?
27FA 270C             (                        legs16.a):00156         beq     next2           ; yes then go processes
27FC 3089D000         (                        legs16.a):00157         leax    -MB,x           ; no then change IP to virtual address
2800 3410             (                        legs16.a):00158         pshs    x               ; push virtual address onto RP
2802 8B30             (                        legs16.a):00159         adda    #MB/256         ; convert op code to real address
2804 1F01             (                        legs16.a):00160         tfr     d,x             ; store in IP
2806 20E2             (                        legs16.a):00161         bra     next            ; rinse and repeat
                      (                        legs16.a):00162 next2
2808 58               (                        legs16.a):00163         lslb                    ; convert op code into byte offset
2809 1F02             (                        legs16.a):00164         tfr     d,y             ; fetch op code address from table
280B 6EB928EF         (                        legs16.a):00165         jmp     [table,y]       ; go do primitive
                      (                        legs16.a):00166         
                      (                        legs16.a):00167 shl
280F 3706             (                        legs16.a):00168         pulu    d
2811 58               (                        legs16.a):00169         lslb
2812 49               (                        legs16.a):00170         rola
2813 3606             (                        legs16.a):00171         pshu    d
2815 20D3             (                        legs16.a):00172         bra     next
                      (                        legs16.a):00173 
                      (                        legs16.a):00174 shr
2817 3706             (                        legs16.a):00175         pulu    d
2819 44               (                        legs16.a):00176         lsra
281A 56               (                        legs16.a):00177         rorb
281B 3606             (                        legs16.a):00178         pshu    d
281D 20CB             (                        legs16.a):00179         bra     next
                      (                        legs16.a):00180 
                      (                        legs16.a):00181 oneplus
281F 3706             (                        legs16.a):00182         pulu    d
2821 C30001           (                        legs16.a):00183         addd    #1
2824 3606             (                        legs16.a):00184         pshu    d
2826 20C2             (                        legs16.a):00185         bra     next
                      (                        legs16.a):00186 
                      (                        legs16.a):00187 oneminus
2828 3706             (                        legs16.a):00188         pulu    d
282A C3FFFF           (                        legs16.a):00189         addd    #-1
282D 3606             (                        legs16.a):00190         pshu    d
282F 20B9             (                        legs16.a):00191         bra     next
                      (                        legs16.a):00192         
                      (                        legs16.a):00193 spat
2831 31C9D000         (                        legs16.a):00194         leay    -MB,u
2835 3620             (                        legs16.a):00195         pshu    y
2837 20B1             (                        legs16.a):00196         bra     next
                      (                        legs16.a):00197 
                      (                        legs16.a):00198 spbang
2839 3706             (                        legs16.a):00199         pulu    d
283B 8B30             (                        legs16.a):00200         adda    #MB/256
283D 1F03             (                        legs16.a):00201         tfr     d,u
283F 20A9             (                        legs16.a):00202         bra     next
                      (                        legs16.a):00203 
                      (                        legs16.a):00204 rpat
2841 31E9D000         (                        legs16.a):00205         leay    -MB,s
2845 3620             (                        legs16.a):00206         pshu    y
2847 20A1             (                        legs16.a):00207         bra     next
                      (                        legs16.a):00208 
                      (                        legs16.a):00209 rpbang
2849 3706             (                        legs16.a):00210         pulu    d
284B 8B30             (                        legs16.a):00211         adda    #MB/256
284D 1F04             (                        legs16.a):00212         tfr     d,s
284F 2099             (                        legs16.a):00213         bra     next
                      (                        legs16.a):00214 
                      (                        legs16.a):00215 exec
2851 3706             (                        legs16.a):00216         pulu    d
2853 20A4             (                        legs16.a):00217         bra     next1
                      (                        legs16.a):00218 
                      (                        legs16.a):00219 at
2855 3720             (                        legs16.a):00220         pulu    y
2857 ECA93000         (                        legs16.a):00221         ldd     MB,y
285B 3606             (                        legs16.a):00222         pshu    d
285D 208B             (                        legs16.a):00223         bra     next
                      (                        legs16.a):00224 
                      (                        legs16.a):00225 bang
285F 3720             (                        legs16.a):00226         pulu    y
2861 3706             (                        legs16.a):00227         pulu    d
2863 EDA93000         (                        legs16.a):00228         std     MB,y
2867 2081             (                        legs16.a):00229         bra     next
                      (                        legs16.a):00230 
                      (                        legs16.a):00231 cat
2869 3720             (                        legs16.a):00232         pulu    y
286B 4F               (                        legs16.a):00233         clra
286C E6A93000         (                        legs16.a):00234         ldb     MB,y
2870 3606             (                        legs16.a):00235         pshu    d
2872 7E27EA           (                        legs16.a):00236         jmp     next
                      (                        legs16.a):00237 
                      (                        legs16.a):00238 cbang
2875 3720             (                        legs16.a):00239         pulu    y
2877 3706             (                        legs16.a):00240         pulu    d
2879 E7A93000         (                        legs16.a):00241         stb     MB,y
287D 7E27EA           (                        legs16.a):00242         jmp     next
                      (                        legs16.a):00243 
                      (                        legs16.a):00244 cell
2880 CC0002           (                        legs16.a):00245         ldd     #2
2883 3606             (                        legs16.a):00246         pshu    d
2885 7E27EA           (                        legs16.a):00247         jmp     next
                      (                        legs16.a):00248 
                      (                        legs16.a):00249 char
2888 CC0001           (                        legs16.a):00250         ldd     #1
288B 3606             (                        legs16.a):00251         pshu    d
288D 7E27EA           (                        legs16.a):00252         jmp     next
                      (                        legs16.a):00253 
                      (                        legs16.a):00254 lit
2890 EC81             (                        legs16.a):00255         ldd     ,x++
2892 3606             (                        legs16.a):00256         pshu    d
2894 7E27EA           (                        legs16.a):00257         jmp     next
                      (                        legs16.a):00258 
                      (                        legs16.a):00259 key
2897 3401             (                        legs16.a):00260         pshs    cc
2899 BDA176           (                        legs16.a):00261         jsr     0xa176  ; call BASIC's console in
289C 3501             (                        legs16.a):00262         puls    cc
289E 1F89             (                        legs16.a):00263         tfr     a,b
28A0 4F               (                        legs16.a):00264         clra
28A1 3606             (                        legs16.a):00265         pshu    d
28A3 7E27EA           (                        legs16.a):00266         jmp     next
                      (                        legs16.a):00267 
                      (                        legs16.a):00268 emit
28A6 3401             (                        legs16.a):00269         pshs    cc
28A8 3706             (                        legs16.a):00270         pulu    d
28AA 1F98             (                        legs16.a):00271         tfr     b,a
28AC BDA282           (                        legs16.a):00272         jsr     0xa282 ; call BASIC's console out
28AF 3501             (                        legs16.a):00273         puls    cc
28B1 7E27EA           (                        legs16.a):00274         jmp     next
                      (                        legs16.a):00275 
                      (                        legs16.a):00276 bye
28B4 6E9FFFFE         (                        legs16.a):00277         jmp     [0xfffe]
                      (                        legs16.a):00278 
                      (                        legs16.a):00279 memz
28B8 CC4000           (                        legs16.a):00280         ldd     #MEMZ
28BB 3606             (                        legs16.a):00281         pshu    d
28BD 7E27EA           (                        legs16.a):00282         jmp     next
                      (                        legs16.a):00283 
                      (                        legs16.a):00284 pat
28C0 4F               (                        legs16.a):00285         clra
28C1 E6D4             (                        legs16.a):00286         ldb     [,u]
28C3 EDC4             (                        legs16.a):00287         std     ,u
28C5 7E27EA           (                        legs16.a):00288         jmp     next
                      (                        legs16.a):00289 
                      (                        legs16.a):00290 pbang
28C8 3720             (                        legs16.a):00291         pulu    y
28CA 3706             (                        legs16.a):00292         pulu    d
28CC E7A4             (                        legs16.a):00293         stb     ,y
28CE 7E27EA           (                        legs16.a):00294         jmp     next
                      (                        legs16.a):00295 
                      (                        legs16.a):00296 
                      (                        legs16.a):00297 ion
28D1 86FF             (                        legs16.a):00298         lda     #0xff
28D3 B728ED           (                        legs16.a):00299         sta     imask           ; handle interrupts
28D6 7E27EA           (                        legs16.a):00300         jmp     next
                      (                        legs16.a):00301 
                      (                        legs16.a):00302         
                      (                        legs16.a):00303 ioff
28D9 7F28ED           (                        legs16.a):00304         clr     imask           ; don't handle interrupts
28DC 7E27EA           (                        legs16.a):00305         jmp     next
                      (                        legs16.a):00306 
                      (                        legs16.a):00307         ;; real interrupt handler
                      (                        legs16.a):00308 int
28DF B6FF02           (                        legs16.a):00309         lda     0xff02          ; reset pia
28E2 7D28ED           (                        legs16.a):00310         tst     imask           ; are we handling interrupt?
28E5 2705             (                        legs16.a):00311         beq     int1            ; no then return
28E7 86FF             (                        legs16.a):00312         lda     #0xff           ; 
28E9 B728EE           (                        legs16.a):00313         sta     iflag           ; set flag for syncronizing with main loop
28EC 3B               (                        legs16.a):00314 int1    rti
                      (                        legs16.a):00315 
28ED 00               (                        legs16.a):00316 imask   .db     0               ; interrupt handler mask
28EE 00               (                        legs16.a):00317 iflag   .db     0               ; marked true by interrupt handler
                      (                        legs16.a):00318         
                      (                        legs16.a):00319 
                      (                        legs16.a):00320 
                      (                        legs16.a):00321 
                      (                        legs16.a):00322 ;;; This table holds the addresses of
                      (                        legs16.a):00323 ;;; the Legs Forth VM
                      (                        legs16.a):00324 table
28EF 28B4             (                        legs16.a):00325         .dw     bye             ; This is the exception
28F1 276C             (                        legs16.a):00326         .dw     push
28F3 2772             (                        legs16.a):00327         .dw     pull
28F5 2778             (                        legs16.a):00328         .dw     drop
28F7 277C             (                        legs16.a):00329         .dw     dup
28F9 2782             (                        legs16.a):00330         .dw     swap            ; 5
28FB 278C             (                        legs16.a):00331         .dw     over
28FD 2792             (                        legs16.a):00332         .dw     bra
28FF 279A             (                        legs16.a):00333         .dw     zbra
2901 27A2             (                        legs16.a):00334         .dw     dofor
2903 27AF             (                        legs16.a):00335         .dw     exit            ; 10
2905 27B7             (                        legs16.a):00336         .dw     mint
2907 27BE             (                        legs16.a):00337         .dw     and
2909 27C8             (                        legs16.a):00338         .dw     or
290B 27D2             (                        legs16.a):00339         .dw     xor
290D 27DC             (                        legs16.a):00340         .dw     com             ; 15
290F 27E4             (                        legs16.a):00341         .dw     plus
2911 280F             (                        legs16.a):00342         .dw     shl
2913 2817             (                        legs16.a):00343         .dw     shr
2915 281F             (                        legs16.a):00344         .dw     oneplus
2917 2828             (                        legs16.a):00345         .dw     oneminus        ; 20
2919 2831             (                        legs16.a):00346         .dw     spat
291B 2839             (                        legs16.a):00347         .dw     spbang
291D 2841             (                        legs16.a):00348         .dw     rpat
291F 2849             (                        legs16.a):00349         .dw     rpbang
2921 2851             (                        legs16.a):00350         .dw     exec            ; 25
2923 2855             (                        legs16.a):00351         .dw     at
2925 285F             (                        legs16.a):00352         .dw     bang
2927 2869             (                        legs16.a):00353         .dw     cat
2929 2875             (                        legs16.a):00354         .dw     cbang
292B 2880             (                        legs16.a):00355         .dw     cell            ; 30
292D 2888             (                        legs16.a):00356         .dw     char
292F 2890             (                        legs16.a):00357         .dw     lit
2931 2897             (                        legs16.a):00358         .dw     key
2933 28A6             (                        legs16.a):00359         .dw     emit
2935 28B4             (                        legs16.a):00360         .dw     bye             ; 35
2937 28B8             (                        legs16.a):00361         .dw     memz
2939 28C0             (                        legs16.a):00362         .dw     pat             ; 37
293B 28C8             (                        legs16.a):00363         .dw     pbang           ; 38
293D 28D1             (                        legs16.a):00364         .dw     ion             ; 39
293F 28D9             (                        legs16.a):00365         .dw     ioff            ; 40
2941                  (                        legs16.a):00366         rmb     table+128-.
                      (                        legs16.a):00367 
                      (                        legs16.a):00368 tend
                      (                        legs16.a):00369 
                      (                        legs16.a):00370         ;; and inbed our Legs16 VM byte-code image
                      (                        legs16.a):00371         org     MB              
3000 0E88000010EE10D7 (                        legs16.a):00372         includebin forth.img
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0000000000000000
     0002000A001E0010
     000A001F0010000A
     0006001B0104000A
     0006001D010A000A
     000401040005001A
     000A0004010A0005
     001C000A00200004
     000A0134000A013A
     000A013E001A000A
     014200050110013E
     001B000A01420005
     0118013E001B000A
     0008016C00200000
     000701700020FFFF
     000A000B000C0008
     01820020FFFF0007
     018600200000000A
     000F0013000A0188
     0010000A00200021
     018E0172000A0001
     000500020005000A
     000300030003000A
     00060006000A0005
     0006000A00050003
     000A019E019E000A
     01B0000E01720008
     01D801BC0172000A
     018E0172000A0100
     FFFF000A00210004
     0022000A01B6001A
     00100005001B000A
     0142002001000010
     000A01F800200000
     011001E400040194
     0008021A00030007
     020A011800200001
     01F801EC01E40004
     01940008021A01AA
     01F8000A0FCB000A
     0002000200030001
     000A0120019E0120
     019E0006018E0008
     025A01A800200000
     000A00010009027C
     012A019E012A019E
     018E0008027801AA
     002000000238000A
     0007025C01AA0020
     FFFF000A00200006
     000A0284001A000A
     010401040104000A
     001A0004000802B4
     01B0029002420008
     02AE01BC000A001A
     0007029A01BC000A
     0004028402980004
     0160000802C8000A
     01BC00040294001A
     00050292001A0008
     02E2002000010007
     02E60020FFFF000A
     0020FFFF01DE001B
     000A0020000001DE
     001B000A00040008
     03040004000A0120
     0004014800010009
     031A012A01540007
     030E0003000A0142
     028A01480284001B
     0142002000000004
     0148014802340306
     01420005001B000A
     031E000A034002F2
     000A0020000A0148
     02E8000A0006018E
     0001018E000201C8
     000A00040020002F
     0020003A03540008
     037A00200030018E
     000A000400200060
     0020006703540008
     039200200057018E
     000A00030020FFFF
     000A01200006001C
     0020002D018E0160
     000803BC00140005
     010A00050020FFFF
     000703C000200000
     01C2002000000005
     0001000903F80011
     0011001100110005
     012A036200040172
     000803F001AA01BC
     002000000238000A
     019E0010000703CA
     01BC000500080402
     01880020FFFF000A
     039A000A0020003F
     00220020000D0022
     00030007041A000A
     0EED000A01E40004
     0020000D018E0160
     00050020000A018E
     0160000D00080424
     000A02340004001A
     0160000804520003
     000A02B802FC0008
     04740172016001DE
     001A000D0008046E
     0019000704700148
     0007049A00040408
     0008049401BC01DE
     001A016000080490
     0020002001480148
     0007049A00030420
     000A000704420024
     001804420007049E
     04B4049E00000004
     7175697404C50442
     00000009696E7465
     727072657404CE04
     24000100015C04D9
     042000000003776E
     6604E5040C000000
     04776E662704F104
     08000000043E6E75
     6D04FE039A000000
     053E6E756D27050A
     0362000000046174
     6F75051803540000
     000677697468696E
     0521034A00010001
     3B052A0344000000
     013A053803400000
     0006686561646572
     0547031E00000007
     6865616465722705
     5103060000000273
     2C055D02FC000000
     043F647570056602
     F2000000015D056F
     02E8000100015B05
     7B02B80000000466
     696E640588029800
     0000056466696E64
     0593029400000003
     3E787405A0029200
     0000053E666C6167
     05AD029000000005
     3E6E616D6505BB02
     8A000000066C6174
     65737405C5028400
     000002646805CF02
     4200000002733D05
     DD02380000000675
     6E6C6F6F7005E902
     3400000004776F72
     6405F60202000000
     05776F7264270601
     01F8000000037469
     62060B01EC000000
     022B21061701E400
     000004656B657906
     2401DE0000000573
     74617465062E01C8
     00000002753C063A
     01C2000000042D72
     6F74064501BC0000
     00036E6970065101
     B600000004747563
     6B065D01B0000000
     0432647570066A01
     AA00000005326472
     6F70067701A80000
     00053364726F7006
     82019E0000000372
     6F74068D01940000
     0003626C3F069601
     8E000000012D06A1
     0188000000036E65
     6706AB0172000000
     02303C06B5016000
     000002303D06BF01
     5400000002632C06
     C80148000000012C
     06D4014200000004
     6865726506DE013E
     00000002637006E9
     013A000000036370
     2706F40134000000
     0363703006FF012A
     0000000363402B07
     0901200000000240
     2B07140118000000
     0363212B071E0110
     00000002212B072B
     010A000000056368
     61722B0738010400
     00000563656C6C2B
     0745010000000005
     646F766172075200
     FF00000005646562
     7567075E002A0000
     00046B65793F076B
     0029000000056977
     6169740777002800
     000004696F666607
     8200270000000369
     6F6E078C00260000
     0002702107960025
     00000002704007A2
     0024000000046D65
     6D7A07AD00230000
     000362796507B900
     2200000004656D69
     7407C40021000000
     036B657907CF0020
     000000036C697407
     DB001F0000000463
     68617207E7001E00
     00000463656C6C07
     F1001D0000000263
     2107FB001C000000
     0263400804001B00
     00000121080D001A
     0000000140081900
     1900000004657865
     6308240018000000
     03727021082F0017
     0000000372704008
     3A00160000000373
     7021084500150000
     0003737040084F00
     1400000002312D08
     5900130000000231
     2B08640012000000
     03736872086F0011
     0000000373686C08
     780010000000012B
     0883000F00000003
     636F6D088E000E00
     000003786F720898
     000D000000026F72
     08A3000C00000003
     616E6408AF000B00
     0000046D696E7408
     BB000A0000000465
     78697408C8000900
     000005646F666F72
     08D4000800000004
     3062726108DF0007
     0000000362726108
     EB0006000000046F
     76657208F7000500
     0000047377617009
     0200040000000364
     7570090E00030000
     000464726F70091A
     0002000000047075
     6C6C092600010000
     0004707573680000
     0000000000046578
     636504A8093E0000
     0004747275650020
     FFFF000A09320951
     0000000566616C73
     6500200000000A09
     440963000000046E
     6F6F70000A095709
     6F00000002637200
     20000D0022000A09
     6509840000000573
     7061636500200020
     0022000A09770996
     0000000262610142
     002000000148000A
     098C09AB00000003
     696D6D0020000102
     8A0292001B000A09
     A009C30001000474
     68656E0142000500
     1B000A09B709D400
     00000127023402B8
     0160000809E20420
     049E000A09CB09EE
     00010002707009D4
     0148000A09E409FD
     000100015E002000
     20014809EE002001
     480148000A09F40A
     1700010002696600
     2000080148099600
     0A0A0D0A2D000100
     04656C7365002000
     0701480996000509
     C3000A0A210A4800
     010005626567696E
     0142000A0A3B0A59
     0001000561676169
     6E00200007014801
     48000A0A4C0A7000
     010005756E74696C
     0020000801480148
     000A0A630A870001
     00057768696C650A
     17000A0A7A0A9900
     0100067265706561
     7400050A5909C300
     0A0A8B0AAC000100
     03666F7200200001
     0148014200200009
     01480996000A0AA1
     0ACA000100046E65
     78740A99000A0ABE
     0AD8000000027240
     00170104001A000A
     0ACE0AED00000005
     63656C6C2D001E01
     8E000A0AE00AFD00
     0000022D2100050A
     ED01B6001B000A0A
     F30B150000000631
     3273776170019E00
     05000A0B070B2900
     0000063032737761
     7001C20005000A0B
     1B0B3C0001000563
     6861723F00200020
     0148023401200003
     001C0148000A0B2F
     0B5A000000047074
     696201F800200040
     0010000A0B4E0B71
     0000000570617273
     650B5A0020000001
     10000601E401B601
     8E00080B93011800
     2000010B5A01EC00
     070B7901A80B5A00
     0A0B640BA2000100
     0128002000290B71
     0003000A0B990BB8
     00000004736C6974
     0002000401200010
     0001000A0BAC0BCD
     0000000122002000
     220B71000A0BC40B
     DF00000002702201
     420BCD0306000A0B
     D50BF10001000273
     2200200BB801480B
     CD0306000A0BE70C
     07000000026E220B
     CD0120000100090C
     19012A015400070C
     0D0003000A0BFD0C
     2900000004617374
     7200040001012000
     10001D0020000100
     0201EC000A0C1D0C
     4A00010005646566
     6572002009630148
     000A0C3D0C5E0000
     00045B69735D0110
     0020000A0005001B
     000A0C520C740000
     0002697309D40005
     0C5E000A0C6A0C8D
     00000009646F2666
     6F72676574000400
     19013E001B000A0C
     7C0CA30000000478
     6C69740002012000
     0600100001000A0C
     970CB9000000025B
     5B014202F2000A0C
     AF0CC9000100025D
     5D034A000A0CBF0C
     D7000100027B7B00
     200CA30148099600
     0A0CCD0CEB000100
     027D7D0020000A01
     480142001E018E00
     06018E0005001B00
     0A0CE10D0D000000
     0474797065012000
     0100090D1D012A00
     2200070D11000300
     0A0D010D2B000100
     022E220BF100200D
     0D0148000A0D210D
     3F000100022E2800
     2000290B710D0D00
     0A0D350D55000000
     0475746F63000400
     20000A018E017200
     080D6B0020003000
     070D6F0020005700
     10000A0D490D7D00
     000002752E000400
     20000F000C0D5500
     0500120012001200
     1202FC00080D990D
     7D0022000A0D730D
     A6000000012E0D7D
     0984096F000A0D9D
     0DBB000000056465
     707468001500207F
     800005018E001200
     0A0DAE0DD6000000
     0562656D69740004
     0012001200120012
     0D5500220020000F
     000C0D550022000A
     0DC90DFD00000005
     77656D6974001500
     04001C0DD6010A00
     1C0DD60003000A0D
     F00E1B0000000464
     756D70096F00040D
     FD0BB800013A0D0D
     096F002000080001
     00090E8200200008
     000100090E4C0004
     001C0DD60984010A
     00070E3A00200008
     018E002000080001
     00090E7C0004001C
     0004019400080E74
     00030020002E0022
     00070E760022010A
     00070E58096F0007
     0E30096F0003000A
     0BB800134C656773
     20466F727468202D
     203136206269740D
     0D096F0BB800026F
     6B0D0D096F049E00
     0A096F0BB800142A
     2A2A20576F726420
     4E6F7420466F756E
     643A200D0D0D0D09
     6F0023000A0E0F0E
     E500000008696E74
     6572616374002004
     200CA30024096F0B
     B800142A2A2A2057
     6F7264204E6F7420
     466F756E643A200D
     0D0D0D096F049E00
     0A0C5E000A0ED50F
     1E000000013D000E
     0160000A0F150F2E
     000000023C3E0F1E
     000F000A0F240F3E
     00000002753C01B0
     000E017200080F4E
     01BC0172000A018E
     0172000A0F340F5E
     00000002753E0005
     0F3E000A0F540F6D
     000000013C01B000
     0E017200080F7D00
     030172000A018E01
     72000A0F640F8C00
     0000013E00050F6D
     000A0F830F9C0000
     00023E3D0F6D000F
     000A0F920FAC0000
     00023C3D01B00F1E
     01C20F6D000D000A
     0FA20FC300000003
     626C3F002000210F
     3E000A01F8002000
     00011001E400040F
     C300080FE3000300
     070FD30004017200
     080FF101AA01F800
     0A0004002000080F
     1E0142001A000C00
     0810130003001400
     20FFFF01F801EC00
     07101D0118002000
     0101F801EC01E400
     04000400040FC300
     05002000080F2E00
     0C00050172000D00
     080FF101AA01F800
     0A0FB81051000000
     08646F6372656174
     650AD80104000200
     1A0019000A104110
     6B00000006637265
     6174650340002010
     5101480020000A01
     48000A105D108800
     000005646F65733E
     0002028A0294001A
     0104001B000A107B
     10A6000000087661
     726961626C65106B
     0148000A109610BC
     00000008636F6E73
     74616E74106B0148
     1088001A000A10AC
     10D3000000056365
     6C6C730011000A10
     C610E40000000561
     6C6C6F7401420010
     013E001B000A
                      (                        legs16.a):00373         
                      (                        legs16.a):00374         end     start

Symbol Table:
[ G] and                              27BE
[ G] at                               2855
[ G] bang                             285F
[ G] bra                              2792
[ G] bye                              28B4
[ G] cat                              2869
[ G] cbang                            2875
[ G] cell                             2880
[ G] char                             2888
[ G] com                              27DC
[ G] dofor                            27A2
[ G] drop                             2778
[ G] dup                              277C
[ G] emit                             28A6
[ G] exec                             2851
[ G] exit                             27AF
[ G] goforth                          274E
[ G] iflag                            28EE
[ G] imask                            28ED
[ G] int                              28DF
[ G] int1                             28EC
[ G] ioff                             28D9
[ G] ion                              28D1
[ G] key                              2897
[ G] lit                              2890
[ G] MB                               3000
[ G] MEMZ                             4000
[ G] memz                             28B8
[ G] mint                             27B7
[ G] mod                              2614
[ G] next                             27EA
[ G] next0                            27F7
[ G] next1                            27F9
[ G] next2                            2808
[ G] oneminus                         2828
[ G] oneplus                          281F
[ G] or                               27C8
[ G] over                             278C
[ G] pat                              28C0
[ G] pbang                            28C8
[ G] plus                             27E4
[ G] pull                             2772
[ G] push                             276C
[ G] rpat                             2841
[ G] rpbang                           2849
[ G] rprim                            2746
[ G] shl                              280F
[ G] shr                              2817
[ G] spat                             2831
[ G] spbang                           2839
[ G] start                            2600
[ G] swap                             2782
[ G] table                            28EF
[ G] tend                             296F
[ G] test                             260C
[ G] xor                              27D2
[ G] zbra                             279A
