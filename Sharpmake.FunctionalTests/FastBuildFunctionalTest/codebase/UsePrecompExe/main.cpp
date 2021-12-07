#include "precomp.h"
#include "util.h"
#include "noprecomp_util.h"
#include "util_noprecomp_excludedbyextension.h"
#include "util_withprecomp_weirdextension.h"
//#include "util_noprecomp.h"

#ifndef PRECOMP_INCLUDED
#error This file must have included the pch
#endif

int main(int, char**)
{
    Util::StaticUtilityMethod();
    Util_NoPrecomp::StaticUtilityMethod();
    Util_NoPrecomp_ExcludedByExtension::StaticUtilityMethod();
    Util_WithPrecomp_WeirdExtension::StaticUtilityMethod();

    return 0;
}
