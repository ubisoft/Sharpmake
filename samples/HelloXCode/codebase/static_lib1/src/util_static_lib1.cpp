#include "src/pch.h"
#include "util_static_lib1.h"
#include <ios>

namespace static_lib1_utils
{
    std::streampos GetRandomPosition()
    {
#if _DEBUG
    std::cout << "- StaticLib1 is built in Debug!" << std::endl;
#endif

#if NDEBUG
    std::cout << "- StaticLib1 is built in Release!" << std::endl;
#endif

        return 1;
    }
}

