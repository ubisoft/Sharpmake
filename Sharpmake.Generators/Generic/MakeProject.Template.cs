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
    public partial class MakeProject
    {
        private static class Template
        {
            public static string GlobalTemplate =
@"include $(CLEAR_VARS)

ifeq ($(COMPIL_FINAL),1)
	LOCAL_EXPORT_CFLAGS += [item.CFlagsExportedFinal]
else
    ifeq ($(APP_OPTIM),release)
        LOCAL_EXPORT_CFLAGS += [item.CFlagsExportedRelease]
    else
	    LOCAL_EXPORT_CFLAGS += [item.CFlagsExportedDebug]
    endif
endif

ifeq ($(COMPIL_FINAL),1)
	[item.PrebuiltStaticLibrariesFinal]
else
    ifeq ($(APP_OPTIM),release)
        [item.PrebuiltStaticLibrariesRelease]
    else
	    [item.PrebuiltStaticLibrariesDebug]
    endif
endif

[item.PrebuiltStaticLibraries]

include $(CLEAR_VARS)

[item.IncludePaths]

LOCAL_MODULE := [item.ModuleName]
LOCAL_ARM_MODE := [item.ArmMode]
LOCAL_SHORT_COMMANDS := [item.ShortCommands]

ifeq ($(COMPIL_FINAL),1)
	LOCAL_CFLAGS += [item.CFlagsFinal]
else
    ifeq ($(APP_OPTIM),release)
        LOCAL_CFLAGS += [item.CFlagsRelease]
    else
	    LOCAL_CFLAGS += [item.CFlagsDebug]
    endif
endif

[item.SourcePaths]

LOCAL_GROUP_STATIC_LIBRARIES := [item.GroupStaticLibraries]
LOCAL_STATIC_LIBRARIES := [item.StaticLibraries]

LOCAL_LDLIBS := [item.SharedLibraries]

[item.BuildType]
";

            public static string IncludePathTemplate = @"LOCAL_C_INCLUDES += [Path]";

            public static string SourceFileTemplate = @"LOCAL_SRC_FILES += [Path]";

            public static string BuildSharedLibraryTemplate = @"include $(BUILD_SHARED_LIBRARY)";
            public static string BuildStaticLibraryTemplate = @"include $(BUILD_STATIC_LIBRARY)";

            public static string PrebuiltStaticLibraryTemplate =
@"include $(CLEAR_VARS)
LOCAL_MODULE := [item.ModuleName]
LOCAL_ARM_MODE := [item.ArmMode]
LOCAL_SRC_FILES := [item.LibraryPath]
include $(PREBUILT_STATIC_LIBRARY)
";
        }
    }
}
