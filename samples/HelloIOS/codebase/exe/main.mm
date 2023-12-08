#include "stdafx.h"
#include "src/util_static_lib1.h"
#include "util_static_lib2.h"
#include "sub folder/useless_static_lib2.h"
#include <TargetConditionals.h>
#include <Foundation/NSString.h>
#include <Foundation/NSObjCRuntime.h>

int HelloIOS()
{
    NSLog(@"Hello iOS World, from " CREATION_DATE "!");

#if _DEBUG
    NSLog(@"- Exe is built in Debug"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!");
#endif

#if NDEBUG
    NSLog(@"- Exe is built in Release"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!");
#endif

    std::vector<int> someArray(5, 6);

    // from static_lib1
    static_lib1_utils::ComputeSum(someArray);
    
    // from static_lib2
    Util2 utilityStatic;
    utilityStatic.DoSomethingUseful();
    StaticLib2::UselessMethod();
    return 0;
}
