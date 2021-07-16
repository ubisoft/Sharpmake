#include "util_static_lib2.h"
#include <iostream>
#include <external.h>

Util2::Util2() = default;

Util2::~Util2()
{
}

void Util2::DoSomethingUseful() const
{
    PrintBuildString("StaticLib2");

    return DoSomethingInternal("Yeah right...");
}

void Util2::DoSomethingInternal(const char* anArgument) const
{
    std::cout << "Useful, right?\n- " << anArgument << std::endl;
}
