#include "src/pch.h"
#include "util_static_lib1.h"
#include <ios>
#include <external.h>

namespace static_lib1_utils
{
    std::streampos GetRandomPosition()
    {
        PrintBuildString("StaticLib1");

        return 1;
    }
}

