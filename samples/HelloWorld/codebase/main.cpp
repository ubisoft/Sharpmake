#include "stdafx.h"

int main(int, char**)
{
    std::cout << "I was built in "

#if _DEBUG
        "Debug"
#endif

#if NDEBUG
        "Release"
#endif

#if _WIN64
        " x64"
#else
        " x86"
#endif

        << std::endl;

    return 0;
}
