#import "src/pch.h"
#import "util_static_lib1.h"
#import <Foundation/NSString.h>
#import <Foundation/NSObjCRuntime.h>

namespace static_lib1_utils
{
    std::streampos GetRandomPosition()
    {
#if _DEBUG
    NSLog(@"- StaticLib1 is built in Debug"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!");
#endif

#if NDEBUG
    NSLog(@"- StaticLib1 is built in Release"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!");
#endif

        return 1;
    }

    int ComputeSum(const std::vector<int>& someInts)
    {
        int acc = 0;
        for (int item : someInts)
            acc += item;
        
        acc += static_lib1_utils::GetRandomPosition();
        
        return acc;
    }

}

