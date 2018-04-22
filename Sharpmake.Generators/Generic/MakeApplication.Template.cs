// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
