#include "precomp.h"
#include "util_dll.h"

#include <cmath>
#include <iostream>
#include "src/util_static_lib1.h"

int UtilDll1::ComputeMagicNumber(const std::vector<int>& someInts)
{
    int acc = 0;
    for (int item : someInts)
        acc += item;

#if _DEBUG
    std::cout << "- Dll1 is built in Debug"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

#if NDEBUG
    std::cout << "- Dll1 is built in Release"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

    acc += static_lib1_utils::GetRandomPosition();

    acc += cosf(M_PI);

    return acc;
}

