//
//  AppDelegate.h
//  MetalWithStoryboard
//
//  Created by Christian Helmich on 04.05.23.
//

#import "Globals.h"

#if USE_UIKIT

@interface AppDelegate : UIResponder <UIApplicationDelegate>
@property (strong, nonatomic) UIWindow *window;
@end

#elif USE_APPKIT

@interface AppDelegate : NSObject <NSApplicationDelegate>
@property (assign) IBOutlet NSWindow *window;
@end

#endif
