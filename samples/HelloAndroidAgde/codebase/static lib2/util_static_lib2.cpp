#include "util_static_lib2.h"
#include <iostream>
#include <android/log.h>

Util2::Util2() = default;

Util2::~Util2()
{
}

void Util2::DoSomethingUseful() const
{
#if _DEBUG
    Log("- StaticLib2 is built in Debug"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!");
#endif

#if NDEBUG
    Log("- StaticLib2 is built in Release"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!");
#endif

    return DoSomethingInternal("Yeah right...");
}

void Util2::DoSomethingInternal(const char* anArgument) const
{
    char buff[256];
    snprintf(buff, sizeof(buff), "Useful, right?\n- %s", anArgument);
    Log((const char*)buff);
}

void Util2::Log(const char* s)
{
    __android_log_print(ANDROID_LOG_VERBOSE, "HelloAndroid", "%s", s);
}
