#import "Globals.h"

#include <cstdio>
#include <string>
#include <fmt/format.h>
#import <Foundation/Foundation.h>

int main(int argc, char** argv)
{
    const char* aFilename = __FILE__;
#if IS_MAC_PLATFORM
    NSString* filename = [NSString stringWithCString: aFilename
                                            encoding: NSUTF8StringEncoding];
    [[NSWorkspace sharedWorkspace] selectFile: [filename stringByResolvingSymlinksInPath]
                        inFileViewerRootedAtPath: @""];
#else
    // UIDocumentBrowserViewController is more involved
#endif // IS_MAC_PLATFORM
    return 0;
}
