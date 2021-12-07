#include "precomp.h"
#include "util.h"

#include <cstdio>

#ifndef PRECOMP_INCLUDED
#error This file must have included the pch
#endif

void Util::StaticUtilityMethod()
{
    printf(SOME_UTILITY_STRING " => %s\n", __func__);
}
