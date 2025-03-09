#include <iostream>
#include "test1.h"
#include "test2.h"

int main(int, char**)
{
    std::cout << "I was built in "

#if _DEBUG
        "Debug"
#endif

#if NDEBUG
        "Release"
#endif

#if _WIN64
        " x64"
#else
        " x86"
#endif

        << std::endl;

    std::cout << "test1: " << test1() << std::endl;
    std::cout << "test2: " << test2() << std::endl;

    return 0;
}
