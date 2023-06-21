//
//  main.m
//  MetalWithStoryboard
//
//  Created by Christian Helmich on 04.05.23.
//

#import "Globals.h"
#import "AppDelegate.h"

#if USE_UIKIT

int main(int argc, char * argv[]) {
    NSString * appDelegateClassName;
    @autoreleasepool {
        // Setup code that might create autoreleased objects goes here.
        appDelegateClassName = NSStringFromClass([AppDelegate class]);
    }
    return UIApplicationMain(argc, argv, nil, appDelegateClassName);
}

#elif USE_APPKIT

int main(int argc, const char * argv[]) {
    @autoreleasepool {
        // Setup code that might create autoreleased objects goes here.
    }
    return NSApplicationMain(argc, argv);
}

#endif
