#pragma once
#import "Globals.h"

#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>

#pragma region Renderer {

@interface Renderer : NSObject <MTKViewDelegate>
-(nonnull instancetype)initWithMetalKitView:(nonnull MTKView *)view;
@end

@interface Renderer ()
@property (nonatomic, strong) id <MTLDevice> device;
@property (nonatomic, strong) id <MTLCommandQueue> commandQueue;
@end

#pragma endregion Renderer }
