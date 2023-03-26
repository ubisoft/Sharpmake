#include "src/pch.h"
#include "util_static_lib1.h"
#include <ios>
#include <external.h>
#include <CoreFoundation/CFUUID.h>

namespace static_lib1_utils
{
    std::streampos GetRandomPosition()
    {
        CFUUIDRef   uuidRef = CFUUIDCreate(nullptr);
        CFUUIDBytes data = CFUUIDGetUUIDBytes(uuidRef);
        CFRelease(uuidRef);
        
        PrintBuildString("StaticLib1");

        return 1;
    }
}

