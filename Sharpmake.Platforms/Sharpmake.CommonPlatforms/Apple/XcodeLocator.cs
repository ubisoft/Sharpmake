// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Diagnostics;

namespace Sharpmake
{
    /// <summary>
    /// Finds the active Xcode developer directory and the clang toolchain the way the Apple tools do
    /// (DEVELOPER_DIR, then xcode-select, then xcrun), instead of assuming /Applications/Xcode.app.
    /// The Resolve* functions take their environment as delegates so they can be tested on any OS.
    /// </summary>
    internal static class XcodeLocator
    {
        internal const string LegacyDeveloperDir = "/Applications/Xcode.app/Contents/Developer";

        private const int ProcessTimeoutMs = 10000;

        private static readonly Lazy<string> s_detectedDeveloperDir = new Lazy<string>(
            () => ResolveDeveloperDir(Environment.GetEnvironmentVariable, RunXcodeSelect, Util.DirectoryExists));

        private static readonly Lazy<string> s_xcrunClangPath = new Lazy<string>(
            () => RunAndReadFirstLine("xcrun", "--find clang"));

        /// <summary>
        /// The developer directory of the active Xcode. Only runs processes on macOS,
        /// on other platforms this is the legacy default path.
        /// </summary>
        internal static string DetectedDeveloperDir
        {
            get
            {
                if (Util.GetExecutingPlatform() != Platform.mac)
                    return LegacyDeveloperDir;
                return s_detectedDeveloperDir.Value;
            }
        }

        /// <summary>
        /// Resolves the "usr" directory of the clang toolchain (the one containing bin/clang and lib/clang)
        /// that goes with the given developer directory.
        /// </summary>
        internal static string ResolveToolchainUsrDir(string developerDir)
        {
            // xcrun only knows about the active Xcode, so don't trust it if the user picked another developer dir.
            Func<string> xcrun = developerDir == DetectedDeveloperDir && Util.GetExecutingPlatform() == Platform.mac
                ? (Func<string>)(() => s_xcrunClangPath.Value)
                : () => null;

            return ResolveToolchainUsrDir(developerDir, xcrun, Util.FileExists);
        }

        internal static string ResolveDeveloperDir(Func<string, string> getEnv, Func<string> runXcodeSelect, Func<string, bool> dirExists)
        {
            string fromEnv = getEnv("DEVELOPER_DIR")?.Trim();
            if (!string.IsNullOrEmpty(fromEnv) && dirExists(fromEnv))
                return fromEnv;

            string fromXcodeSelect = runXcodeSelect()?.Trim();
            if (!string.IsNullOrEmpty(fromXcodeSelect) && dirExists(fromXcodeSelect))
                return fromXcodeSelect;

            return LegacyDeveloperDir;
        }

        internal static string ResolveToolchainUsrDir(string developerDir, Func<string> runXcrunFindClang, Func<string, bool> fileExists)
        {
            // Full Xcode
            string xcodeUsr = $"{developerDir}/Toolchains/XcodeDefault.xctoolchain/usr";
            if (fileExists($"{xcodeUsr}/bin/clang"))
                return xcodeUsr;

            // Command Line Tools
            string cltUsr = $"{developerDir}/usr";
            if (fileExists($"{cltUsr}/bin/clang"))
                return cltUsr;

            // Ask xcrun: <usr>/bin/clang => <usr>
            string clangPath = runXcrunFindClang()?.Trim();
            if (!string.IsNullOrEmpty(clangPath))
            {
                string binDir = System.IO.Path.GetDirectoryName(clangPath.Replace('\\', '/'));
                string usrDir = binDir == null ? null : System.IO.Path.GetDirectoryName(binDir);
                if (!string.IsNullOrEmpty(usrDir))
                    return usrDir.Replace('\\', '/');
            }

            // Nothing found, keep the historical layout so errors point at a meaningful path.
            return xcodeUsr;
        }

        private static string RunXcodeSelect()
        {
            if (Util.GetExecutingPlatform() != Platform.mac)
                return null;
            return RunAndReadFirstLine("xcode-select", "-p");
        }

        private static string RunAndReadFirstLine(string fileName, string arguments)
        {
            if (Util.GetExecutingPlatform() != Platform.mac)
                return null;

            try
            {
                var startInfo = new ProcessStartInfo(fileName, arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        return null;

                    // Read stderr asynchronously so a chatty tool can't fill the pipe and block us.
                    process.ErrorDataReceived += (_, __) => { };
                    process.BeginErrorReadLine();

                    string line = process.StandardOutput.ReadLine();
                    if (!process.WaitForExit(ProcessTimeoutMs))
                    {
                        try { process.Kill(); } catch { }
                        return null;
                    }

                    return process.ExitCode == 0 ? line : null;
                }
            }
            catch (Exception e)
            {
                Util.LogWrite($"Failed to run '{fileName} {arguments}': {e.Message}");
                return null;
            }
        }
    }
}
