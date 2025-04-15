// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

// reference for options: https://developer.apple.com/documentation/xcode/build-settings-reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sharpmake
{
    public static partial class Options
    {
        public static class XCode
        {
            public static class Editor
            {
                public enum Indent
                {
                    [Default]
                    Tabs,
                    Spaces
                }
            };

            public static class Compiler
            {
                public enum AlwaysSearchUserPaths
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum ClangEnableModules
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum OnlyActiveArch
                {
                    Enable,
                    [Default]
                    Disable
                }
                public enum ClangAnalyzerLocalizabilityNonlocalized
                {
                    [Default]
                    Enable,
                    Disable
                }

                public class Archs
                {
                    public string Value;
                    public Archs(string value)
                    {
                        Value = value;
                    }
                }

                public enum AutomaticReferenceCounting
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum CLanguageStandard
                {
                    ANSI,
                    [Default]
                    CompilerDefault,
                    C89,
                    GNU89,
                    C99,
                    GNU99,
                    C11,
                    GNU11
                }

                public enum GenerateInfoPlist
                {
                    Enable,
                    [Default]
                    Disable
                }

                public class CodeSignEntitlements : StringOption
                {
                    public CodeSignEntitlements(string value) : base(value)
                    {
                    }
                }

                public class CodeSigningIdentity : StringOption
                {
                    public CodeSigningIdentity(string value) : base(value)
                    {
                    }
                }

                public class ProductBundleDisplayName : StringOption
                {
                    public ProductBundleDisplayName(string value) : base(value) { }
                }

                public class ProductBundleIdentifier : StringOption
                {
                    public ProductBundleIdentifier(string value) : base(value) { }
                }

                public class ProductBundleVersion : StringOption
                {
                    public ProductBundleVersion(string value) : base(value) { }
                }

                public class ProductBundleShortVersion : StringOption
                {
                    public ProductBundleShortVersion(string value) : base(value) { }
                }

                /// <summary>
                /// Resolves to `INSTALL_PATH` in Xcode build settings
                /// Allows to override the default INSTALL_PATH to set a custom Xcode macro value
                /// e.g. `@rpath/../Frameworks` for MacOS Frameworks
                /// or `@rpath` for dylibs
                /// </summary>
                public class ProductInstallPath : StringOption
                {
                    public ProductInstallPath(string value) : base(value) { }
                }

                /// <summary>
                /// Resolves to `LD_RUNPATH_SEARCH_PATHS` in Xcode build settings
                /// Allows to set paths for LD_RUNPATH_SEARCH_PATHS to set a custom Xcode macro value
                /// </summary>
                public class LdRunPaths : Strings
                {
                    public LdRunPaths(params string[] paths)
                        : base(paths)
                    { }
                }


                public enum EnableGpuFrameCaptureMode
                {
                    [Default]
                    AutomaticallyEnable,
                    MetalOnly,
                    OpenGLOnly,
                    Disable
                }

                public enum CppLanguageStandard
                {
                    CPP98,
                    [Default]
                    CPP11,
                    CPP14,
                    CPP17,
                    CPP20,
                    GNU98,
                    GNU11,
                    GNU14,
                    GNU17,
                    GNU20
                }

                /// <summary>
                /// Add buildSettings options as needed by Xcode project with swift code
                /// 4_0, 4_2, 5_0 correspond to 4.0, 4.2, 5.0, the 3 options in Xcode 15 by Aug 2023, add more when needed
                /// </summary>
                public enum SwiftLanguageVersion
                {
                    [Default]
                    Disable,
                    SWIFT4_0,
                    SWIFT4_2,
                    SWIFT5_0,
                    SWIFT6_0
                }

                public class SwiftModuleName : StringOption
                {
                    public SwiftModuleName(string value) : base(value) { }
                    public static readonly string Default = "[project.LowerName]";
                }

                public class SwiftAdditionalCompilerOptions : OrderableStrings
                {
                }

                public enum DeadStrip
                {
                    Disable,
                    [Default]
                    Code,
                    Inline,
                    All
                }

                public enum DebugInformationFormat
                {
                    Stabs,
                    Dwarf,
                    [Default]
                    DwarfWithDSym
                }

                public class DevelopmentTeam : StringOption
                {
                    public DevelopmentTeam(string value) : base(value) { }
                }

                public enum ProvisioningStyle
                {
                    [Default]
                    Automatic,
                    Manual
                }

                public enum DeploymentPostProcessing
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum DynamicNoPic
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum EnableBitcode
                {
                    [Default]
                    Enable,
                    Disable
                }

                [Obsolete("Deprecated. Use `CppExceptions`, `ObjCExceptions`, or `ObjCARCExceptions` instead.", error: false)]
                public enum Exceptions
                {
                    [Default]
                    Disable,
                    Enable,
                    EnableCpp,
                    EnableObjC,
                }

                public enum CppExceptions
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum ObjCExceptions
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum ObjCARCExceptions
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum AsyncExceptions
                {
                    [Default]
                    Disable,
                    Enable
                }
                public class ExternalResourceFolders : Strings
                {
                    public ExternalResourceFolders(params string[] paths)
                        : base(paths)
                    { }
                }

                public class ExternalResourceFiles : Strings
                {
                    public ExternalResourceFiles(params string[] paths)
                        : base(paths)
                    { }
                }

                public class ExternalResourcePackages : Strings
                {
                    public ExternalResourcePackages(params string[] paths)
                        : base(paths)
                    { }
                }

                public abstract class Frameworks : Strings
                {
                    protected Frameworks(params string[] paths)
                        : base(paths)
                    { }
                }

                [Obsolete("Deprecated. Use `conf.XcodeSystemFrameworks` instead.", error: true)]
                public class SystemFrameworks : Frameworks
                {
                    public SystemFrameworks(params string[] frameworkNames)
                        : base(frameworkNames)
                    { }
                }

                [Obsolete("Deprecated. Use `conf.XcodeUserFrameworks` instead.", error: true)]
                public class UserFrameworks : Frameworks
                {
                    public UserFrameworks(params string[] paths)
                        : base(paths)
                    { }
                }

                [Obsolete("Deprecated. Use `conf.XcodeFrameworkPaths` instead.", error: true)]
                public class FrameworkPaths : Strings
                {
                    public FrameworkPaths(params string[] paths)
                        : base(paths)
                    { }
                }

                public enum GenerateDebuggingSymbols
                {
                    Enable,
                    [Default]
                    DeadStrip,
                    Disable
                }

                public class InfoPListFile
                {
                    public string Value;
                    public InfoPListFile(string value)
                    {
                        Value = value;
                    }
                }

                public class UnitTestInfoPListFile
                {
                    public string Value;
                    public UnitTestInfoPListFile(string value)
                    {
                        Value = value;
                    }
                }

                public enum ICloud
                {
                    [Default]
                    Enable,
                    Disable
                }

                public class IPhoneOSDeploymentTarget
                {
                    public string MinimumVersion;
                    public IPhoneOSDeploymentTarget(string minimumVersion)
                    {
                        MinimumVersion = minimumVersion;
                    }
                }

                public class TvOSDeploymentTarget
                {
                    public string MinimumVersion;
                    public TvOSDeploymentTarget(string minimumVersion)
                    {
                        MinimumVersion = minimumVersion;
                    }
                }

                public class WatchOSDeploymentTarget
                {
                    public string MinimumVersion;
                    public WatchOSDeploymentTarget(string minimumVersion)
                    {
                        MinimumVersion = minimumVersion;
                    }
                }

                public class MacOSDeploymentTarget
                {
                    public string MinimumVersion;
                    public MacOSDeploymentTarget(string minimumVersion)
                    {
                        MinimumVersion = minimumVersion;
                    }
                }

                public enum LibraryStandard
                {
                    CppStandard,
                    [Default]
                    LibCxx
                }

                public enum ModelTuning
                {
                    None,
                    G3,
                    G4,
                    [Default]
                    G5,
                }

                public enum GccNoCommonBlocks
                {
                    [Default]
                    Enable,
                    Disable
                }

                // Optimization
                public enum OptimizationLevel
                {
                    [Default(DefaultTarget.Debug)]
                    Disable,
                    Fast,
                    Faster,
                    Fastest,
                    [Default(DefaultTarget.Release)]
                    Smallest,
                    Aggressive,
                    AggressiveSize
                }

                public enum PreserveDeadCodeInitsAndTerms
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum PrivateSymbols
                {
                    [Default]
                    Disable,
                    Enable
                }

                public class ProvisioningProfile : StringOption
                {
                    public ProvisioningProfile(string profileName) : base(profileName)
                    {
                    }
                }

                public enum RTTI
                {
                    [Default]
                    Disable,
                    Enable
                }

                [Obsolete("Deprecated and Ignored. Use `ApplePlatform.Settings.MacOSSDKPath` instead.", error: true)]
                public class SDKRoot
                {
                    public string Value;
                    public SDKRoot(string value)
                    {
                        Value = value;
                    }
                }

                public enum SkipInstall
                {
                    Enable,
                    [Default]
                    Disable
                }

                public class SpecificDeviceLibraryPaths : Strings
                {
                    public SpecificDeviceLibraryPaths(params string[] paths)
                        : base(paths)
                    { }
                }

                public class SpecificSimulatorLibraryPaths : Strings
                {
                    public SpecificSimulatorLibraryPaths(params string[] paths)
                        : base(paths)
                    { }
                }

                public enum StrictObjCMsgSend
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum StripDebugSymbolsDuringCopy
                {
                    [Default]
                    Enable,
                    Disable
                }

                [Flags]
                public enum TargetedDeviceFamily
                {
                    [Default]
                    Ios = 1 << 0,
                    Ipad = 1 << 1,
                    Tvos = 1 << 2,
                    Watchos = 1 << 3,

                    IosAndIpad = Ios | Ipad,
                    MacCatalyst = Ipad,
                }

                public class AssetCatalogCompilerAppIconName : StringOption
                {
                    public AssetCatalogCompilerAppIconName(string value) : base(value)
                    {
                    }
                }
                public class AssetCatalogCompilerLaunchImageName : StringOption
                {
                    public AssetCatalogCompilerLaunchImageName(string value) : base(value)
                    {
                    }
                }

                public class AssetCatalogCompilerAlternateAppIconNames : Strings
                {
                    public AssetCatalogCompilerAlternateAppIconNames(params string[] values)
                        : base(values)
                    {
                    }
                }

                public class AssetCatalogCompilerGlobalAccentColorName : StringOption
                {
                    public AssetCatalogCompilerGlobalAccentColorName(string value) : base(value)
                    {
                    }
                }

                public class AssetCatalogCompilerWidgetBackgroundColorName : StringOption
                {
                    public AssetCatalogCompilerWidgetBackgroundColorName(string value) : base(value)
                    {
                    }
                }

                public enum AssetCatalogCompilerIncludeAllAppIconAssets
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum AssetCatalogCompilerIncludeInfoPlistLocalizations
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum AssetCatalogCompilerOptimization
                {
                    Time,
                    [Default]
                    Space
                }

                public enum AssetCatalogCompilerStandaloneIconBehavior
                {
                    [Default]
                    Default,
                    None,
                    All
                }

                public enum AssetCatalogCompilerSkipAppStoreDeployment
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum AssetCatalogNotices
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum AssetCatalogWarnings
                {
                    [Default]
                    Disable,
                    Enable
                }

                /// <summary>
                /// Testability is used for automated tests in XCode.
                /// Activating it will disable other options, like stripping and private symbols.
                /// See https://developer.apple.com/documentation/xcode/build-settings-reference#Enable-Testability
                /// </summary>
                public enum Testability
                {
                    Enable,
                    [Default]
                    Disable
                }

                public class ValidArchs
                {
                    public string Archs;
                    public ValidArchs(params string[] archs)
                    {
                        Archs = archs.Aggregate((first, next) => first + " " + next);
                    }
                }

                public enum ObjCWeakReferences
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum Warning64To32BitConversion
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum WarningBlockCaptureAutoReleasing
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningBooleanConversion
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum WarningComma
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningConstantConversion
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningDeprecatedObjCImplementations
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningDuplicateMethodMatch
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningEmptyBody
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningEnumConversion
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningDirectIsaUsage
                {
                    Enable,
                    EnableAndError,
                    [Default]
                    Disable
                }

                public enum WarningInfiniteRecursion
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningIntConversion
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningNonLiteralNullConversion
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningObjCImplicitRetainSelf
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningObjCLiteralConversion
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningRangeLoopAnalysis
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningReturnType
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum WarningRootClass
                {
                    Enable,
                    EnableAndError,
                    [Default]
                    Disable
                }

                public enum WarningStrictPrototypes
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningSuspiciousMove
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningUndeclaredSelector
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum WarningUniniatializedAutos
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum WarningUnreachableCode
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum WarningUnusedFunction
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum WarningUnusedVariable
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum TreatWarningsAsErrors
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum SupportsMaccatalyst
                {
                    Enable,
                    Disable
                }

                public enum SupportsMacDesignedForIphoneIpad
                {
                    Enable,
                    Disable
                }

                public enum SwiftEmitLocStrings
                {
                    Disable,
                    Enable
                }

                public enum MetalFastMath
                {
                    Disable,
                    [Default]
                    Enable
                }

                public enum UseHeaderMap
                {
                    [Default]
                    Disable,
                    Enable
                }
            }

            /// <summary>
            /// This exposes the Info Propery List (Info.plist) settings to Sharpmake
            /// These settings have their own group for better grouping
            /// https://developer.apple.com/documentation/bundleresources?language=objc
            /// Option class naming follows their equivalent key.
            /// </summary>
            public static class InfoPlist
            {
                public class CFBundleSpokenName : StringOption
                {
                    public CFBundleSpokenName(string value)
                        : base(value) { }
                }

                public class CFBundleDevelopmentRegion : StringOption
                {
                    public CFBundleDevelopmentRegion(string value)
                        : base(value) { }
                }

                public class CFBundleExecutable : StringOption
                {
                    public CFBundleExecutable(string value)
                        : base(value) { }
                }

                public class CFBundleLocalizations : Strings
                {
                    public CFBundleLocalizations(params string[] values)
                        : base(values) { }
                }

                public enum NSHighResolutionCapable
                {
                    Disable,
                    Enable
                }

                public enum CFBundleAllowMixedLocalizations
                {
                    Disable,
                    Enable
                }

                /// --- macOS specific settings ---
                public class NSHumanReadableCopyright : StringOption
                {
                    public NSHumanReadableCopyright(string value)
                        : base(value) { }
                }

                public class NSMainStoryboardFile : StringOption
                {
                    public NSMainStoryboardFile(string value)
                        : base(value) { }
                }

                public class NSMainNibFile : StringOption
                {
                    public NSMainNibFile(string value)
                        : base(value) { }
                }

                public class NSPrefPaneIconFile : StringOption
                {
                    public NSPrefPaneIconFile(string value)
                        : base(value) { }
                }

                public class NSPrefPaneIconLabel : StringOption
                {
                    public NSPrefPaneIconLabel(string value)
                        : base(value) { }
                }

                public class NSPrincipalClass : StringOption
                {
                    public NSPrincipalClass(string value)
                        : base(value) { }
                }

                public enum LSRequiresNativeExecution
                {
                    Disable,
                    Enable
                }

                public enum LSMultipleInstancesProhibited
                {
                    Disable,
                    Enable
                }

                public enum NSSupportsAutomaticGraphicsSwitching
                {
                    Disable,
                    Enable
                }

                public enum NSPrefersDisplaySafeAreaCompatibilityMode
                {
                    Disable,
                    Enable
                }

                public enum UISupportsTrueScreenSizeOnMac
                {
                    Disable,
                    Enable
                }

                /// --- iOS specific settings ---
                public enum LSRequiresIPhoneOS
                {
                    Disable,
                    Enable
                }

                public class UIRequiredDeviceCapabilities : Strings
                {
                    public UIRequiredDeviceCapabilities(params string[] paths)
                        : base(paths)
                    {
                    }
                }

                public class UIMainStoryboardFile : StringOption
                {
                    public UIMainStoryboardFile(string value)
                        : base(value) { }
                }

                public class UILaunchStoryboardName : StringOption
                {
                    public UILaunchStoryboardName(string value)
                        : base(value) { }
                }

                public class CFBundleIconFile : StringOption
                {
                    public CFBundleIconFile(string value)
                        : base(value) { }
                }

                public class CFBundleIconFiles : Strings
                {
                    public CFBundleIconFiles(params string[] paths)
                        : base(paths)
                    {
                    }
                }

                public class CFBundleIconName : StringOption
                {
                    public CFBundleIconName(string value)
                        : base(value) { }
                }

                public enum UIPrerenderedIcon
                {
                    Disable,
                    Enable
                }

                public enum UIInterfaceOrientation
                {
                    UIInterfaceOrientationPortrait,
                    UIInterfaceOrientationPortraitUpsideDown,
                    UIInterfaceOrientationLandscapeLeft,
                    UIInterfaceOrientationLandscapeRight,
                }

                public class UIInterfaceOrientation_iPhone : WithArgOption<UIInterfaceOrientation>
                {
                    public UIInterfaceOrientation_iPhone(UIInterfaceOrientation value)
                       : base(value) { }
                }

                public class UIInterfaceOrientation_iPad : WithArgOption<UIInterfaceOrientation>
                {
                    public UIInterfaceOrientation_iPad(UIInterfaceOrientation value)
                       : base(value) { }
                }

                public class UISupportedInterfaceOrientations
                {
                    private UIInterfaceOrientation[] _uIInterfaceOrientations;
                    public UISupportedInterfaceOrientations(params UIInterfaceOrientation[] values)
                    {
                        _uIInterfaceOrientations = values;
                    }

                    public override string ToString()
                    {
                        return "\"" + string.Join(" ", _uIInterfaceOrientations) + "\"";
                    }
                }

                public class UISupportedInterfaceOrientations_iPhone : UISupportedInterfaceOrientations
                {
                    public UISupportedInterfaceOrientations_iPhone(params UIInterfaceOrientation[] values)
                        : base(values) { }

                }

                public class UISupportedInterfaceOrientations_iPad : UISupportedInterfaceOrientations
                {
                    public UISupportedInterfaceOrientations_iPad(params UIInterfaceOrientation[] values)
                        : base(values) { }

                }

                public enum UIUserInterfaceStyle
                {
                    Automatic,
                    Light,
                    Dark,
                }

                public enum UIWhitePointAdaptivityStyle
                {
                    UIWhitePointAdaptivityStyleStandard,
                    UIWhitePointAdaptivityStyleReading,
                    UIWhitePointAdaptivityStylePhoto,
                    UIWhitePointAdaptivityStyleVideo,
                    UIWhitePointAdaptivityStyleGame,
                }

                public enum UIRequiresFullScreen
                {
                    Disable,
                    Enable
                }

                public enum UIStatusBarHidden
                {
                    Disable,
                    Enable
                }

                public enum UIViewControllerBasedStatusBarAppearance
                {
                    Disable,
                    Enable
                }

                public enum UIStatusBarStyle
                {
                    UIStatusBarStyleDefault,
                    UIStatusBarStyleLightContent,
                    UIStatusBarStyleDarkContent,
                }

                public enum UIApplicationSupportsIndirectInputEvents
                {
                    Disable,
                    Enable
                }

                public enum UIRequiresPersistentWiFi
                {
                    Disable,
                    Enable
                }

                /// --- tvOS specific settings ---
                public enum UIAppSupportsHDR
                {
                    Disable,
                    Enable
                }

            }

            public static class Linker
            {
                /// <summary>
                /// Enables symbols stripping after the binary is linked.
                /// </summary>
                public enum StripLinkedProduct
                {
                    [Default]
                    Disable,
                    Enable
                }

                /// <summary>
                /// When stripping is enabled.
                /// The level of symbol stripping to be performed on the linked product of the build.
                /// </summary>
                public enum StripStyle
                {
                    AllSymbols,           // Completely strips the binary, removing the symbol table and relocation information (strip option -s)
                    NonGlobalSymbols,     // Strips non-global symbols, but saves external symbols (strip option -x)
                    [Default]
                    DebuggingSymbolsOnly  // Strips debugging symbols, but saves local and global symbols (strip option -S)
                }

                /// <summary>
                /// When stripping is enabled.
                /// Adjust the level of symbol stripping so that when the linked product of the build is stripped, all Swift symbols will be removed.
                /// </summary>
                public enum StripSwiftSymbols
                {
                    [Default]
                    Disable,
                    Enable
                }

                /// <summary>
                /// Additional Strip Flags.
                /// For a complete list, see documentation for /usr/bin/strip.
                /// </summary>
                public class AdditionalStripFlags : StringOption
                {
                    public AdditionalStripFlags(string value) : base(value)
                    {
                    }
                }

                /// <summary>
                /// Xcode has a setting called Single-Object Prelink, which allows libraries and frameworks to include the necessary symbols 
                /// from other libraries so that the underlying libraries do not need to be linked against in an application using your framework.
                /// </summary>
                public enum PerformSingleObjectPrelink
                {
                    [Default]
                    Disable,
                    Enable
                }

                /// <summary>
                /// List of libraries that need to be included into Single-Object Prelink process.
                /// Use space separator to include multiple libraries.
                /// </summary>
                public class PrelinkLibraries : PathOption
                {
                    public PrelinkLibraries(string path)
                       : base(path)
                    {
                    }
                }

                /// <summary>
                /// When enable, "-ObjC" will be added into LinkerOptions, then linker Loads all members of static archive libraries that implement an Objective-C class or category.
                /// </summary>
                public enum LinkObjC
                {
                    Disable,
                    [Default]
                    Enable
                }

                /// <summary>
                /// Sets an internal install path (LC_ID_DYLIB) in a dynamic library. Any clients linked against the library will record that path as the way dyld should locate this library.
                /// </summary>
                public class DyLibInstallName : StringOption
                {
                    public DyLibInstallName(string value) : base(value)
                    {
                    }
                }

            }

            /// <summary>
            /// This exposes the scheme settings to Sharpmake
            /// These settings have their own group for better grouping
            /// https://developer.apple.com/documentation/xcode/customizing-the-build-schemes-for-a-project
            /// Option class naming follows their equivalent key.
            /// </summary>
            public static class Scheme
            {
                public enum MetalAPIValidation
                {
                    [Default]
                    Enable,
                    Disable
                }              

                /// <summary>
                /// Resolve to <CommandLineArguments> in xcscheme, 
                ///  for configuring 'Arguments Passed On Launch' as 'Edit Scheme' in XCode can do
                /// </summary>
                public class DebugArguments: List<string>
                {
                    public DebugArguments(List<string> args)
                        : base(args) 
                    {
                    }
                }

                public class EnvironmentVariables
                {
                    public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
                }

                /// <summary>
                /// This option can be used to set a custom runnable path in the scheme file for fastbuild targets
                /// </summary>
                public class CustomRunnablePath : PathOption
                {
                    public CustomRunnablePath(string path) : base(path)
                    {
                    }
                }

                /// <summary>
                /// This option can be used to set a custom working directory
                /// </summary>
                public class CustomWorkingDirectory : PathOption
                {
                    public CustomWorkingDirectory(string path) : base(path)
                    {
                    }
                }

                /// <summary>
                /// This option can be used to set a custom working directory
                /// </summary>
                public class CustomLLDBInitFile : PathOption
                {
                    public CustomLLDBInitFile(string path) : base(path)
                    {
                    }
                }
            }
        }
    }
}
