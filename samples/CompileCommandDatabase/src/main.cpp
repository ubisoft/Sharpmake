#include <iostream>

#include "hello.h"
#include "goodbye.h"

int main()
{
    libhello::say_hello();
    libgoodbye::say_goodbye();
    return 0;
}

