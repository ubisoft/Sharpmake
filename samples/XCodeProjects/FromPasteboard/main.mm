#import "Globals.h"
#import <fmt/core.h>
#import <string>

std::string GetTextFromClipboard()
{
    std::string clipboardText;

#if IS_MAC_PLATFORM
    NSPasteboard *pasteboard = [NSPasteboard generalPasteboard];
    NSArray *classArray = [NSArray arrayWithObject:[NSString class]];
    NSDictionary *options = [NSDictionary dictionary];
    if( [pasteboard canReadObjectForClasses:classArray options:options] )
    {
        NSArray *objectsToPaste = [pasteboard readObjectsForClasses:classArray options:options];
        NSString *text = [objectsToPaste firstObject];
        if (text)
        {
            clipboardText = [text UTF8String];
        }
    }
#else
    UIPasteboard *pasteboard = [UIPasteboard generalPasteboard];
    if (pasteboard.hasStrings)
    {
        clipboardText = [[pasteboard string] UTF8String];
    }
    else if (pasteboard.hasURLs)
    {
        clipboardText = [[[pasteboard URL] absoluteString] UTF8String];
    }
#endif // IS_MAC_PLATFORM

    return clipboardText;
}

int main(int argc, char** argv)
{
    auto pbtext = GetTextFromClipboard();
    if (pbtext.empty())
    {
        fmt::println("<paste board is empty>");
        return 1;
    }

    fmt::println("{}", pbtext);
    return 0;
}
