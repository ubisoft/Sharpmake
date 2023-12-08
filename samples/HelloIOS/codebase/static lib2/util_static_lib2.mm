#import "util_static_lib2.h"
#import <Foundation/NSString.h>
#import <Foundation/NSObjCRuntime.h>

Util2::Util2() = default;

Util2::~Util2()
{
}

void Util2::DoSomethingUseful() const
{
#if _DEBUG
    NSLog(@"- StaticLib2 is built in Debug"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!");
#endif

#if NDEBUG
    NSLog(@"- StaticLib2 is built in Release"
#  if USES_FASTBUILD
        " with FastBuild"
#  endif
        "!");
#endif

    return DoSomethingInternal("Yeah right...");
}

void Util2::DoSomethingInternal(const char* anArgument) const
{
    NSString *nsstringFormat = [NSString localizedStringWithFormat:@"Useful, right?\n- %@", [NSString stringWithUTF8String:anArgument]];
    NSLog(@"%@", nsstringFormat);
}
