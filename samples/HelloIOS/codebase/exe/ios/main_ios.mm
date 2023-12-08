
#import <TargetConditionals.h>
#import <UIKit/UIKit.h>
#import <thread>
#import <atomic>
#import <main_ios.h>

int HelloIOS();

namespace {

bool g_LaunchReady = false;
std::mutex g_LaunchMutex;
std::condition_variable g_LaunchCondition;

int g_testErrorCode = 0;
bool g_testRunning = true;

//---------------------------------------------------------------------------------------------------------------------

void TestsThreadLoop(int argumentCount, char** arguments)
{
    g_testRunning = true;
    {
        std::unique_lock<std::mutex> lock(g_LaunchMutex);
        g_LaunchCondition.wait(lock, [](){return g_LaunchReady;});
    }
	sleep(1);

    g_testErrorCode = HelloIOS();
    
    g_testRunning = false;
}

} // anonymous namespace

//--------------------------------------------------------------------------------------------------------------------

@implementation AppDelegate

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions
{
    {
        std::lock_guard<std::mutex> lock(g_LaunchMutex);
        g_LaunchReady = true;
        g_LaunchCondition.notify_one();
    }
    
    return YES;
}

- WaitTestDone
{
    while(g_testRunning){
        sleep(1);
    }
}
- (int) GetTestErrorCode
{
    return g_testErrorCode;
}

- (bool *) GetTestRunningFlag
{
	return &g_testRunning;
}

@end

//---------------------------------------------------------------------------------------------------------------------

@interface GameViewController : UIViewController

// Cache properties for easy retrieval by Window
@property (readonly, assign) CGPoint touchPosition;
@property (readonly, assign) Boolean touchPressed;
@property (readonly, assign) Boolean secondTouchPressed;

@end

@implementation GameViewController
{
    // Keep track of 2 touches.
    // - First touch: we care about its position and if it is currently pressed or not.
    // - Second touch: we emulate a right click when two fingers are pressed on the screen.
    UITouch* trackedTouches[2];
    int currentTrackedTouchesCount;
    
    // Cache properties for easy retrieval by Window
    CGPoint touchPosition;
    Boolean touchPressed;
    Boolean secondTouchPressed;
}

@synthesize touchPosition;
@synthesize touchPressed;
@synthesize secondTouchPressed;

- (instancetype) initWithCoder:(NSCoder *)aDecoder
{
    self = [super initWithCoder:aDecoder];
    
    if(self)
    {
        currentTrackedTouchesCount = 0;
        touchPosition.x = 0;
        touchPosition.y = 0;
        touchPressed = false;
        secondTouchPressed = false;
    }
    
    return self;
}

- (void)touchesBegan:(NSSet *)touches withEvent:(UIEvent *)event
{
    [super touchesBegan:touches withEvent:event];
    
    //
    // Early exit condition
    //
    
    if (currentTrackedTouchesCount >= 2)
        return;
    
    if (event.type != UIEventTypeTouches)
        return;
    
    //
    // Handle touches
    //
    
    for (UITouch *currentTouch in touches)
    {
        if (currentTrackedTouchesCount >= 2)
            return;
        
        trackedTouches[currentTrackedTouchesCount] = currentTouch;
        ++currentTrackedTouchesCount;
        
        if (currentTrackedTouchesCount == 1)
        {
            UIScreen* screen = self.view.window.screen ?: [UIScreen mainScreen];
            
            touchPosition = [currentTouch locationInView:self.view];
            touchPosition.x *= screen.nativeScale;
            touchPosition.y *= screen.nativeScale;
            
            touchPressed = true;
        }
        else
        {
            touchPressed = false;
            secondTouchPressed = true;
        }
        
        auto index = trackedTouches[0] == nil ? 0 : 1;
        trackedTouches[index] = currentTouch;
    }
}
 
- (void)touchesEnded:(NSSet *)touches withEvent:(UIEvent *)event
{
    [super touchesEnded:touches withEvent:event];
    
    //
    // Early exit condition
    //
    
    if (event.type != UIEventTypeTouches)
        return;
        
    //
    // Handle touches
    //
        
    int index;
    
    for (UITouch *currentTouch in touches)
    {
        for(index = 0; index < 2; ++index)
        {
            if(currentTouch == trackedTouches[index])
                break;
        }
        
        if (index == 2)
            continue;
        
        trackedTouches[index] = nil;

        if (currentTrackedTouchesCount == 2)
        {
            secondTouchPressed = false;
        }
        else
        {
            touchPressed = false;
        }
        
        --currentTrackedTouchesCount;
    }
}

@end

//---------------------------------------------------------------------------------------------------------------------

int main(int argumentCount, char** arguments)
{
    std::thread testsThread(TestsThreadLoop, argumentCount, arguments);

    auto errorCode = UIApplicationMain(argumentCount, arguments, nil, NSStringFromClass([AppDelegate class]));

    return errorCode;
}
