#include "util_static_lib2.h"
#include <iostream>

Util2::Util2() = default;

Util2::~Util2()
{
}

void Util2::DoSomethingUseful() const
{
#if _DEBUG
    std::cout << "- StaticLib2 is built in Debug"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

#if NDEBUG
    std::cout << "- StaticLib2 is built in Release"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

    return DoSomethingInternal("Yeah right...");
}

void Util2::DoSomethingInternal(const char* anArgument) const
{
    std::cout << "Useful, right?\n- " << anArgument << std::endl;
}
