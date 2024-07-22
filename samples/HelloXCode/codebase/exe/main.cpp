#include "stdafx.h"
#include "util_dll.h"
#include "util_static_lib2.h"
#include "sub folder/useless_static_lib2.h"

#include <external.h>
#include "systeminclude.h"
#include "systemincludedll.h"

#ifndef ADDITIONAL_COMPILER_FLAG
#error "ADDITIONAL_COMPILER_FLAG not defined"
#endif

extern "C" int add(int a, int b);

int main(int, char**)
{
    std::cout << "Hello XCode World, from " CREATION_DATE "!" << std::endl;

    PrintBuildString("Exe");

    std::vector<int> someArray(5, 6);

    // from plain C file
    std::ignore = add(10, 20);

    // from dll1
    UtilDll1 utilityDll;
    utilityDll.ComputeSum(someArray);

    // from static_lib2
    Util2 utilityStatic;
    utilityStatic.DoSomethingUseful();
    StaticLib2::UselessMethod();
    SystemFct();
    SystemIncludeDllFct();
    return 0;
}
