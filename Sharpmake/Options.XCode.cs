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

                public class CodeSignEntitlements
                {
                    public string Value;
                    public CodeSignEntitlements(string value)
                    {
                        Value = value;
                    }
                }

                public class CodeSigningIdentity
                {
                    public string Value;
                    public CodeSigningIdentity(string value)
                    {
                        Value = value;
                    }
                }

                public class ProductBundleIdentifier : StringOption
                {
                    public ProductBundleIdentifier(string value) : base(value) { }
                }

                public enum CppLanguageStandard
                {
                    CPP98,
                    [Default]
                    CPP11,
                    CPP14,
                    GNU98,
                    GNU11,
                    GNU14
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

                public class ProvisioningStyle : StringOption
                {
                    public ProvisioningStyle(string value) : base(value) { }
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

                public enum Exceptions
                {
                    Disable,
                    Enable,
                    EnableCpp,
                    [Default]
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

                public enum LibraryStandard
                {
                    [Default]
                    CppStandard,
                    LibCxx
                }

                public class MacOSDeploymentTarget
                {
                    public string MinimumVersion;
                    public MacOSDeploymentTarget(string minimumVersion)
                    {
                        MinimumVersion = minimumVersion;
                    }
                }

                public enum ModelTuning
                {
                    None,
                    G3,
                    G4,
                    [Default]
                    G5,
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

                public class ProvisioningProfile
                {
                    public string ProfileName;
                    public ProvisioningProfile(string profileName)
                    {
                        ProfileName = profileName;
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

                public enum StripDebugSymbolsDuringCopy
                {
                    [Default]
                    Enable,
                    Disable
                }

                public class TargetedDeviceFamily
                {
                    public string Value;
                    public TargetedDeviceFamily(string value)
                    {
                        Value = value;
                    }
                }

                public class ValidArchs
                {
                    public string Archs;
                    public ValidArchs(params string[] archs)
                    {
                        Archs = archs.Aggregate((first, next) => first + " " + next);
                    }
                }

                public enum Warning64To32BitConversion
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum WarningBooleanConversion
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum WarningConstantConversion
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

                public enum WarningIntConversion
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
        }
    }
}