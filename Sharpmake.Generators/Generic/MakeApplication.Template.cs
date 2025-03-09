// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.Generic
{
    public partial class MakeApplication
    {
        private static class Template
        {
            public static string ApplicationContent =
@"APP_PLATFORM := [item.AppPlatform]

COMPIL_FINAL = 0

## Compiles in debug mode
ifeq ($(NDK_DEBUG),1)
	APP_OPTIM := debug
else
	APP_OPTIM := release
	COMPIL_FINAL = 1
endif

APP_ABI := [item.Abi]
APP_STL := [item.Stl]

ifeq ($(APP_OPTIM),release)
	APP_CFLAGS := [item.CFlagsRelease]
else
	APP_CFLAGS := [item.CFlagsDebug]
endif

NDK_TOOLCHAIN_VERSION=[item.ToolchainVersion]
";

            public static string MainProjectContent =
@"LOCAL_PATH:= $(call my-dir)
include $(call all-subdir-makefiles)
";
        }
    }
}
