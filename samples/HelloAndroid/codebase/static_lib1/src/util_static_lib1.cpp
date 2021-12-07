#include "src/pch.h"
#include "util_static_lib1.h"
#include "util_static_lib2.h"
#include <ios>

namespace static_lib1_utils
{
    std::streampos GetRandomPosition()
    {
#if _DEBUG
        Util2::Log("- StaticLib1 is built in Debug"
#if USES_FASTBUILD
            " with FastBuild"
#endif
            "!");
#endif

#if NDEBUG
        Util2::Log("- StaticLib1 is built in Release"
#if USES_FASTBUILD
            " with FastBuild"
#endif
            "!");
#endif

        return 1;
    }
}
