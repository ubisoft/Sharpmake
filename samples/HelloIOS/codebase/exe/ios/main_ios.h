
#include <UIKit/UIKit.h>

//--------------------------------------------------------------------------------------------------------------------

@interface AppDelegate : UIResponder <UIApplicationDelegate>

@property (strong, nonatomic) UIWindow *window;

- WaitTestDone;

-(int) GetTestErrorCode;
-(bool*) GetTestRunningFlag;
@end
