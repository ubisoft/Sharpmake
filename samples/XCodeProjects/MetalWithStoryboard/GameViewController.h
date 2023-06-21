//
//  GameViewController.h
//  MetalWithStoryboard
//
//  Created by Christian Helmich on 04.05.23.
//

#import "Globals.h"
#import "Renderer.h"

#if USE_UIKIT

// Our iOS view controller
@interface GameViewController : UIViewController

@end

#elif USE_APPKIT

// Our macOS view controller
@interface GameViewController : NSViewController

@end

#endif
