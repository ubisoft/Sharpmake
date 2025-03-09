INCLUDE utils.asm

.data
    message db "Hello world!", 0

extern_function printf

.code
hello_world proc

    push rbp
    mov rbp, rsp
    sub rsp, 32

    lea rcx, [message]
    call printf

    add rsp, 32
    pop rbp

    ret

hello_world endp

end
