#ifndef __cplusplus
#error This file mustn't be compiled as C
#endif

#include "hello_c.h"

#define _HAS_EXCEPTIONS 0

#include <iostream>

int main(int, char**)
{
    hello_c();
    std::cout << "Hello from C++!" << std::endl;
    return 0;
}
