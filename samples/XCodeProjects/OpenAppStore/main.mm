#import "Globals.h"
#import <fmt/core.h>
#import <sys/sysctl.h>


void OpenStore(const unsigned int aStoreId)
{
#if IS_MAC_PLATFORM
    NSString* ituneLink = [NSString stringWithFormat:@"macappstore://itunes.apple.com/app/id%u", aStoreId];
    [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:ituneLink]];
#else
    NSString* ituneLink = [NSString stringWithFormat:@"itms-apps://itunes.apple.com/app/id%u", aStoreId];
    dispatch_async(dispatch_get_main_queue(), ^{
        [[UIApplication sharedApplication] openURL:[NSURL URLWithString:ituneLink] options:@{} completionHandler:nil];
    });
#endif // IS_MAC_PLATFORM
}

int main(int argc, char** argv)
{
    OpenStore(0);
    return 0;
}
