#include "stdafx.h"

int main(int, char**)
{
#if !defined(FIRST_ChildProject_Foo) || !defined(SECOND_ChildProject_FooBar) || !defined(THIRD_ChildProject_Bar)
    #error One of the expected defines was not found!
#endif
    std::cout << "ChildProject!" << std::endl;
    return 0;
}
