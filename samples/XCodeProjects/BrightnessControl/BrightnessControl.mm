#import "BrightnessControl.h"
#import "Globals.h"

#if IS_MAC_PLATFORM
#import <CoreGraphics/CoreGraphics.h>
#import <IOKit/IOKitLib.h>
#include <IOKit/graphics/IOGraphicsLib.h>

extern "C" int DisplayServicesGetBrightness(CGDirectDisplayID id, float *brightness) __attribute__((weak_import));
extern "C" int DisplayServicesSetBrightness(CGDirectDisplayID id, float brightness) __attribute__((weak_import));
#endif // IS_MAC_PLATFORM

float GetScreenBrightness()
{
#if IS_MAC_PLATFORM
    NSScreen *mainScreen = [NSScreen mainScreen];
    auto* dict = [mainScreen deviceDescription];    //NSDictionary<NSDeviceDescriptionKey, id> *
    CGDirectDisplayID displayId = [[dict valueForKey: @"NSScreenNumber"] intValue];
    float value;
    auto ret = DisplayServicesGetBrightness(displayId, &value);
    return value;
#else
    return [UIScreen mainScreen].brightness;
#endif // IS_MAC_PLATFORM
}


void SetScreenBrightness(float aScreenBrightness)
{
#if IS_MAC_PLATFORM
    NSScreen *mainScreen = [NSScreen mainScreen];
    auto* dict = [mainScreen deviceDescription];    //NSDictionary<NSDeviceDescriptionKey, id> *
    CGDirectDisplayID displayId = [[dict valueForKey: @"NSScreenNumber"] intValue];
    DisplayServicesSetBrightness(displayId, aScreenBrightness);
#else
    [UIScreen mainScreen].brightness = aScreenBrightness;
#endif // IS_MAC_PLATFORM
}
