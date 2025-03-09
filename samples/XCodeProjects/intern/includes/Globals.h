#pragma once

#pragma region GlobalImports {

#include <assert.h>
#import <TargetConditionals.h>
#import <Foundation/Foundation.h>
#import <QuartzCore/QuartzCore.h>
#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>
#import <GameController/GameController.h>

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

#if TARGET_OS_MACCATALYST
#define IS_CATALYST_PLATFORM      (1)
#elif TARGET_OS_IOS
#define IS_IOS_PLATFORM           (1)
#elif TARGET_OS_TV
#define IS_TVOS_PLATFORM          (1)
#elif TARGET_OS_OSX
#define IS_MAC_PLATFORM           (1)
#endif

#ifndef IS_CATALYST_PLATFORM
#define IS_CATALYST_PLATFORM      (0)
#endif

#ifndef IS_IOS_PLATFORM
#define IS_IOS_PLATFORM           (0)
#endif

#ifndef IS_TVOS_PLATFORM
#define IS_TVOS_PLATFORM          (0)
#endif

#ifndef IS_MAC_PLATFORM
#define IS_MAC_PLATFORM           (0)
#endif

#pragma endregion GlobalImports }
