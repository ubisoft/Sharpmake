#pragma region Renderer {

#import "Globals.h"
#import "Renderer.h"
#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>

@implementation Renderer
MTLClearColor color = MTLClearColorMake(0, 0, 0, 1);

-(nonnull instancetype)initWithMetalKitView:(nonnull MTKView *)view;
{
    self = [super init];
    
    if (self)
    {
        _device = view.device;
        _commandQueue = [_device newCommandQueue];
    }

    return self;
}

- (void)drawInMTKView:(nonnull MTKView *)view
{
#if TARGET_OS_OSX
    CGFloat framebufferScale = view.window.screen.backingScaleFactor ?: NSScreen.mainScreen.backingScaleFactor;
#else
    CGFloat framebufferScale = view.window.screen.scale ?: UIScreen.mainScreen.scale;
#endif

    @autoreleasepool
    {
        id<CAMetalDrawable> surface = [view currentDrawable];

        color.red = (color.red > 1.0) ? 0 : color.red + 0.01;

        MTLRenderPassDescriptor *pass = [MTLRenderPassDescriptor renderPassDescriptor];
        pass.colorAttachments[0].clearColor = color;
        pass.colorAttachments[0].loadAction  = MTLLoadActionClear;
        pass.colorAttachments[0].storeAction = MTLStoreActionStore;
        pass.colorAttachments[0].texture = surface.texture;

        id<MTLCommandBuffer> buffer = [_commandQueue commandBuffer];
        id<MTLRenderCommandEncoder> encoder = [buffer renderCommandEncoderWithDescriptor:pass];
        [encoder endEncoding];
        [buffer presentDrawable:surface];   // [buffer presentDrawable:view.currentDrawable];
        [buffer commit];
    }
}

- (void)mtkView:(nonnull MTKView *)view drawableSizeWillChange:(CGSize)size
{
}

@end

#pragma endregion Renderer }
