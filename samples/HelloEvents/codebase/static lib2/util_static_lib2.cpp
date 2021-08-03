#include "util_static_lib2.h"

#pragma warning(push)
#pragma warning(disable: 4668)
#pragma warning(disable: 4710)
#pragma warning(disable: 4711)
#include <iostream>
#pragma warning(pop)

Util2::Util2() = default;

Util2::~Util2()
{
}

void Util2::DoSomethingUseful() const
{
#if defined(_DEBUG) && _DEBUG
    std::cout << "- StaticLib2 is built in Debug"
#  if defined(USES_FASTBUILD) && USES_FASTBUILD
        " with FastBuild"
#  endif
        "!" << std::endl;
#endif

#if defined(NDEBUG) && NDEBUG
    std::cout << "- StaticLib2 is built in Release"
#  if defined(USES_FASTBUILD) && USES_FASTBUILD
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
