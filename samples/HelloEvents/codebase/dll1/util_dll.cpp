#include "precomp.h"
#include "util_dll.h"

#include "src/util_static_lib1.h"

#pragma warning(push)
#pragma warning(disable: 4668)
#pragma warning(disable: 4710)
#pragma warning(disable: 4711)
#include <iostream>
#pragma warning(pop)

int UtilDll1::ComputeSum(const std::vector<int>& someInts)
{
    int acc = 0;
    for (int item : someInts)
        acc += item;

#if defined(_DEBUG) && _DEBUG
    std::cout << "- Dll1 is built in Debug"
#  if defined(USES_FASTBUILD) && USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

#if defined(NDEBUG) && NDEBUG
    std::cout << "- Dll1 is built in Release"
#  if defined(USES_FASTBUILD) && USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

    acc += static_cast<int>(static_lib1_utils::GetRandomPosition());

    return acc;
}

