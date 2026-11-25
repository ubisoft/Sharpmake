// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;

namespace Sharpmake
{
    /// <summary>
    /// Per-level naming data for <see cref="InstructionSetLevel"/>. The validation rules of a level are in <see cref="Util"/>.
    /// </summary>
    internal static class InstructionSetLevelInfo
    {
        /// <summary>
        /// The token a level adds to platform strings (paths) and configuration names, e.g. "APX".
        /// Empty for the baseline. Independent of the enum value name, which appears in target strings.
        /// </summary>
        internal static string GetNameToken(InstructionSetLevel level)
        {
            switch (level)
            {
                case InstructionSetLevel.APX: return "APX";
                default: return "";
            }
        }
    }

    public static partial class Util
    {
        // Visual Studio versions, oldest to newest. DevEnv bit values are not chronological
        // (vs2026 is above xcode and make), so "or newer" is decided by position in this list.
        // Append future Visual Studio versions here.
        private static readonly DevEnv[] s_visualStudioOrder =
        {
            DevEnv.vs2015,
            DevEnv.vs2017,
            DevEnv.vs2019,
            DevEnv.vs2022,
            DevEnv.vs2026,
        };

        private const ProcessorArchitecture AllArchitectures = ProcessorArchitecture.x86 | ProcessorArchitecture.x64 | ProcessorArchitecture.arm64;

        private static ProcessorArchitecture GetValidArchitectures(InstructionSetLevel level)
        {
            switch (level)
            {
                case InstructionSetLevel.Baseline: return AllArchitectures;
                case InstructionSetLevel.APX: return ProcessorArchitecture.x64;
                default: return 0;
            }
        }

        /// <returns>The oldest DevEnv that supports the level, or 0 if there is no requirement.</returns>
        private static DevEnv GetMinimumDevEnv(InstructionSetLevel level)
        {
            switch (level)
            {
                case InstructionSetLevel.APX: return DevEnv.vs2026;
                default: return 0;
            }
        }

        /// <summary>
        /// The token a non-default architecture adds to platform strings (paths) when the level is the baseline.
        /// </summary>
        private static string GetArchitectureNameToken(ProcessorArchitecture architecture)
        {
            switch (architecture)
            {
                case ProcessorArchitecture.arm64: return "Armv8"; // MSVC ARM64 default is /arch:armv8.0
                default: return architecture.ToString();
            }
        }

        private static bool IsDevEnvAtLeast(DevEnv devEnv, DevEnv minimum)
        {
            int index = Array.IndexOf(s_visualStudioOrder, devEnv);
            int minimumIndex = Array.IndexOf(s_visualStudioOrder, minimum);
            return index >= 0 && minimumIndex >= 0 && index >= minimumIndex;
        }

        private static IPlatformDescriptor QueryPlatformDescriptor(ITarget target)
        {
            return PlatformRegistry.Query<IPlatformDescriptor>(target.GetPlatform());
        }

        /// <summary>
        /// Gets the processor architecture of a target: the explicit fragment if set, otherwise the
        /// platform's default (0 for platforms whose descriptor doesn't declare one yet).
        /// </summary>
        internal static ProcessorArchitecture GetProcessorArchitecture(ITarget target)
        {
            if (target.TryGetFragment(out ProcessorArchitecture architecture) && architecture != 0)
                return architecture;

            return QueryPlatformDescriptor(target)?.DefaultProcessorArchitecture ?? 0;
        }

        /// <summary>
        /// Gets the instruction set level of a target: the explicit fragment if set, otherwise
        /// <see cref="InstructionSetLevel.Baseline"/>.
        /// </summary>
        internal static InstructionSetLevel GetInstructionSetLevel(ITarget target)
        {
            if (target.TryGetFragment(out InstructionSetLevel level) && level != 0)
                return level;

            return InstructionSetLevel.Baseline;
        }

        /// <summary>
        /// Gets the platform string used in paths and symbol names for a target: the platform's simple string,
        /// followed by a token for a non-default instruction set level or architecture (e.g. "Win64APX",
        /// "Win64Armv8"). Default combinations give the platform's simple string unchanged.
        /// </summary>
        public static string GetDisambiguatedPlatformString(ITarget target)
        {
            IPlatformDescriptor descriptor = QueryPlatformDescriptor(target);
            return descriptor != null ? descriptor.GetDisambiguatedPlatformString(target) : GetSimplePlatformString(target.GetPlatform());
        }

        /// <summary>
        /// Gets the platform name used by the solution configuration of a target (e.g. "x64", "x64-APX", "ARM64").
        /// </summary>
        internal static string GetSolutionPlatformName(ITarget target)
        {
            Platform platform = target.GetPlatform();
            IPlatformDescriptor descriptor = PlatformRegistry.Query<IPlatformDescriptor>(platform);
            return descriptor != null ? descriptor.GetSolutionPlatformName(target) : GetToolchainPlatformString(platform, target);
        }

        /// <summary>
        /// Suffix, with its leading space, that distinguishes configurations sharing a Visual Studio platform
        /// (e.g. " APX" for "Release APX|x64"). Empty for the baseline level.
        /// </summary>
        internal static string GetConfigurationNameSuffix(ITarget target)
        {
            IPlatformDescriptor descriptor = QueryPlatformDescriptor(target);
            return descriptor != null ? descriptor.GetConfigurationNameSuffix(target) : DefaultConfigurationNameSuffix(target);
        }

        /// <summary>
        /// Gets what a custom solution platform name must append to tell apart the targets that differ by
        /// architecture or instruction set level: "" for the defaults, otherwise "x64-APX" or "ARM64".
        /// A custom <see cref="Solution.Configuration.PlatformName"/> is never modified by Sharpmake, so
        /// names that ignore it give the same platform to every architecture (reported as duplicates).
        /// </summary>
        public static string GetArchitectureDisplaySuffix(ITarget target)
        {
            string levelToken = InstructionSetLevelInfo.GetNameToken(GetInstructionSetLevel(target));
            if (levelToken.Length != 0)
                return GetProcessorArchitecture(target) + "-" + levelToken;

            ProcessorArchitecture architecture = GetProcessorArchitecture(target);
            if (architecture != 0 && !IsOmittedAsDefault(target, architecture))
                return architecture.ToString().ToUpperInvariant();

            return "";
        }

        /// <summary>
        /// Default implementation of <see cref="IPlatformDescriptor.GetConfigurationNameSuffix(ITarget)"/>.
        /// </summary>
        internal static string DefaultConfigurationNameSuffix(ITarget target)
        {
            string token = InstructionSetLevelInfo.GetNameToken(GetInstructionSetLevel(target));
            return token.Length == 0 ? "" : " " + token;
        }

        /// <summary>
        /// Default implementation of <see cref="IPlatformDescriptor.GetDisambiguatedPlatformString(ITarget)"/>.
        /// </summary>
        internal static string AppendArchitectureTokens(string simplePlatformString, ITarget target)
        {
            string levelToken = InstructionSetLevelInfo.GetNameToken(GetInstructionSetLevel(target));
            if (levelToken.Length != 0)
                return simplePlatformString + levelToken;

            ProcessorArchitecture architecture = GetProcessorArchitecture(target);
            ProcessorArchitecture defaultArchitecture = QueryPlatformDescriptor(target)?.DefaultProcessorArchitecture ?? 0;
            if (architecture != defaultArchitecture)
                return simplePlatformString + GetArchitectureNameToken(architecture);

            return simplePlatformString;
        }

        /// <summary>
        /// True for fragment values that are left out of target strings because they equal the default
        /// (an explicit default must be the same target as an unset one).
        /// </summary>
        internal static bool IsOmittedAsDefault(ITarget target, object fragmentValue)
        {
            switch (fragmentValue)
            {
                case InstructionSetLevel level:
                    return level == InstructionSetLevel.Baseline;
                case ProcessorArchitecture architecture:
                    return target.TryGetFragment(out Platform platform)
                        && architecture == (PlatformRegistry.Query<IPlatformDescriptor>(platform)?.DefaultProcessorArchitecture ?? 0);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Throws if the explicit <see cref="ProcessorArchitecture"/> / <see cref="InstructionSetLevel"/>
        /// of the target are not supported by its platform, architecture or Visual Studio version.
        /// Targets that set neither are always valid.
        /// </summary>
        internal static void VerifyProcessorArchitectureAndLevel(ITarget target)
        {
            target.TryGetFragment(out ProcessorArchitecture explicitArchitecture);
            target.TryGetFragment(out InstructionSetLevel explicitLevel);

            if (explicitArchitecture == 0 && explicitLevel == 0)
                return;

            IPlatformDescriptor descriptor = QueryPlatformDescriptor(target);
            string subject = $"Target \"{target}\" (platform {GetSimplePlatformString(target.GetPlatform())})";

            if (explicitArchitecture != 0)
            {
                ProcessorArchitecture supported = descriptor?.SupportedProcessorArchitectures ?? 0;
                if ((supported & explicitArchitecture) != explicitArchitecture)
                {
                    throw new Error("{0}: processor architecture {1} is not supported by this platform; supported: {2}.",
                        subject, explicitArchitecture, supported == 0 ? "none" : supported.ToString());
                }
            }

            // An explicit Baseline is valid everywhere
            if (explicitLevel == 0 || explicitLevel == InstructionSetLevel.Baseline)
                return;

            ProcessorArchitecture architecture = GetProcessorArchitecture(target);
            InstructionSetLevel allowedLevels = descriptor?.GetSupportedInstructionSetLevels(architecture) ?? InstructionSetLevel.Baseline;
            ProcessorArchitecture validArchitectures = GetValidArchitectures(explicitLevel);
            if ((allowedLevels & explicitLevel) != explicitLevel || (validArchitectures & architecture) == 0)
            {
                throw new Error("{0}: instruction set level {1} is not supported with processor architecture {2}; supported levels on this platform for that architecture: {3}.",
                    subject, explicitLevel, architecture == 0 ? "(none)" : architecture.ToString(), allowedLevels);
            }

            DevEnv minimumDevEnv = GetMinimumDevEnv(explicitLevel);
            if (minimumDevEnv != 0 && target.TryGetFragment(out DevEnv devEnv) && !IsDevEnvAtLeast(devEnv, minimumDevEnv))
            {
                throw new Error("{0}: instruction set level {1} requires {2} or newer, but the target uses {3}.",
                    subject, explicitLevel, minimumDevEnv, devEnv);
            }
        }
    }
}
