#import "Globals.h"
#import <fmt/format.h>
#import <CoreFoundation/CoreFoundation.h>
#import <cstdio>

int main(int argc, char** argv)
{
    NSArray* paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
    NSString* documentsDirectory = [paths objectAtIndex:0];
    NSLog(@"documentsDirectory: %@\n", documentsDirectory);

    CFBundleRef cf_mainBundle = CFBundleGetMainBundle();
    CFURLRef cf_resourceDir = CFURLCopyAbsoluteURL(CFBundleCopyResourcesDirectoryURL(cf_mainBundle));
    CFStringRef cf_resourceDirS = CFURLGetString(cf_resourceDir);
    fmt::println("resourceDirS (CF): {}", CFStringGetCStringPtr(cf_resourceDirS, kCFStringEncodingUTF8));

    NSBundle* mainBundle = [NSBundle mainBundle];
    NSURL* resourceDir = [mainBundle resourceURL];
    NSLog(@"mainBundle (abs): %@\n", [resourceDir absoluteString]);
    NSLog(@"mainBundle (path): %@\n", [resourceDir path]);

    std::string contents("");
    NSArray* contentFiles = @[@"foobar.dat", @"huba/hoge.dat"];
    for (id contentFile in contentFiles)
    {
        NSURL* fileUrl = [NSURL fileURLWithPath: contentFile relativeToURL: resourceDir];
        NSLog(@"fileUrl: %@\n", [fileUrl path]);

        FILE* file = fopen([[fileUrl path] UTF8String], "r");
        if (file)
        {
            char c;
            while (!feof(file))
            {
                fread(&c, sizeof(char), 1, file);
                contents += c;
            }
        }
        else
        {
            fmt::println(stderr, "could not find {}", [[fileUrl path] UTF8String]);
        }
        fclose(file);
    }

    fmt::print("contents: '{}'", contents);
    return 0;
}


//NSApplicationSupportDirectory
