#include "external.h"
#include <iostream>

void PrintBuildString(const char* binaryName)
{
#if _DEBUG
    const char* configName = "Debug";
#elif NDEBUG
    const char* configName = "Release";
#endif

    std::cout << "- " <<
        binaryName <<
        " is built in " <<
        configName <<
#if USES_FASTBUILD
        " with FastBuild"
#endif
        "!" << std::endl;
}
