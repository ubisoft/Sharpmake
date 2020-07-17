#include "stdafx.h"
#include "util_dll.h"
#include "util_static_lib2.h"
#include "sub folder/useless_static_lib2.h"

int main(int, char**)
{
    std::cout << "Hello XCode World, from " CREATION_DATE "!" << std::endl;

#if _DEBUG
    std::cout << "- Exe is built in Debug"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

#if NDEBUG
    std::cout << "- Exe is built in Release"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

    std::vector<int> someArray(5, 6);

    // from dll1
    UtilDll1 utilityDll;
    utilityDll.ComputeSum(someArray);

    // from static_lib2
    Util2 utilityStatic;
    utilityStatic.DoSomethingUseful();
    StaticLib2::UselessMethod();
    return 0;
}
