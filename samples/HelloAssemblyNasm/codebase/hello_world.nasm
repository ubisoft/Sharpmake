%include "utils.nasm"

section .data
    message db 'Hello world!', 0


section .text
    global hello_world
    extern_function printf

hello_world:
    push rbp
    mov rbp, rsp
    sub rsp, 32

    lea rcx, [rel message]
    call printf

    add rsp, 32
    mov rsp, rbp
    pop rbp
    ret
