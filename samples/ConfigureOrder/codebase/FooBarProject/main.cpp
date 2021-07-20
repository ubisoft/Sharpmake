#include "stdafx.h"

int main(int, char**)
{
#if !defined(FIRST_FooBarProject_Bar) || !defined(SECOND_FooBarProject_Foo)
    #error One of the expected defines was not found!
#endif
    std::cout << "FooBarProject!" << std::endl;
    return 0;
}
