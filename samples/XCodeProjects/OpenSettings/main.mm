#import "Globals.h"
#import <fmt/core.h>
#import <sys/sysctl.h>


void RequestOpenSettings()
{
#if IS_MAC_PLATFORM
    [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"x-apple.systempreferences:com.apple.preference"]];
#else
    dispatch_async(dispatch_get_main_queue(), ^{
        [[UIApplication sharedApplication] openURL:[NSURL URLWithString:UIApplicationOpenSettingsURLString] options:@{} completionHandler:nil];
    });
#endif // IS_MAC_PLATFORM
}

int main(int argc, char** argv)
{
    RequestOpenSettings();
    return 0;
}
