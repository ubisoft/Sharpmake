#import "Globals.h"
#import <fmt/core.h>
#import <string>

bool CopyTextToClipboard(const char* aString)
{
#if IS_MAC_PLATFORM
    NSPasteboard *pasteboard = [NSPasteboard generalPasteboard];
    [pasteboard clearContents];
    bool result = [pasteboard setString: [NSString stringWithCString: aString
                                                            encoding: NSUTF8StringEncoding]
                                forType: NSPasteboardTypeString];
    pasteboard = nil;
    return result;
#else
    UIPasteboard *pasteboard = [UIPasteboard generalPasteboard];
    [pasteboard setString: [NSString stringWithCString: aString
                                                encoding: NSUTF8StringEncoding]];
    pasteboard = nil;
    return true;
#endif // IS_MAC_PLATFORM
}

int main(int argc, char** argv)
{
    if (argc > 1)
    {
        return CopyTextToClipboard(argv[1]) ? 0 : 1;
    }
    else
    {
        return CopyTextToClipboard(argv[0]) ? 0 : 1;
    }
}
