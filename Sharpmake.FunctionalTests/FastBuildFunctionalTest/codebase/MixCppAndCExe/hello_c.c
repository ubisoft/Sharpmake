#include <stdio.h>

#ifdef __cplusplus
#error This file mustn't be compiled as C++
#endif

void hello_c(void)
{
    printf("Hello from C code!\n");
}
