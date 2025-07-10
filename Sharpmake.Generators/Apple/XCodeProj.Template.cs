// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Sharpmake.Generators.Apple
{
    public partial class XCodeProj
    {
        private static class Template
        {
            public static string GlobalHeader =
@"// !$*UTF8*$!
{
	archiveVersion = [archiveVersion];
	classes = {
	};
	objectVersion = [objectVersion];
	objects = {

";
            public static string GlobalFooter =
@"	};
	rootObject = [RootObject.Uid] /* Project object */;
}
";

            public static string SectionBegin = @"/* Begin [item.SectionString] section */
";
            public static string SectionEnd = @"/* End [item.SectionString] section */

";

            public static string SectionSubItem =
@"				[item.Uid] /* [item.Identifier] */,
";
            public static string ProjectReferenceSubItem =
@"				{
					ProductGroup = [group.Uid] /* Products */;
					ProjectRef = [project.Uid] /* [project.Name] */;
				},
";
            public static string ProjectTargetAttribute =
@"					[item.Uid] /* [item.Identifier] */ = {
						DevelopmentTeam = [project.DevelopmentTeam];
						ProvisioningStyle = [project.ProvisioningStyle];
						SystemCapabilities = {
							com.apple.iCloud = {
								enabled = [project.ICloudSupport];
							};
						};
					};
";

            public static Dictionary<ItemSection, string> Section = new Dictionary<ItemSection, string>
            {
                { ItemSection.PBXBuildFile,
@"		[item.Uid] /* [item.File.Name] in [item.File.Type] */ = {
			isa = PBXBuildFile;
			fileRef = [item.File.Uid] /* [item.File.Name] */;
			settings = [item.Settings];
		};
"               },

                { ItemSection.PBXContainerItemProxy,
@"		[item.Uid] /* PBXContainerItemProxy */ = {
			isa = PBXContainerItemProxy;
			containerPortal = [item.ProjectReference.Uid];
			proxyType = [item.ProxyType];
			remoteGlobalIDString = [item.ProxyItem.Uid];
			remoteInfo = [item.ProjectReference.ProjectName];
		};
"               },

                { ItemSection.PBXFileReference,
@"		[item.Uid] /* [item.Name] */ = {
			isa = PBXFileReference;
			explicitFileType = [item.ExplicitFileType];
			lastKnownFileType = [item.FileType];
			includeInIndex = [item.IncludeInIndex];
			name = ""[item.Name]"";
			path = ""[item.Path]"";
			sourceTree = [item.SourceTree];
		};
"               },

                { ItemSection.PBXFrameworksBuildPhase,
@"		[item.Uid] /* Frameworks */ = {
			isa = PBXFrameworksBuildPhase;
			buildActionMask = [item.BuildActionMask];
			files = (
[itemChildren]			);
			runOnlyForDeploymentPostprocessing = [item.RunOnlyForDeploymentPostprocessing];
		};
"               },

                {ItemSection.PBXShellScriptBuildPhase,
@"		[item.Uid] /* Scripts */ =
		{
			isa = PBXShellScriptBuildPhase;
			buildActionMask = 2147483647;
			files = ();
			inputFileListPaths = ();
			inputPaths = ();
			outputFileListPaths = ();
			outputPaths = ();
			runOnlyForDeploymentPostprocessing = 0;
			shellPath = /bin/sh;
			shellScript = ""[item.script]"";
		};
"
                },

                { ItemSection.PBXGroup,
@"		[item.Uid] /* [item.Identifier] */ = {
			isa = PBXGroup;
			children = (
[itemChildren]			);
			name = ""[item.Name]"";
			path = ""[item.Path]"";
			sourceTree = [item.SourceTree];
			usesTabs = [editorOptions.IndentUseTabs];
		};
"               },

                { ItemSection.PBXNativeTarget,
@"		[item.Uid] /* [item.Identifier] */ = {
			isa = PBXNativeTarget;
			buildConfigurationList = [item.ConfigurationList.Uid] /* Build configuration list for PBXNativeTarget ""[item.Identifier]"" */;
			buildPhases = (
				[item.ShellScriptPreBuildPhaseUIDs] /* ShellScripts (PreBuild) */,
				[item.HeadersBuildPhasesUIDs] /* Headers */,
				[item.CopyFilePreBuildPhasesUIDs] /* CopyFiles (PreBuild) */,
				[item.ResourcesBuildPhase.Uid] /* Resources */,
				[item.CopyFileBuildPhasesUIDs] /* CopyFiles */,
				[item.SourceBuildPhaseUID] /* Sources */,
				[item.FrameworksBuildPhase.Uid] /* Frameworks */,
				[item.CopyFilePostBuildPhasesUIDs] /* CopyFiles (Post Build) */,
				[item.ShellScriptPostBuildPhaseUIDs] /* ShellScripts (PreBuild) */,
			);
			buildRules = (
			);
			dependencies = (
[itemChildren]			);
			name = ""[item.Identifier]"";
			productInstallPath = ""[item.ProductInstallPath]"";
			productName = ""[item.Identifier]"";
			productReference = [item.OutputFile.Uid] /* [item.OutputFile.Name] */;
			productType = ""[item.ProductType]"";
		};
"               },

                { ItemSection.PBXLegacyTarget,
@"		[item.Uid] /* [item.Identifier] */ = {
			isa = PBXLegacyTarget;
			buildArgumentsString = ""[item.BuildArgumentsString]"";
			buildConfigurationList = [item.ConfigurationList.Uid] /* Build configuration list for PBXLegacyTarget ""[item.Identifier]"" */;
			buildPhases = (
			);
			buildToolPath = ""[item.BuildToolPath]"";
			buildWorkingDirectory = ""[item.BuildWorkingDirectory]"";
			dependencies = (
			);
			name = ""[item.Identifier]"";
			passBuildSettingsInEnvironment = 1;
			productName = ""[item.Identifier]"";
			productType = ""[item.ProductType]"";
		};
"               },

                { ItemSection.PBXProject,
@"		[item.Uid] /* Project object */ = {
			isa = PBXProject;
			attributes = {
				TargetAttributes = {
[itemTargetAttributes]				};
			};
			buildConfigurationList = [item.ConfigurationList.Uid] /* Build configuration list for PBXProject ""[item.Identifier]"" */;
			compatibilityVersion = ""[item.CompatibilityVersion]"";
			developmentRegion = en;
			hasScannedForEncodings = 0;
			knownRegions = (
				en,
				Base,
			);
			mainGroup = [item.MainGroup.Uid] /* [item.MainGroup.Name] */;
			projectDirPath = """";
			projectReferences = (
[itemProjectReferences]			);
			projectRoot = """";
			targets = (
[itemTargets]			);
		};
"               },

                { ItemSection.PBXReferenceProxy,
@"		[item.Uid] /* [item.OutputFile.Name] */ = {
			isa = PBXReferenceProxy;
			fileType = [item.FileType];
			path = [item.OutputFile.FileName];
			remoteRef = [item.Proxy.Uid] /* [item.Proxy.Identifier] */;
			sourceTree = [item.SourceTree];
		};
"               },

                { ItemSection.PBXResourcesBuildPhase,
@"		[item.Uid] /* Resources */ = {
			isa = PBXResourcesBuildPhase;
			buildActionMask = [item.BuildActionMask];
			files = (
[itemChildren]			);
			runOnlyForDeploymentPostprocessing = [item.RunOnlyForDeploymentPostprocessing];
		};
"               },

                { ItemSection.PBXSourcesBuildPhase,
@"		[item.Uid] /* Sources */ = {
			isa = PBXSourcesBuildPhase;
			buildActionMask = [item.BuildActionMask];
			files = (
[itemChildren]			);
			runOnlyForDeploymentPostprocessing = [item.RunOnlyForDeploymentPostprocessing];
		};
"               },

                { ItemSection.PBXHeadersBuildPhase,
@"		[item.Uid] /* Headers */ = {
			isa = PBXHeadersBuildPhase;
			buildActionMask = [item.BuildActionMask];
			files = (
[itemChildren]			);
			runOnlyForDeploymentPostprocessing = [item.RunOnlyForDeploymentPostprocessing];
		};
"               },

                { ItemSection.PBXCopyFilesBuildPhase,
@"		[item.Uid] /* [item.Identifier] */ = {
			isa = PBXCopyFilesBuildPhase;
			buildActionMask = [item.BuildActionMask];
			name = ""[item.Identifier]"";
			dstPath = ""[item.TargetPath]"";
			dstSubfolderSpec = [item.FolderSpec];
			runOnlyForDeploymentPostprocessing = [item.RunOnlyForDeploymentPostprocessing];
			files = (
[itemChildren]			);
		};
"               },

                { ItemSection.PBXVariantGroup,
@"
"               },

                { ItemSection.PBXTargetDependency,
@"		[item.Uid] /* PBXTargetDependency */ = {
			isa = PBXTargetDependency;
			name = ""[item.ProjectReference.Name]"";
			targetProxy = [item.Proxy.Uid];
			target = [item.TargetIdentifier];
		};
"               },

                { ItemSection.XCBuildConfiguration_NativeTarget,
@"		[item.Uid] /* Native Target - [item.Optimization] */ = {
			isa = XCBuildConfiguration;
			buildSettings = {
				CODE_SIGN_ENTITLEMENTS = ""[item.Options.CodeSignEntitlements]"";
				CODE_SIGN_IDENTITY = ""[item.Options.CodeSigningIdentity]"";
				""CODE_SIGN_IDENTITY[sdk=iphoneos*]"" = ""[item.Options.CodeSigningIdentity]"";
				CONFIGURATION_BUILD_DIR = ""[item.Options.BuildDirectory]"";
				COPY_PHASE_STRIP = [item.Options.StripDebugSymbolsDuringCopy];
				DEAD_CODE_STRIPPING = [item.Options.DeadStripping];
				DEBUG_INFORMATION_FORMAT = [item.Options.DebugInformationFormat];
				DEPLOYMENT_POSTPROCESSING = [item.Options.DeploymentPostProcessing];
				DEVELOPMENT_TEAM = [item.Options.DevelopmentTeam];
				ENABLE_BITCODE = [item.Options.EnableBitcode];
				EXCLUDED_SOURCE_FILE_NAMES = [item.Options.ExcludedSourceFileNames];
				FRAMEWORK_SEARCH_PATHS = [item.Options.FrameworkPaths];
				FASTBUILD_TARGET = ""[item.Options.FastBuildTarget]"";
				GCC_DYNAMIC_NO_PIC = [item.Options.DynamicNoPic];
				GCC_ENABLE_CPP_EXCEPTIONS = [item.Options.CppExceptionHandling];
				GCC_ENABLE_CPP_RTTI = [item.Options.RuntimeTypeInfo];
				GCC_ENABLE_OBJC_EXCEPTIONS = [item.Options.ObjCExceptionHandling];
				CLANG_ENABLE_OBJC_ARC_EXCEPTIONS = [item.Options.ObjCARCExceptionHandling];
				GCC_GENERATE_DEBUGGING_SYMBOLS = [item.Options.GenerateDebuggingSymbols];
				GCC_INLINES_ARE_PRIVATE_EXTERN = [item.Options.PrivateInlines];
				GCC_MODEL_TUNING = [item.Options.ModelTuning];
				GCC_SYMBOLS_PRIVATE_EXTERN = [item.Options.PrivateSymbols];
				HEADER_SEARCH_PATHS = [item.Options.IncludePaths];
				SYSTEM_HEADER_SEARCH_PATHS = [item.Options.IncludeSystemPaths];
				INFOPLIST_FILE = ""[item.Options.InfoPListFile]"";
				INSTALL_PATH = ""[item.Options.ProductInstallPath]"";
				IPHONEOS_DEPLOYMENT_TARGET = ""[item.Options.IPhoneOSDeploymentTarget]"";
				TVOS_DEPLOYMENT_TARGET = ""[item.Options.TvOSDeploymentTarget]"";
				MACOSX_DEPLOYMENT_TARGET = [item.Options.MacOSDeploymentTarget];
				WATCHOS_DEPLOYMENT_TARGET = ""[item.Options.WatchOSDeploymentTarget]"";
				LIBRARY_SEARCH_PATHS = [item.Options.LibraryPaths];
				LD_RUNPATH_SEARCH_PATHS = [item.Options.LdRunPaths];
				""LIBRARY_SEARCH_PATHS[sdk=iphoneos*]"" = [item.Options.SpecificDeviceLibraryPaths];
				""LIBRARY_SEARCH_PATHS[sdk=iphonesimulator*]"" = [item.Options.SpecificSimulatorLibraryPaths];
				MACH_O_TYPE = ""[item.Options.MachOType]"";
				PRESERVE_DEAD_CODE_INITS_AND_TERMS = [item.Options.PreserveDeadCodeInitsAndTerms];
				PRODUCT_BUNDLE_IDENTIFIER = ""[item.Options.ProductBundleIdentifier]"";
				PRODUCT_NAME = ""[item.Configuration.TargetFileName]"";
				MARKETING_VERSION = ""[item.Options.ProductBundleVersion]"";
				CURRENT_PROJECT_VERSION = ""[item.Options.ProductBundleShortVersion]"";
				PROVISIONING_PROFILE_SPECIFIER = ""[item.Options.ProvisioningProfile]"";
				SKIP_INSTALL = [item.Options.SkipInstall];
				STRIP_INSTALLED_PRODUCT = [item.Options.StripLinkedProduct];
				STRIP_STYLE= [item.Options.StripStyle];
				STRIPFLAGS = ""[item.Options.AdditionalStripFlags]"";
				STRIP_SWIFT_SYMBOLS = [item.Options.StripSwiftSymbols];
				SYMROOT = ""[item.Options.BuildDirectory]"";
				VALID_ARCHS = ""[item.Options.ValidArchs]"";
				GENERATE_MASTER_OBJECT_FILE = [item.Options.GenerateMasterObjectFile];
				PRELINK_LIBS = ""[item.Options.PreLinkedLibraries]"";
				MTL_FAST_MATH = [item.Options.MetalFastMath];
				SUPPORTS_MACCATALYST = ""[item.Options.SupportsMaccatalyst]"";
				SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD = ""[item.Options.SupportsMacDesignedForIphoneIpad]"";
				GENERATE_INFOPLIST_FILE = [item.Options.GenerateInfoPlist];
				SWIFT_EMIT_LOC_STRINGS = [item.Options.SwiftEmitLocStrings];
				INFOPLIST_KEY_CFBundleDisplayName = ""[item.Options.ProductBundleDisplayName]"";
				INFOPLIST_KEY_CFBundleSpokenName = ""[item.Options.CFBundleSpokenName]"";
				INFOPLIST_KEY_CFBundleVersion = ""[item.Options.ProductBundleVersion]"";
				INFOPLIST_KEY_CFBundleShortVersionString = ""[item.Options.ProductBundleShortVersion]"";
				INFOPLIST_KEY_CFBundleDevelopmentRegion = ""[item.Options.CFBundleDevelopmentRegion]"";
				INFOPLIST_KEY_CFBundleExecutable = ""[item.Options.CFBundleExecutable]"";
				INFOPLIST_KEY_CFBundleLocalizations = [item.Options.CFBundleLocalizations];
				INFOPLIST_KEY_CFBundleAllowMixedLocalizations = [item.Options.CFBundleAllowMixedLocalizations];
				INFOPLIST_KEY_NSHighResolutionCapable = [item.Options.NSHighResolutionCapable];
				INFOPLIST_KEY_NSHumanReadableCopyright = ""[item.Options.NSHumanReadableCopyright]"";
				INFOPLIST_KEY_LSMinimumSystemVersion = [item.Options.MacOSDeploymentTarget];
				INFOPLIST_KEY_NSMainStoryboardFile = [item.Options.NSMainStoryboardFile];
				INFOPLIST_KEY_NSMainNibFile = [item.Options.NSMainNibFile];
				INFOPLIST_KEY_NSPrefPaneIconFile = [item.Options.NSPrefPaneIconFile];
				INFOPLIST_KEY_NSPrefPaneIconLabel = [item.Options.NSPrefPaneIconLabel];
				INFOPLIST_KEY_NSPrincipalClass = [item.Options.NSPrincipalClass];
				INFOPLIST_KEY_NSPrefersDisplaySafeAreaCompatibilityMode = [item.Options.NSPrefersDisplaySafeAreaCompatibilityMode];
				INFOPLIST_KEY_NSSupportsAutomaticGraphicsSwitching = [item.Options.NSSupportsAutomaticGraphicsSwitching];
				INFOPLIST_KEY_LSMultipleInstancesProhibited = [item.Options.LSMultipleInstancesProhibited];
				INFOPLIST_KEY_LSRequiresNativeExecution = [item.Options.LSRequiresNativeExecution];
				INFOPLIST_KEY_UISupportsTrueScreenSizeOnMac = [item.Options.UISupportsTrueScreenSizeOnMac];
				INFOPLIST_KEY_LSRequiresIPhoneOS = [item.Options.LSRequiresIPhoneOS];
				INFOPLIST_KEY_UIRequiredDeviceCapabilities = [item.Options.UIRequiredDeviceCapabilities];
				INFOPLIST_KEY_UIMainStoryboardFile = [item.Options.UIMainStoryboardFile];
				INFOPLIST_KEY_UILaunchStoryboardName = [item.Options.UILaunchStoryboardName];
				INFOPLIST_KEY_CFBundleIconFile = [item.Options.CFBundleIconFile];
				INFOPLIST_KEY_CFBundleIconFiles = ""[item.Options.CFBundleIconFiles]"";
				INFOPLIST_KEY_CFBundleIconName = [item.Options.CFBundleIconName];
				INFOPLIST_KEY_UIPrerenderedIcon = [item.Options.UIPrerenderedIcon];
				INFOPLIST_KEY_UIInterfaceOrientation = [item.Options.UIInterfaceOrientation];
				INFOPLIST_KEY_UIInterfaceOrientation_iPhone = [item.Options.UIInterfaceOrientation_iPhone];
				INFOPLIST_KEY_UIInterfaceOrientation_iPad = [item.Options.UIInterfaceOrientation_iPad];
				INFOPLIST_KEY_UISupportedInterfaceOrientations = [item.Options.UISupportedInterfaceOrientations];
				INFOPLIST_KEY_UISupportedInterfaceOrientations_iPad = [item.Options.UISupportedInterfaceOrientations_iPad];
				INFOPLIST_KEY_UISupportedInterfaceOrientations_iPhone = [item.Options.UISupportedInterfaceOrientations_iPhone];
				INFOPLIST_KEY_UIUserInterfaceStyle = [item.Options.UIUserInterfaceStyle];
				INFOPLIST_KEY_UIWhitePointAdaptivityStyle = [item.Options.UIWhitePointAdaptivityStyle];
				INFOPLIST_KEY_UIRequiresFullScreen = [item.Options.UIRequiresFullScreen];
				INFOPLIST_KEY_UIStatusBarHidden = [item.Options.UIStatusBarHidden];
				INFOPLIST_KEY_UIViewControllerBasedStatusBarAppearance = [item.Options.UIViewControllerBasedStatusBarAppearance];
				INFOPLIST_KEY_UIStatusBarStyle = [item.Options.UIStatusBarStyle];
				INFOPLIST_KEY_UIApplicationSupportsIndirectInputEvents = [item.Options.UIApplicationSupportsIndirectInputEvents];
				INFOPLIST_KEY_UIRequiresPersistentWiFi = [item.Options.UIRequiresPersistentWiFi];
				INFOPLIST_KEY_UIAppSupportsHDR = [item.Options.UIAppSupportsHDR];
			};
			name = [item.Options.TargetName];
		};
"               },

                { ItemSection.XCBuildConfiguration_LegacyTarget,
@"		[item.Uid] /* Legacy Target - [item.Optimization] */ = {
			isa = XCBuildConfiguration;
			buildSettings = {
				FASTBUILD_TARGET = ""[item.Options.FastBuildTarget]"";
				MACH_O_TYPE = ""[item.Options.MachOType]"";
				ONLY_ACTIVE_ARCH = YES;
			};
			name = [item.Options.TargetName];
		};
"               },

                { ItemSection.XCBuildConfiguration_UnitTestTarget,
@"		[item.Uid] /* UnitTest Target - [item.Optimization] */ = {
			isa = XCBuildConfiguration;
			buildSettings = {
				BUNDLE_LOADER = ""$(TEST_HOST)"";
				CODE_SIGN_IDENTITY = ""[item.Options.CodeSigningIdentity]"";
				""CODE_SIGN_IDENTITY[sdk=iphoneos*]"" = ""[item.Options.CodeSigningIdentity]"";
				CONFIGURATION_BUILD_DIR = ""[item.Options.BuildDirectory]"";
				DEVELOPMENT_TEAM = [item.Options.DevelopmentTeam];
				EXCLUDED_SOURCE_FILE_NAMES = [ExcludedSourceFileNames];
				FRAMEWORK_SEARCH_PATHS = (
[item.Options.FrameworkPaths]
				);
				GCC_DYNAMIC_NO_PIC = [item.Options.DynamicNoPic];
				GCC_ENABLE_CPP_RTTI = [item.Options.RuntimeTypeInfo];
				GCC_SYMBOLS_PRIVATE_EXTERN = [item.Options.PrivateSymbols];
				HEADER_SEARCH_PATHS = [item.Options.IncludePaths];
				INFOPLIST_FILE = ""[item.Options.UnitTestInfoPListFile]"";
				IPHONEOS_DEPLOYMENT_TARGET = ""[item.Options.IPhoneOSDeploymentTarget]"";
				LIBRARY_SEARCH_PATHS = [item.Options.LibraryPaths];
				OTHER_LDFLAGS = -ObjC;
				PRODUCT_NAME = ""[item.Target.Identifier]"";
				PRODUCT_BUNDLE_IDENTIFIER = ""[item.Options.ProductBundleIdentifier].unittest"";
				SYMROOT = ""[SymRoot]"";
				TARGETED_DEVICE_FAMILY = ""[item.Options.TargetedDeviceFamily]"";
				TEST_HOST = ""[testHost]"";
				WRAPPER_EXTENSION = xctest;
		};
			name = [item.Options.TargetName];
		};
"               },

                { ItemSection.XCBuildConfiguration_Project,
@"		[item.Uid] /* Project - [item.Optimization] */ = {
			isa = XCBuildConfiguration;
			buildSettings = {
				ALWAYS_SEARCH_USER_PATHS = [item.Options.AlwaysSearchUserPaths];
				ARCHS = [item.Options.Archs];
				ASSETCATALOG_COMPILER_APPICON_NAME = [item.Options.AssetCatalogCompilerAppIconName];
				ASSETCATALOG_COMPILER_ALTERNATE_APPICON_NAMES = [item.Options.AssetCatalogCompilerAlternateAppIconNames];
				ASSETCATALOG_COMPILER_GLOBAL_ACCENT_COLOR_NAME = [item.Options.AssetCatalogCompilerGlobalAccentColorName];
				ASSETCATALOG_COMPILER_WIDGET_BACKGROUND_COLOR_NAME = [item.Options.AssetCatalogCompilerWidgetBackgroundColorName];
				ASSETCATALOG_COMPILER_INCLUDE_ALL_APPICON_ASSETS = [item.Options.AssetCatalogCompilerIncludeAllAppIconAssets];
				ASSETCATALOG_COMPILER_INCLUDE_INFOPLIST_LOCALIZATIONS = [item.Options.AssetCatalogCompilerIncludeInfoPlistLocalizations];
				ASSETCATALOG_COMPILER_LAUNCHIMAGE_NAME = [item.Options.AssetCatalogCompilerLaunchImageName];
				ASSETCATALOG_COMPILER_OPTIMIZATION = [item.Options.AssetCatalogCompilerOptimization];
				ASSETCATALOG_COMPILER_SKIP_APP_STORE_DEPLOYMENT = [item.Options.AssetCatalogCompilerSkipAppStoreDeployment];
				ASSETCATALOG_COMPILER_STANDALONE_ICON_BEHAVIOR = [item.Options.AssetCatalogCompilerStandaloneIconBehavior];
				ASSETCATALOG_NOTICES = [item.Options.AssetCatalogNotices];
				ASSETCATALOG_WARNINGS = [item.Options.AssetCatalogWarnings];
				CLANG_ANALYZER_LOCALIZABILITY_NONLOCALIZED = [item.Options.ClangAnalyzerLocalizabilityNonlocalized];
				CLANG_CXX_LANGUAGE_STANDARD = ""[item.Options.CppStandard]"";
				CLANG_CXX_LIBRARY = ""[item.Options.StdLib]"";
				CLANG_ENABLE_MODULES = [item.Options.ClangEnableModules];
				CLANG_ENABLE_OBJC_ARC = [item.Options.AutomaticReferenceCounting];
				CLANG_ENABLE_OBJC_WEAK = [item.Options.ObjCWeakReferences];
				CLANG_WARN_BLOCK_CAPTURE_AUTORELEASING = [item.Options.WarningBlockCaptureAutoReleasing];
				CLANG_WARN_BOOL_CONVERSION = [item.Options.WarningBooleanConversion];
				CLANG_WARN_COMMA = [item.Options.WarningComma];
				CLANG_WARN_CONSTANT_CONVERSION = [item.Options.WarningConstantConversion];
				CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS = [item.Options.WarningDeprecatedObjCImplementations];
				CLANG_WARN_DIRECT_OBJC_ISA_USAGE = [item.Options.WarningDirectIsaUsage];
				CLANG_WARN_EMPTY_BODY = [item.Options.WarningEmptyBody];
				CLANG_WARN_ENUM_CONVERSION = [item.Options.WarningEnumConversion];
				CLANG_WARN_INFINITE_RECURSION = [item.Options.WarningInfiniteRecursion];
				CLANG_WARN_INT_CONVERSION = [item.Options.WarningIntConversion];
				CLANG_WARN_NON_LITERAL_NULL_CONVERSION = [item.Options.WarningNonLiteralNullConversion];
				CLANG_WARN_OBJC_IMPLICIT_RETAIN_SELF = [item.Options.WarningObjCImplicitRetainSelf];
				CLANG_WARN_OBJC_LITERAL_CONVERSION = [item.Options.WarningObjCLiteralConversion];
				CLANG_WARN_OBJC_ROOT_CLASS = [item.Options.WarningRootClass];
				CLANG_WARN_RANGE_LOOP_ANALYSIS = [item.Options.WarningRangeLoopAnalysis];
				CLANG_WARN_STRICT_PROTOTYPES = [item.Options.WarningStrictPrototypes];
				CLANG_WARN_SUSPICIOUS_MOVE = [item.Options.WarningSuspiciousMove];
				CLANG_WARN_UNREACHABLE_CODE = [item.Options.WarningUnreachableCode];
				CLANG_WARN__DUPLICATE_METHOD_MATCH = [item.Options.WarningDuplicateMethodMatch];
				ENABLE_STRICT_OBJC_MSGSEND = [item.Options.StrictObjCMsgSend];
				ENABLE_TESTABILITY = [item.Options.Testability];
				EXECUTABLE_PREFIX = [item.Options.ExecutablePrefix];
				GCC_C_LANGUAGE_STANDARD = ""[item.Options.CStandard]"";
				GCC_NO_COMMON_BLOCKS = [item.Options.GccNoCommonBlocks];
				GCC_OPTIMIZATION_LEVEL = [item.Options.OptimizationLevel];
				GCC_PRECOMPILE_PREFIX_HEADER = [item.Options.UsePrecompiledHeader];
				GCC_PREFIX_HEADER = ""[item.Options.PrecompiledHeader]"";
				GCC_PREPROCESSOR_DEFINITIONS = [item.Options.PreprocessorDefinitions];
				GCC_TREAT_WARNINGS_AS_ERRORS = [item.Options.TreatWarningsAsErrors];
				GCC_WARN_64_TO_32_BIT_CONVERSION = [item.Options.Warning64To32BitConversion];
				GCC_WARN_ABOUT_RETURN_TYPE = [item.Options.WarningReturnType];
				GCC_WARN_UNDECLARED_SELECTOR = [item.Options.WarningUndeclaredSelector];
				GCC_WARN_UNINITIALIZED_AUTOS = [item.Options.WarningUniniatializedAutos];
				GCC_WARN_UNUSED_FUNCTION = [item.Options.WarningUnusedFunction];
				GCC_WARN_UNUSED_VARIABLE = [item.Options.WarningUnusedVariable];
				LD_DYLIB_INSTALL_NAME = ""[item.Options.DyLibInstallName]"";
				ONLY_ACTIVE_ARCH = [item.Options.OnlyActiveArch];
				OTHER_CPLUSPLUSFLAGS = [item.Options.CompilerOptions];
				OTHER_CFLAGS = [item.Options.CompilerOptions];
				OTHER_LDFLAGS = [item.Options.LinkerOptions];
				SDKROOT = ""[item.Options.SDKRoot]"";
				TARGETED_DEVICE_FAMILY = ""[item.Options.TargetedDeviceFamily]"";
				SWIFT_VERSION = [item.Options.SwiftVersion];
				USE_HEADERMAP = [item.Options.UseHeaderMap];
			};
			name = [item.Options.TargetName];
		};
"               },

                { ItemSection.XCConfigurationList,
@"		[item.Uid] /* Build configuration list for [item.ConfigurationType] ""[item.RelatedItem.Identifier]"" */ = {
			isa = XCConfigurationList;
			buildConfigurations = (
[itemChildren]			);
			defaultConfigurationIsVisible = 0;
			defaultConfigurationName = ""[item.DefaultConfiguration.Identifier]"";
		};
"               }
            };

            public static string CommandLineArgumentsBegin =
@"      <CommandLineArguments>
";

            public static string CommandLineArgument =
@"         <CommandLineArgument
            argument = ""[argument]""
            isEnabled = ""YES"">
         </CommandLineArgument>
";

            public static string CommandLineArgumentsEnd =
@"      </CommandLineArguments>";

            public static string EnvironmentVariablesBegin =
@"      <EnvironmentVariables>
";

            public static string EnvironmentVariablesEnd =
@"      </EnvironmentVariables>";

            public static string EnvironmentVariable =
@"         <EnvironmentVariable
            key = ""[name]""
            value = ""[value]""
            isEnabled = ""YES"">
         </EnvironmentVariable>
";

            public static string SchemeTestableReference =
@"
         <TestableReference
            skipped = ""NO"">
            <BuildableReference
               BuildableIdentifier = ""primary""
               BlueprintIdentifier = ""[item.Uid]""
               BuildableName = ""[item.OutputFile.BuildableName]""
               BlueprintName = ""[item.Identifier]""
               ReferencedContainer = ""container:[projectFile].xcodeproj"">
            </BuildableReference>
         </TestableReference>";

            /// <summary>
            /// This section is used to configure the executable to run for native projects.
            /// </summary>
            public static string SchemeRunnableNativeProject = 
@"      <BuildableProductRunnable>
          <BuildableReference
              BuildableIdentifier = ""primary""
              BlueprintIdentifier = ""[item.Uid]""
              BuildableName = ""[item.OutputFile.BuildableName]""
              BlueprintName = ""[item.Identifier]""
              ReferencedContainer = ""container:[projectFile].xcodeproj"">
          </BuildableReference>
      </BuildableProductRunnable>
";

            /// <summary>
            /// This section is used to configure the executable to run for makefile projects.
            /// </summary>
            public static string SchemeRunnableMakeFileProject =
@"      <PathRunnable
         runnableDebuggingMode = ""0""
         FilePath = ""[runnableFilePath]"">
      </PathRunnable>
";

            /// <summary>
            /// First part of schema file
            /// </summary>
            /// <remarks>
            /// Schema files have the following format:
            /// SchemeFileTemplatePart1
            /// SchemeRunnableNativeProject OR SchemeRunnableMakeFileProject
            /// SchemeFileTemplatePart2
            /// </remarks>
            public static string SchemeFileTemplatePart1 =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Scheme
   LastUpgradeVersion = ""0460""
   version = ""1.3"">
   <BuildAction
      parallelizeBuildables = ""YES""
      buildImplicitDependencies = ""[buildImplicitDependencies]"">
      <BuildActionEntries>
         <BuildActionEntry
            buildForTesting = ""YES""
            buildForRunning = ""YES""
            buildForProfiling = ""YES""
            buildForArchiving = ""YES""
            buildForAnalyzing = ""YES"">
            <BuildableReference
               BuildableIdentifier = ""primary""
               BlueprintIdentifier = ""[item.Uid]""
               BuildableName = ""[item.OutputFile.BuildableName]""
               BlueprintName = ""[item.Identifier]""
               ReferencedContainer = ""container:[projectFile].xcodeproj"">
            </BuildableReference>
         </BuildActionEntry>
      </BuildActionEntries>
   </BuildAction>
   <TestAction
      buildConfiguration = ""[DefaultTarget]""
      selectedDebuggerIdentifier = ""Xcode.DebuggerFoundation.Debugger.LLDB""
      selectedLauncherIdentifier = ""Xcode.DebuggerFoundation.Launcher.LLDB""
      shouldUseLaunchSchemeArgsEnv = ""YES"">
      <Testables>[testableElements]
      </Testables>
   </TestAction>
   <LaunchAction
      buildConfiguration = ""[DefaultTarget]""
      selectedDebuggerIdentifier = ""Xcode.DebuggerFoundation.Debugger.LLDB""
      selectedLauncherIdentifier = ""Xcode.DebuggerFoundation.Launcher.LLDB""
      launchStyle = ""0""
      customLLDBInitFile = ""[options.CustomLLDBInitFile]""
      useCustomWorkingDirectory = ""[UseCustomDir]""
      customWorkingDirectory = ""[options.CustomDirectory]""
      ignoresPersistentStateOnLaunch = ""NO""
      debugDocumentVersioning = ""YES""
      enableGPUFrameCaptureMode = ""[options.EnableGpuFrameCaptureMode]""
      enableGPUValidationMode = ""[options.MetalAPIValidation]""
      allowLocationSimulation = ""YES"">
";

            /// <summary>
            /// Secondpart of schema file
            /// </summary>
            public static string SchemeFileTemplatePart2 = 
@"[commandLineArguments]
[environmentVariables]
      <AdditionalOptions>
      </AdditionalOptions>
   </LaunchAction>
   <ProfileAction
      buildConfiguration = ""[DefaultTarget]""
      shouldUseLaunchSchemeArgsEnv = ""YES""
      savedToolIdentifier = """"
      useCustomWorkingDirectory = ""NO""
      debugDocumentVersioning = ""YES"">
   </ProfileAction>
   <AnalyzeAction
      buildConfiguration = ""[DefaultTarget]"">
   </AnalyzeAction>
   <ArchiveAction
      buildConfiguration = ""[DefaultTarget]""
      revealArchiveInOrganizer = ""YES"">
   </ArchiveAction>
</Scheme>
";
        }
    }
}
