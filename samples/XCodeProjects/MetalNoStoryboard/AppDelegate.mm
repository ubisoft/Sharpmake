#import "Globals.h"
#import "AppDelegate.h"
#import "Renderer.h"
#import "ViewController.h"

#pragma region AppDelegate {

@implementation AppDelegate

#if USE_APPKIT

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
    NSRect frame = [[NSScreen mainScreen] frame];
    self.window = [[NSWindow alloc] initWithContentRect: frame
                                    styleMask: NSWindowStyleMaskFullScreen | NSWindowStyleMaskFullSizeContentView | NSWindowStyleMaskBorderless //NSWindowStyleMaskClosable | 
                                    backing: NSBackingStoreBuffered
                                    defer: NO];
    ViewController *viewController = [[ViewController alloc] init];
    [viewController viewDidLoad];
    [self.window setContentViewController: viewController];
    [self.window setContentView: viewController.view];
    [self.window makeKeyAndOrderFront: self];
    // [self.window toggleFullScreen: nil];

    return YES;
}

- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)sender
{
    return YES;
}

#elif USE_UIKIT

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions
{
    CGRect frame = [[UIScreen mainScreen] bounds];

    self.window = [[UIWindow alloc] initWithFrame: frame];
    ViewController *viewController = [[ViewController alloc] init];
    [self.window setRootViewController: viewController];
    [self.window makeKeyAndVisible];

    return YES;
}

- (void)applicationWillResignActive:(UIApplication *)application
{
    // Sent when the application is about to move from active to inactive state. This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) or when the user quits the application and it begins the transition to the background state.
    // Use this method to pause ongoing tasks, disable timers, and throttle down OpenGL ES frame rates. Games should use this method to pause the game.
}

- (void)applicationDidEnterBackground:(UIApplication *)application
{
    // Use this method to release shared resources, save user data, invalidate timers, and store enough application state information to restore your application to its current state in case it is terminated later.
    // If your application supports background execution, this method is called instead of applicationWillTerminate: when the user quits.
}

- (void)applicationWillEnterForeground:(UIApplication *)application
{
    // Called as part of the transition from the background to the inactive state; here you can undo many of the changes made on entering the background.
}

- (void)applicationDidBecomeActive:(UIApplication *)application
{
    // Restart any tasks that were paused (or not yet started) while the application was inactive. If the application was previously in the background, optionally refresh the user interface.
}

- (void)applicationWillTerminate:(UIApplication *)application
{
    // Called when the application is about to terminate. Save data if appropriate. See also applicationDidEnterBackground:.
}

#endif

@end

#pragma endregion AppDelegate }
