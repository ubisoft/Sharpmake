#include "stdafx.h"

#include "src/util_static_lib1.h"
#include "util_static_lib2.h"
#include "sub folder/useless_static_lib2.h"

int exe()
{
    Util2::Log("Hello Android World, from " CREATION_DATE "!");

#if _DEBUG
    Util2::Log("- Exe is built in Debug !");
#endif

#if NDEBUG
    Util2::Log("- Exe is built in Release !");
#endif

    // from static_lib1
    static_lib1_utils::GetRandomPosition();

    // from static_lib2
    Util2 utilityStatic;
    utilityStatic.DoSomethingUseful();
    StaticLib2::UselessMethod();
    return 0;
}

/**
 * This is the main entry point of a native application that is using
 * android_native_app_glue.  It runs in its own thread, with its own
 * event loop for receiving input events and doing other things.
 */
void android_main(struct android_app* state)
{
    exit(exe());
}
