#pragma once
#import "Globals.h"
#import "Renderer.h"

#pragma region ViewController {

#if USE_APPKIT

@interface ViewController : NSViewController
@property (nonatomic, readonly) MTKView* mtkView;
@property (nonatomic, strong) Renderer *renderer;
@end

#elif USE_UIKIT

@interface ViewController : UIViewController
@property (nonatomic, readonly) MTKView* mtkView;
@property (nonatomic, strong) Renderer *renderer;
@end

#endif

#pragma endregion ViewController }
