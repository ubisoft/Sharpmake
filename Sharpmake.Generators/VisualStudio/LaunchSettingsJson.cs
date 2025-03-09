// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Sharpmake.Generators.VisualStudio
{
    internal class LaunchSettingsJson
    {
        private const string FileNameWithoutExtension = "launchSettings";
        private const string Extension = ".json";
        private const string FileName = FileNameWithoutExtension + Extension;

        /// <summary>
        /// Generate a launchSettings.json in a Properties subfolder of the csproj from the conf.CsprojUserFile
        /// </summary>
        /// <param name="builder">The builder to use</param>
        /// <param name="project">The project the conf belong to</param>
        /// <param name="projectPath">The path of the csproj</param>
        /// <param name="configurations">The list of configurations to lookup for CsprojUserFile</param>
        /// <param name="generatedFiles">Files written by the method</param>
        /// <param name="skipFiles">Files already up-to-date and skipped</param>
        /// <returns>The full path of the launchSettings.json</returns>
        public static string Generate(
            Builder builder,
            Project project,
            string projectPath,
            IEnumerable<Project.Configuration> configurations,
            IList<string> generatedFiles,
            IList<string> skipFiles
        )
        {
            bool overwriteFile;
            var launchSettingsProfiles = GetLaunchSettingsFromCsprojUserFile(project, configurations, out overwriteFile);
            if (launchSettingsProfiles == null || !launchSettingsProfiles.Any())
                return null;

            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);

            var root = new JsonRoot
            {
                profiles = launchSettingsProfiles
            };

            // Write the list of files.
            writer.Write(JsonSerializer.Serialize(root, GetJsonSerializerOptions()));
            writer.Flush();

            //Skip overwriting user file if it exists already so he can keep his setup
            // unless the UserProjSettings specifies to overwrite
            var userFileInfo = new FileInfo(Path.Combine(projectPath, "Properties", FileName));
            bool shouldWrite = !userFileInfo.Exists || overwriteFile;
            if (shouldWrite && builder.Context.WriteGeneratedFile(typeof(LaunchSettingsJson), userFileInfo, memoryStream))
                generatedFiles.Add(userFileInfo.FullName);
            else
                skipFiles.Add(userFileInfo.FullName);

            return userFileInfo.FullName;
        }

        /// <summary>
        /// Get the formatting properties of the json
        /// </summary>
        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                // shouldn't matter
                AllowTrailingCommas = true,

                // we want enums to be written in plain text, so we need a converter
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },

                // we only write the values that are non default
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,

                // write properties as they are named in the Profile class
                PropertyNamingPolicy = null,

                // the file is read by humans as well as machine, so indent it
                WriteIndented = true,
            };
        }

        /// <summary>
        /// Represents the json root node
        /// </summary>
        private class JsonRoot
        {
            /// <summary>
            /// The list of profiles, the key is the profile name
            /// </summary>
            public Dictionary<string, Profile> profiles { get; set; }
        }

        /// <summary>
        /// Represents an individual profile in the json
        /// </summary>
        private class Profile
        {
            public enum Command
            {
                Invalid, // keep this as default below so we force the other values to be written in the json
                Project,
                Executable
            }

            /// <summary>
            /// Identifies the debug target to run.
            /// </summary>
            /// <remarks>
            /// Mandatory argument.
            /// </remarks>
            public Command commandName { get; set; } = Command.Invalid;

            /// <summary>
            /// The arguments to pass to the target being run.
            /// </summary>
            public string commandLineArgs { get; set; }

            /// <summary>
            /// An absolute or relative path to the executable.
            /// </summary>
            public string executablePath { get; set; }

            /// <summary>
            /// Sets the working directory of the command.
            /// </summary>
            public string workingDirectory { get; set; }

            /// <summary>
            /// Set to true to enable native code debugging.
            /// </summary>
            /// <remarks>
            /// Default: false
            /// </remarks>
            public bool nativeDebugging { get; set; }

            public override bool Equals(object obj)
            {
                return obj is Profile profile &&
                       commandName == profile.commandName &&
                       commandLineArgs == profile.commandLineArgs &&
                       executablePath == profile.executablePath &&
                       workingDirectory == profile.workingDirectory &&
                       nativeDebugging == profile.nativeDebugging;
            }

            public override int GetHashCode()
            {
                int hashCode = -451875935;
                hashCode = hashCode * -1521134295 + commandName.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(commandLineArgs);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(executablePath);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(workingDirectory);
                hashCode = hashCode * -1521134295 + nativeDebugging.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Convert a CsprojUserFileSettings.StartActionSetting to a Profile.Command
        /// </summary>
        /// <param name="startActionSetting">The setting to convert</param>
        /// <returns>The converted setting</returns>
        /// <exception cref="NotSupportedException">Some values are not supported, so we'll throw if that's the case</exception>
        private static Profile.Command GetCommandFromStartAction(Project.Configuration.CsprojUserFileSettings.StartActionSetting startActionSetting)
        {
            switch (startActionSetting)
            {
                case Project.Configuration.CsprojUserFileSettings.StartActionSetting.Project:
                    return Profile.Command.Project;
                case Project.Configuration.CsprojUserFileSettings.StartActionSetting.Program:
                    return Profile.Command.Executable;
                case Project.Configuration.CsprojUserFileSettings.StartActionSetting.URL:
                default:
                    throw new NotSupportedException($"{startActionSetting} is not supported in {FileName}");
            }
        }

        /// <summary>
        /// Helper method to convert the value of a string from CsprojUserFileSettings to the expected format of Profile
        /// </summary>
        /// <param name="value">The value read from CsprojUserFileSettings</param>
        /// <returns>The converted value, or null if it was unset or empty</returns>
        private static string GetStringOrNullIfRemoveLineTag(string value)
        {
            if (string.IsNullOrEmpty(value) || value == FileGeneratorUtilities.RemoveLineTag)
                return null;

            return value;
        }

        /// <summary>
        /// Converts a CsprojUserFileSettings to a Profile
        /// </summary>
        /// <param name="csprojUserFileSettings">The CsprojUserFileSettings to convert</param>
        /// <returns>The converted Profile</returns>
        /// <exception cref="NotImplementedException">Throws in case an unsupported setting is passed</exception>
        private static Profile GetProfileFromCsprojUserFileConf(Project.Configuration.CsprojUserFileSettings csprojUserFileSettings)
        {
            if (GetStringOrNullIfRemoveLineTag(csprojUserFileSettings.StartURL) != null)
                throw new NotImplementedException($"Don't know how to convert CsprojUserFileSettings.StartURL in {FileName}");

            return new Profile
            {
                commandName = GetCommandFromStartAction(csprojUserFileSettings.StartAction),
                commandLineArgs = csprojUserFileSettings.StartArguments,
                executablePath = GetStringOrNullIfRemoveLineTag(csprojUserFileSettings.StartProgram),
                workingDirectory = GetStringOrNullIfRemoveLineTag(csprojUserFileSettings.WorkingDirectory),
                nativeDebugging = csprojUserFileSettings.EnableUnmanagedDebug
            };
        }

        /// <summary>
        /// Construct the list of profiles from the conf.CsprojUserFile from all configurations
        /// </summary>
        /// <param name="project">The project that the configurations belong to</param>
        /// <param name="configurations">The list of configurations to lookup</param>
        /// <param name="overwriteFile">Will be set to true if the file is allowed to be overwritten</param>
        /// <returns>The list of profiles</returns>
        private static Dictionary<string, Profile> GetLaunchSettingsFromCsprojUserFile(
            Project project,
            IEnumerable<Project.Configuration> configurations,
            out bool overwriteFile
        )
        {
            var csprojUserFileConfs = configurations.Where(conf => conf.CsprojUserFile != null);
            overwriteFile = !csprojUserFileConfs.Any(conf => !conf.CsprojUserFile.OverwriteExistingFile);

            var profiles = csprojUserFileConfs
                .Select(conf => GetProfileFromCsprojUserFileConf(conf.CsprojUserFile))
                .Distinct()
                .ToList();

            if (!profiles.Any())
                return null;

            // in case we have only one profile, use the project name without any suffix
            if (profiles.Count == 1)
                return profiles.ToDictionary(profile => project.Name, profile => profile);

            // in case we have more, we'll suffix the project name with a decimal number starting from 1
            var dict = new Dictionary<string, Profile>();
            for (int i = 0; i < profiles.Count; i++)
                dict.Add($"{project.Name} {i + 1}", profiles[i]);
            return dict;
        }
    }
}
