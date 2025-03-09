#include "useless_static_lib2.h"
#include <Foundation/NSString.h>
#include <Foundation/NSObjCRuntime.h>

namespace StaticLib2
{
    void UselessMethod()
    {
        NSLog(@"- Useless in fact!");
    }
}

