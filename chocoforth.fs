\ ( "<spaces>name" -- xt ) エラーチェックが必要
: '
    WORD FIND >CFA
;

\ Compilation: ( "<spaces>name" -- ) Run-time: ( -- xt )
: ['] IMMEDIATE
    LIT LIT ,                           \ LIT をコンパイル
    ' ,                                 \ ワードをパースしてコンパイル
;

\ 6.2.2530 [COMPILE]
\ Compilation: ( "<spaces>name" -- )
: [COMPILE] IMMEDIATE
    ' ,
;

: LITERAL IMMEDIATE
    ['] LIT ,                           \ LIT をコンパイル
    ,                                   \ リテラルをコンパイル
;

: TRUE  1 ;
: FALSE 0 ;
: NOT   0= ;

: IF IMMEDIATE
    ['] 0BRANCH ,         \ 偽の場合のジャンプ
    HERE @                \ 偽の場合のジャンプ開始アドレスをスタックに
    0 ,                   \ あとでこの 0 をオフセットで上書く
;

: THEN IMMEDIATE
    DUP                               \ IF での HERE @ を DUP
    HERE @ SWAP -                     \ オフセットを計算
    SWAP !                            \ IF での HERE @ に オフセットを
;

: ELSE IMMEDIATE
    ['] BRANCH ,                \ 真の場合のジャンプ
    HERE @                      \ 真の場合のジャンプ開始位置
    0 ,                         \ 真の場合のオフセット
    SWAP                        \ IF と ELSE の HERE @ を入れ替え
    DUP                         \ IF の HERE @ を DUP
    HERE @ SWAP -               \ 偽だった場合のオフセット
    SWAP !                      \ DUP した IF の HERE @ にオフセットを
;
: test-if-true 1 IF 49 EMIT ELSE 48 EMIT THEN ;
test-if-true
: test-if-false 0 IF 49 EMIT ELSE 48 EMIT THEN ;
test-if-false


: RECURSE IMMEDIATE
    LATEST @                            \ コンパイル中のワードの
    >CFA                                \ codewordを
    ,                                   \ コンパイルする。
;

\ 6.1.0760 BEGIN Compilation: ( C: -- dest ) Run-time: ( -- )
: BEGIN IMMEDIATE
    HERE @                              \ ループの先頭アドレスをスタックに
;

: UNTIL IMMEDIATE
    ['] 0BRANCH ,
    HERE @ -
    ,
;

: AGAIN IMMEDIATE
    ['] BRANCH ,
    HERE @ -
    ,
;

: WHILE IMMEDIATE
    ['] 0BRANCH ,
    HERE @
    0 ,
;

: REPEAT IMMEDIATE
    ['] BRANCH ,
    SWAP
    HERE @ - ,
    DUP
    HERE @ SWAP -
    SWAP !
;

: UNLESS IMMEDIATE
    ['] NOT ,
    [COMPILE] IF
;
: test-unless
    0 UNLESS
    49 EMIT
ELSE
    50 EMIT
THEN
    1 UNLESS
    49 EMIT
ELSE
    50 EMIT
THEN
;
test-unless

: [CHAR] IMMEDIATE
    ['] LIT ,
    CHAR ,
;

\ 6.1.0080 ( ( "ccc<paren>" -- )
: ( IMMEDIATE
    1                                   \ ネストの深さ
    BEGIN
        KEY
        DUP [CHAR] ( = IF               \ ネストしたコメントの開始
            DROP
            1+                          \ ネスト + 1
        ELSE
            [CHAR] ) = IF               \ コメントの終り
                1-                      \ ネスト - 1
            THEN
        THEN
    DUP 0= UNTIL                        \ ネストが 0 ならおしまい
    DROP
;

: '\n' 10 ;
: BL   32 ;
: CR
    '\n' EMIT ;
: SPACE
    BL EMIT ;

: / /MOD SWAP DROP ;
: MOD /MOD DROP ;
\ 6.1.1910 NEGATE
: NEGATE ( n1 -- n2 )
    0 SWAP - ;


\ 6.1.0890 CELLS ( n1 -- n2 )
: CELLS 8 * ;


\ あとでトレーリングスペース付きで再定義する。
: U. ( u -- )
    BASE @ /MOD                         \ ( 13 -- 3 10 )
    ?DUP IF                             \ 商が 0 でなければ
        RECURSE                         \ 商をプリントするために再帰
    THEN
    ( 余りをプリント )
    DUP 10 < IF
        [CHAR] 0                        \ 余り + '0' を
    ELSE
        10 - [CHAR] A                   \ 余り - 10 + 'A' を
    THEN
    +
    EMIT                                \ プリントする。
;

\ 6.1.2320 U. トレーリングスペース付きで再定義する。
: U. ( u -- )
    U. SPACE
;

\ 6.1.0180 .
: . ( n -- )
    DUP 0< IF
        [CHAR] - EMIT
        NEGATE
    THEN
    U.
;
CR 123 .
CR -123 .

: .S
    DSP@                                \ スタックポインタを取得
    S0 @
    SWAP
    BEGIN
        2DUP >                          \ スタックの底につくまで
    WHILE
            DUP @ U.                    \ 中身をプリント
            1 CELLS +                   \ 8 バイト移動
    REPEAT
    2DROP
;
: test-.S
    CR 10 9 8 7 6 5 4 3 2 1 .S
    DROP DROP DROP DROP DROP
    CR .S
    DROP DROP DROP DROP DROP
;
test-.S

\ 6.1.0706 ALIGNED
\ CORE
: ALIGNED ( addr -- a-addr )
    1 CELLS 1- +
    1 CELLS 1- INVERT AND                    \ (addr + 7) & ~7)
;

\ 6.1.0705 ALIGN
\ CORE
: ALIGN	( -- )
    HERE @ ALIGNED HERE !
;

\ 6.1.0860 C,
\ c-comma CORE
: C, ( char -- )
    HERE @ C!
    1 HERE +!
;

\ 6.1.2165 S"
\ s-quote CORE
\ Compilation: ( "ccc<quote>" -- )
\ Run-time:    ( -- c-addr u )
: S" IMMEDIATE
    STATE @ IF                          \ コンパイル中?
        ['] LITSTRING ,
        HERE @                          \ 文字列アドレスの開始位置
        0 ,                             \ ダミーの文字列長
        BEGIN
            KEY DUP [CHAR] " <>
        WHILE
                C,
        REPEAT
        DROP                            \ " を捨てる。
        DUP                             \ 文字列長のアドレスを DUP
        HERE @ SWAP -                   \ 文字列長を計算
        1 CELLS -                       \ 文字列長の分を引く
        SWAP !                          \ 文字列長をセット
        ALIGN
    ELSE
        \ ここからは IMMEDIATE モードの場合
        \ HERE は更新しないので、HERE を更新するワードで上書きされる。
        HERE @                          \ 文字列先頭
        BEGIN
            KEY DUP [CHAR] " <>
        WHILE
                OVER C!
                1+
        REPEAT
        DROP                            \ " を捨てる。
        HERE @ -                        \ 文字列長を計算
        HERE @                          \ 文字列先頭
        SWAP
    THEN
;
: abc S" abc" ;
CR abc TYPE
S" def" TYPE

\ 6.1.0190 ." 
\ dot-quote CORE 
\ Compilation: ( "ccc<quote>" -- )
\ Run-time: ( -- )
: ." IMMEDIATE
    STATE @ IF
        [COMPILE] S"
        ['] TYPE ,
    ELSE
        BEGIN
            KEY DUP [CHAR] " = IF
                DROP
                EXIT
            THEN
            EMIT
        AGAIN
    THEN
;
: ABC ." ABC" ;
CR ABC ." DEF"


: DECIMAL ( -- ) 10 BASE ! ;
: HEX ( -- ) 16 BASE ! ;



: doLIST
    15564230926372212880    \ 0xd7ff419090909090 nop x 5 call r15
;

: doCREATE
    15492173332334284944         \ 0xd6ff419090909090 nop x 5 call r14
;

\ 6.1.1000 CREATE
\ CORE ( "<spaces>name" -- ) name Execution: ( -- a-addr )
: CREATE
    WORD HEADER
    doCREATE ,
    0 ,                  \ DOES> がある場合は (DOES>) で書き換えられる。
;

: (DOES>)
    R>                                  \ DOES> の次の rsi を
    LATEST @ >CFA 1 CELLS +             \ CREATE で 0 , したとろこに
    !                                   \ セットする。
;

\ 6.1.1250 DOES>
\ does CORE
: DOES> IMMEDIATE
    ['] (DOES>) ,
    doLIST ,                             \ DOES> の後ろを実行。
;

\ 6.1.0950 CONSTANT
\ CORE
: CONSTANT ( x "<spaces>name" -- )
    CREATE
    ,
    DOES>
      @
;
( : yon doCREATE 0 52 )
CR
CHAR 4 CONSTANT yon
yon yon EMIT EMIT

\ 6.1.2410 VARIABLE
\ CORE ( "<spaces>name" -- ) name Execution: ( -- a-addr )
: VARIABLE
    CREATE
    0 ,
;
CR
VARIABLE var1
CHAR a var1 !
var1 @ EMIT
CHAR b var1 !
var1 @ EMIT

\ 6.2.2405 VALUE 
\ CORE EXT ( x "<spaces>name" -- ) name Execution: ( -- x )
: VALUE
    CREATE
    ,
    DOES>
      @
;

\ 6.2.2295 TO
\ CORE EXT
\ Interpretation: ( x "<spaces>name" -- )
\ Compilation: ( "<spaces>name" -- )
\ Run-time: ( x -- )
: TO IMMEDIATE
    WORD
    FIND
    >CFA
    2 CELLS +                           \ doCREATE, (DOES>)
    STATE @ IF
        ['] LIT ,
        ,
        ['] ! ,
    ELSE
        !
    THEN
;
CR
CHAR A VALUE val1
val1 EMIT
CHAR B TO val1
val1 EMIT
: test-value
    [CHAR] C TO val1
    val1 EMIT
;
test-value


\ 6.1.0710 ALLOT
\ CORE
: ALLOT ( n -- )
    HERE +!
;

\ 6.2.2000 PAD
VARIABLE PAD 1024 ALLOT

\ 11.6.1.2054 R/O
: R/O ( -- fam )
    O_RDONLY
;

\ 11.6.1.2056 R/W
: R/W
    O_RDWR
;

: CSTRING ( addr len -- c-addr )
    SWAP OVER           ( len addr len )
    HERE @ SWAP         ( len addr daddr len )
    CMOVE               ( len )
    HERE @ +            ( dattr+len )
    0 SWAP C!           ( NULL 止め )
    HERE @              ( 文字列の先頭 )
;

\ 11.6.1.1970 OPEN-FILE
: OPEN-FILE ( c-addr u fam -- fileid ior )
    -ROT                ( fam c-addr u )
    CSTRING             ( fam c-addr )
    SYS_OPEN SYSCALL2   ( rax )
    DUP DUP 0< IF
        NEGATE
    ELSE
        DROP 0
    THEN
;

\ 11.6.1.0900 CLOSE-FILE
: CLOSE-FILE ( fileid -- ior )
    SYS_CLOSE SYSCALL1
    NEGATE
;

\ 11.6.1.2080 READ-FILE
: READ-FILE ( c-addr u1 fileid -- u2 ior )
    ROT SWAP    ( u1 c-addr fileid )
    SYS_READ
    SYSCALL3    ( u2 )
    DUP         ( u2 u2 )
    DUP 0< IF
        NEGATE  ( u2 errno )
    ELSE
        DROP 0  ( u2 0 )
    THEN
;
CR S" /etc/passwd" R/O ." OPEN -> " OPEN-FILE .
CR DUP PAD 10 ROT ."READ-FILE -> " READ-FILE . PAD SWAP CR TYPE
CR ." CLOSE -> " CLOSE-FILE .

\ 11.6.1.2090 READ-LINE
: READ-LINE ( c-addr u1 fileid -- u2 flag ior )
    >R OVER 1 R>        ( c-addr u1 c-addr 1 fileid )
    READ-FILE           ( c-addr u1 u2 ior )
    TODO
;


( MAMIMUMEMO )
CR MAMIMUMEMO

: fib
    DUP 2 <= IF
        DROP 1
    ELSE
        DUP 1- RECURSE SWAP 2 - RECURSE +
    THEN
;
CR 10 fib .

CR HELLO
CR ." ok" CR
