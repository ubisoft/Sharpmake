#include "src/pch.h"
#include "util_static_lib1.h"
#include <ios>

namespace static_lib1_utils
{
    std::streampos GetRandomPosition()
    {
#if defined(_DEBUG) && _DEBUG
    std::cout << "- StaticLib1 is built in Debug"
#  if defined(USES_FASTBUILD) && USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

#if defined(NDEBUG) && NDEBUG
    std::cout << "- StaticLib1 is built in Release"
#  if defined(USES_FASTBUILD) && USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

        return 1;
    }
}

