#include "noprecomp_util.h"

#include <cstdio>

#ifdef PRECOMP_INCLUDED
#error This file must NOT include the pch
#endif

void Util_NoPrecomp::StaticUtilityMethod()
{
    printf("%s\n", __func__);
}
