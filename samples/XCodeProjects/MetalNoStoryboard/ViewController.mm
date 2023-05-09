#pragma region ViewController {

#import "Globals.h"
#import "ViewController.h"
#import "Renderer.h"

@implementation ViewController
{
    MTKView *_view;
    Renderer *_renderer;
}

- (MTKView *)mtkView
{
    return _view;
}

#if USE_APPKIT

//! override loadView to *avoid* going into NSViewController.loadView which tries to load a nib file
- (void)loadView
{
    // magic below
    // DO NOT CALL [super loadView];
}

#endif // USE_APPKIT


- (void)viewDidLoad
{
    [super viewDidLoad];

    id<MTLDevice> device = MTLCreateSystemDefaultDevice();
    if (!device)
    {
        NSLog(@"Metal is not supported");
        abort();
    }

#if USE_APPKIT
    CGRect frame = (CGRect)[[NSScreen mainScreen] visibleFrame];
#elif USE_UIKIT
    CGRect frame = [[UIScreen mainScreen] bounds];
#endif

    _view = [[MTKView alloc] initWithFrame: frame device: device];

    self.view = self.mtkView;
    //[self.view addSubview: self.mtkView];
    self.renderer = [[Renderer alloc] initWithMetalKitView: self.mtkView];
    self.mtkView.delegate = self.renderer;

    [self.mtkView setPreferredFramesPerSecond:60];
}

- (BOOL)prefersStatusBarHidden
{
    return YES;
}

@end

#pragma endregion ViewController }
