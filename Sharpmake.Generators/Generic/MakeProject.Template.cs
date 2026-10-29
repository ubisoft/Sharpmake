// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.Generic
{
    public partial class MakeProject
    {
        private static class Template
        {
            internal const string GlobalTemplate =
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

            internal const string IncludePathTemplate = @"LOCAL_C_INCLUDES += [Path]";

            internal const string SourceFileTemplate = @"LOCAL_SRC_FILES += [Path]";

            internal const string BuildSharedLibraryTemplate = @"include $(BUILD_SHARED_LIBRARY)";
            internal const string BuildStaticLibraryTemplate = @"include $(BUILD_STATIC_LIBRARY)";

            internal const string PrebuiltStaticLibraryTemplate =
@"include $(CLEAR_VARS)
LOCAL_MODULE := [item.ModuleName]
LOCAL_ARM_MODE := [item.ArmMode]
LOCAL_SRC_FILES := [item.LibraryPath]
include $(PREBUILT_STATIC_LIBRARY)
";
        }
    }
}
