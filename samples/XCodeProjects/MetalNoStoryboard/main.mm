#import "Globals.h"
#import "AppDelegate.h"

int main(int argc, char *argv[])
{
    @autoreleasepool
    {
#if USE_APPKIT
        NSApplication* app = NSApplication.sharedApplication;
        app.ActivationPolicy = NSApplicationActivationPolicyRegular;
        //[app setActivationPolicy: NSApplicationActivationPolicyRegular];
        NSMenuItem* item = [NSMenuItem new];
        NSApp.mainMenu = [NSMenu new];
        item.submenu = [NSMenu new];
        [app.mainMenu addItem: item];
        [item.submenu addItem: [[NSMenuItem alloc] initWithTitle:@"Quit" action:@selector(terminate:) keyEquivalent:@"q"]];

        AppDelegate* delegate = [[AppDelegate alloc] init];
        [app setDelegate: delegate];
        return NSApplicationMain(argc, (const char * _Nonnull * _Nonnull)argv);
#elif USE_UIKIT
        return UIApplicationMain(argc, argv, nil, NSStringFromClass([AppDelegate class]));
#endif
    }
}
