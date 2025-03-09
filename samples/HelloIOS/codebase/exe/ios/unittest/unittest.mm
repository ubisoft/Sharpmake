
#import <XCTest/XCTest.h>

#import <main_ios.h>

@interface HelloIOS_XCTest : XCTestCase
{
@private
    UIApplication * app;
}

@end

@implementation HelloIOS_XCTest

- (void)setUp {
    // Put setup code here. This method is called before the invocation of each test method in the class.
}

- (void)tearDown {
    // Put teardown code here. This method is called after the invocation of each test method in the class.
}

- (void)testHelloXCTest {
    
    app = [UIApplication sharedApplication];

    //Avoid blocking main thread
    XCTestExpectation *expectation = [self expectationWithDescription:@"Test timed out."];
 
	__block AppDelegate* appDelegate = (AppDelegate*)[app delegate];
	__block int testErrorCode = 0;
	bool* testRunning = [appDelegate GetTestRunningFlag];
	
	dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
		// Note: the test is ok with [appDelegate WaitTestDone] with Xcode 15.0.1, iPhoneSimulator17.0, but somehow [appDelegate WaitTestDone] will cause test fail for debug config with Xcode 12.4, iphonesimulator14.4
		// the crash log show it's triggered by objc_autorelease for appDelegate, the crash gone after access the plain type app's variable.
		// but don't know why [appDelegate GetTestErrorCode] doesn't cause crash.
		while(*testRunning)
			sleep(1);
		NSLog(@"WaitTestDone!");
		testErrorCode = [appDelegate GetTestErrorCode];
		NSLog(@"GetTestErrorCode!");
		[expectation fulfill];
	});
	
	auto timeout = 300;// in seconds, this is for test, should set a reasonable value for the specific app
    [self waitForExpectationsWithTimeout:timeout handler:nil]; // Waits until the test fulfills all expectations or until it times out.
	
	NSLog(@"TestErrorCode: %d", testErrorCode);
    XCTAssertEqual(0, testErrorCode);
}

@end
