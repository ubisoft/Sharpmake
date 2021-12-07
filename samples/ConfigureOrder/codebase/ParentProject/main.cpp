#include "stdafx.h"

int main(int, char**)
{
#if !defined(FIRST_ParentProject_Bar) || !defined(SECOND_ParentProject_Foo)
    #error One of the expected defines was not found!
#endif
    std::cout << "ParentProject!" << std::endl;
    return 0;
}
