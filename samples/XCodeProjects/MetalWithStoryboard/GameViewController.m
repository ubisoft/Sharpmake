//
//  GameViewController.m
//  MetalWithStoryboard
//
//  Created by Christian Helmich on 04.05.23.
//

#import "Globals.h"
#import "GameViewController.h"
#import "Renderer.h"

@implementation GameViewController
{
    MTKView *_view;

    Renderer *_renderer;
}

- (void)viewDidLoad
{
    [super viewDidLoad];

    _view = (MTKView *)self.view;

    _view.device = MTLCreateSystemDefaultDevice();

#if USE_UIKIT
    _view.backgroundColor = UIColor.blackColor;
#endif

    if(!_view.device)
    {
        NSLog(@"Metal is not supported on this device");

#if USE_UIKIT
        self.view = [[UIView alloc] initWithFrame:self.view.frame];
#elif USE_APPKIT
        self.view = [[NSView alloc] initWithFrame:self.view.frame];
#endif

        return;
    }

    _renderer = [[Renderer alloc] initWithMetalKitView:_view];

    [_renderer mtkView:_view drawableSizeWillChange:_view.bounds.size];

    _view.delegate = _renderer;
}

@end
