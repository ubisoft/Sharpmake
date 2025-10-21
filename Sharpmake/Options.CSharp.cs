// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Linq;

namespace Sharpmake
{
    public static partial class Options
    {
        public static class CSharp
        {
            public enum DefaultConfiguration
            {
                [Default]
                Debug,
                Release,
                Other
            }

            public enum FileAlignment
            {
                None,
                [Default]
                Value512,
                Value1024,
                Value2048,
                Value4096,
                Value8192,
            }

            public enum RollForward
            {
                [Default]
                Minor,
                Major,
                LatestPatch,
                LatestMinor,
                LatestMajor,
                Disable,
            }

            public enum CreateVsixContainer
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum IncludeAssemblyInVSIXContainer
            {
                [Default]
                Enabled,
                Disabled
            }

            public enum CopyVsixExtensionFiles
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum GeneratePkgDefFile
            {
                [Default]
                None,

                Enabled,
                Disabled,
            }

            public enum DeployExtension
            {
                [Default]
                Enabled,
                Disabled
            }

            public enum DebugType
            {
                [Default(DefaultTarget.Debug)]
                Full,
                [Default(DefaultTarget.Release)]
                Pdbonly,
                None,
                Portable,
                Embedded,
            }

            public enum ErrorReport
            {
                Prompt,
                Queue,
                [Default]
                None
            }

            public enum Install
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum InstallFrom
            {
                Web,
                Disk,
                Unc,
                [Default]
                None
            }

            public enum SignAssembly
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum UpdateEnabled
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum UpdateMode
            {
                Foreground,
                [Default]
                Other
            }

            public enum UpdateIntervalUnits
            {
                Days,
                [Default]
                None
            }

            public enum UpdatePeriodically
            {
                [Default]
                Enabled,
                Disabled
            }

            public enum UpdateRequired
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum DllBaseAddress
            {
                x11000000,
                x12000000,
                [Default]
                None
            }

            public enum CopyOutputSymbolsToOutputDirectory
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum RunPostBuildEvent
            {
                Always,
                [Default]
                OnBuildSuccess,
                OnOutputUpdated
            }

            public enum ProduceReferenceAssembly
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum UseWpf
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum UseWindowsForms
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum Nullable
            {
                [Default]
                Disabled,
                Enabled
            }

            public enum PublishSingleFile
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum PublishTrimmed
            {
                Enabled,
                [Default]
                Disabled
            }
            public enum PublishAot
            {
                Enabled,
                [Default]
                Disabled
            }


            public class UpdateInterval : IntOption
            {
                public UpdateInterval(int interval)
                    : base(interval)
                { }
            }

            public class ApplicationRevision : StringOption
            {
                public ApplicationRevision(string revision) : base(revision) { }
            }

            public class ApplicationVersion : StringOption
            {
                public ApplicationVersion(string version) : base(version) { }
            }

            public class AssemblyOriginatorKeyFile : PathOption
            {
                public AssemblyOriginatorKeyFile(string keyFile) : base(keyFile) { }
            }

            public class MinimumVisualStudioVersion : StringOption
            {
                public MinimumVisualStudioVersion(string version) : base(version) { }
            }

            public class OldToolsVersion : StringOption
            {
                public OldToolsVersion(string version) : base(version) { }
            }

            public class VsToolsPath : StringOption
            {
                public VsToolsPath(string toolPath) : base(toolPath) { }
            }

            public class VisualStudioVersion : StringOption
            {
                public VisualStudioVersion(string version) : base(version) { }
            }

            public class ConcordSDKDir : StringOption
            {
                public ConcordSDKDir(string dir) : base(dir) { }
            }

            public class PublishURL : StringOption
            {
                public PublishURL(string url) : base(url) { }
            }

            public class ManifestKeyFile : StringOption
            {
                public ManifestKeyFile(string url) : base(url) { }
            }

            public class ManifestCertificateThumbprint : StringOption
            {
                public ManifestCertificateThumbprint(string url) : base(url) { }
            }

            public class InstallURL : StringOption
            {
                public InstallURL(string url) : base(url) { }
            }

            public class SupportUrl : StringOption
            {
                public SupportUrl(string url) : base(url) { }
            }

            public class ProductName : StringOption
            {
                public ProductName(string name) : base(name) { }
            }

            public class PublisherName : StringOption
            {
                public PublisherName(string name) : base(name) { }
            }

            public class WebPage : StringOption
            {
                public WebPage(string page) : base(page) { }
            }

            public class BootstrapperComponentsUrl : StringOption
            {
                public BootstrapperComponentsUrl(string url) : base(url) { }
            }

            public class MinimumRequiredVersion : StringOption
            {
                public MinimumRequiredVersion(string url) : base(url) { }
            }

            /// <summary>
            /// Suppressed specific warnings in a C# project.
            /// </summary>
            /// <remarks>
            /// This option generates a `NoWarn` element in the C# project XML.
            /// </remarks>
            public class SuppressWarning : StringOption
            {
                public SuppressWarning(params int[] warnings) : base(string.Join(",", warnings.Select(w => w.ToString()))) { }

                /// <summary>
                /// Creates a new <see cref="SuppressWarning"/> instance from a list of warning
                /// code labels.
                /// </summary>
                /// <param name="warnings">The list of warning code labels to suppress. See remarks.</param>
                /// <remarks>
                /// If <paramref name="warnings"/> contains elements that are not C# compiler
                /// warnings, those warning numbers *must* include the 2-letter prefix. For
                /// example, NuGet warnings must be prefixed by `NU`. (ie: `NU1603`)
                /// </remarks>
                public SuppressWarning(params string[] warnings) : base(string.Join(",", warnings)) { }
            }

            /// <summary>
            /// Prevent specific warnings from being treated as errors when <see cref="TreatWarningsAsErrors"/> is enabled
            /// </summary>
            /// <remarks>
            /// This option generates a `WarningsNotAsErrors` element in the C# project XML.
            /// </remarks>
            public class WarningsNotAsErrors : StringOption
            {
                public WarningsNotAsErrors(params int[] warnings) : base(string.Join(",", warnings.Select(w => w.ToString(System.Globalization.CultureInfo.InvariantCulture)))) { }

                /// <summary>
                /// Creates a new <see cref="WarningsNotAsErrors"/> instance from a list of warning
                /// code labels.
                /// </summary>
                /// <param name="warnings">The list of warning code labels to avoid treating as errors. See remarks.</param>
                /// <remarks>
                /// If <paramref name="warnings"/> contains elements that are not C# compiler
                /// warnings, those warning numbers *must* include the 2-letter prefix. For
                /// example, NuGet warnings must be prefixed by `NU`. (ie: `NU1603`)
                /// </remarks>
                public WarningsNotAsErrors(params string[] warnings) : base(string.Join(",", warnings)) { }
            }

            /// <summary>
            /// Treat specific warnings as errors.
            /// </summary>
            /// <remarks>
            /// This option generates a `WarningsAsErrors` element in the C# project XML.
            /// </remarks>
            public class WarningsAsErrors : StringOption
            {
                public WarningsAsErrors(params int[] warnings) : base(string.Join(",",
                    warnings.Select(w => w.ToString(System.Globalization.CultureInfo.InvariantCulture))))
                {
                }

                /// <summary>
                /// Creates a new <see cref="WarningsAsErrors"/> instance from a list of warning
                /// code labels.
                /// </summary>
                /// <param name="warnings">The list of warning code labels to treat as errors.</param>
                public WarningsAsErrors(params string[] warnings) : base(string.Join(",", warnings)) { }
            }

            public class CopyVsixExtensionLocation : StringOption
            {
                public CopyVsixExtensionLocation(string location) : base(location) { }
            }

            public class ProductVersion : StringOption
            {
                public ProductVersion(string versionString) : base(versionString) { }
            }

            public class FileVersion : StringOption
            {
                public FileVersion(string versionString) : base(versionString) { }
            }

            public class Version : StringOption
            {
                public Version(string versionString) : base(versionString) { }
            }

            public class Product : StringOption
            {
                public Product(string product) : base(product) { }
            }

            public class Copyright : StringOption
            {
                public Copyright(string copyright) : base(copyright) { }
            }

            public enum MapFileExtensions
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum IsWebBootstrapper
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum PublishWizardCompleted
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum OpenBrowserOnPublish
            {
                [Default]
                Enabled,
                Disabled
            }

            public enum CreateWebPageOnPublish
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum CreateDesktopShortcut
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum UseCodeBase
            {
                Enabled,
                [Default]
                Disabled
            }

            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version#c-language-version-reference
            public enum LanguageVersion
            {
                [Default]
                LatestMajorVersion, // The compiler accepts syntax from the latest released major version of the compiler.
                LatestMinorVersion, // The compiler accepts syntax from the latest released version of the compiler (including minor version).
                Preview, // The compiler accepts all valid language syntax from the latest preview version.
                ISO1, // The compiler accepts only syntax that is included in ISO/IEC 23270:2003 C# (1.0/1.2).
                ISO2, // The compiler accepts only syntax that is included in ISO/IEC 23270:2006 C# (2.0).
                CSharp3,
                CSharp4,
                CSharp5,
                CSharp6,
                CSharp7,
                CSharp7_1,
                CSharp7_2,
                CSharp7_3,
                CSharp8,
                CSharp9,
                CSharp10,
                CSharp11,
                CSharp12,
                CSharp13,
            }

            // Disable warning MSB3270 when disabled
            public enum ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch
            {
                Disabled,
                [Default]
                Enabled
            }

            public enum WarningLevel
            {
                Level0,
                Level1,
                Level2,
                Level3,
                [Default]
                Level4,
                Level5
            }

            public enum DebugSymbols
            {
                [Default(DefaultTarget.Debug)]
                Enabled,
                [Default(DefaultTarget.Release)]
                Disabled
            }

            public enum Optimize
            {
                [Default(DefaultTarget.Release)]
                Enabled,
                [Default(DefaultTarget.Debug)]
                Disabled
            }

            public enum AllowUnsafeBlocks
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum Prefer32Bit
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum DisableFastUpToDateCheck
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum TreatWarningsAsErrors
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum UseVSHostingProcess
            {
                [Default]
                Enabled,
                Disabled
            }

            public enum GenerateManifests
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum SignManifests
            {
                [Default]
                Enabled,
                Disabled
            }

            public enum UseApplicationTrust
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum BootstrapperEnabled
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum RegisterOutputPackage
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum RegisterWithCodebase
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum AutoGenerateBindingRedirects
            {
                Enabled,
                [Default]
                Disabled
            }

            public enum GenerateBindingRedirectsOutputType
            {
                Enabled,
                [Default]
                Disabled
            }

            /// <summary>
            /// Exclude from SonarQube C# static analysis
            /// </summary>
            public enum SonarQubeExclude
            {
                [Default]
                Disabled,
                Enabled
            }

            /// <summary>
            /// Controls whether the project is published when running a publish command
            /// Only affects processes that use the Publish target, such as the dotnet sdk projects
            /// </summary>
            public enum IsPublishable
            {
                Disabled,
                [Default]
                Enabled
            }
        }
    }
}
