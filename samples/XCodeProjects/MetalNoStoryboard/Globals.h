#pragma once

#pragma region GlobalImports {

#include <cassert>
#import <TargetConditionals.h>
#import <Foundation/Foundation.h>
#import <QuartzCore/QuartzCore.h>
#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>

#if __has_include(<UIKit/UIKit.h>)
#import <UIKit/UIKit.h>
#define USE_UIKIT 1

#elif TARGET_OS_MAC
#if __has_include(<AppKit/AppKit.h>)
#import <AppKit/AppKit.h>
#define USE_APPKIT 1
#endif

#if __has_include(<Cocoa/Cocoa.h>)
#import <Cocoa/Cocoa.h>
#endif
#endif // TARGET_OS_MAC

#pragma endregion GlobalImports }
