#pragma once
#import "Globals.h"

#pragma region AppDelegate {

#if USE_APPKIT

@interface AppDelegate : NSObject <NSApplicationDelegate, NSWindowDelegate>
@property(strong, nonatomic) NSWindow *window;
@end

#elif USE_UIKIT

@interface AppDelegate : UIResponder <UIApplicationDelegate>
@property(strong, nonatomic) UIWindow *window;
@end

#endif

#pragma endregion AppDelegate }
