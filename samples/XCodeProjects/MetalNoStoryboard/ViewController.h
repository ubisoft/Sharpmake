#pragma once
#import "Globals.h"
#import "Renderer.h"

#pragma region ViewController {

@interface ViewController : GCEventViewController
@property (nonatomic, readonly) MTKView* mtkView;
@property (nonatomic, strong) Renderer *renderer;
@end

#pragma endregion ViewController }
