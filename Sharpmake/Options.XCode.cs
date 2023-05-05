// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Linq;

namespace Sharpmake
{
    public static partial class Options
    {
        public static class XCode
        {
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

                public enum Exceptions
                {
                    [Default]
                    Disable,
                    Enable,
                    EnableCpp,
                    EnableObjC,
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

                public class SystemFrameworks : Frameworks
                {
                    public SystemFrameworks(params string[] frameworkNames)
                        : base(frameworkNames)
                    { }
                }

                public class UserFrameworks : Frameworks
                {
                    public UserFrameworks(params string[] paths)
                        : base(paths)
                    { }
                }

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

                public enum Testability
                {
                    [Default]
                    Enable,
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
            }

            public static class Linker
            {
                public enum StripLinkedProduct
                {
                    Disable,
                    [Default]
                    Enable
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
            }
        }
    }
}
