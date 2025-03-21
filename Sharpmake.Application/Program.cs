// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Sharpmake.Generators;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Application
{
    public enum ExitCode
    {
        Success = 0,
        GenerationError,
        Error,
        InternalError,
        UnknownError,
    }

    public static partial class Program
    {
        [DefaultValue(None)]
        internal enum TestOptions
        {
            None,
            Regression,
            QuickConfigure,
            Configure
        }

        #region Log

        public static bool DebugEnable = false;
        private static int s_errorCount = 0;
        private static int s_warningCount = 0;

        public static void LogWrite(string format, params object[] args)
        {
            LogWrite(string.Format(format, args));
        }

        public static void LogWrite(string message)
        {
            string prefix = string.Empty;

            if (DebugEnable)
            {
                TimeSpan span = DateTime.Now - Util.ProgramStartTime;
                prefix = string.Format("[{0:00}:{1:00}] ", span.Minutes, span.Seconds);
                message = prefix + message;
            }

            Console.Write(message);
            if (Debugger.IsAttached)
            {
                message = message.Replace(prefix + Util.CallerInfoTag, string.Empty);
                Trace.Write(message);
            }
        }

        public static void LogWriteLine(string format, params object[] args)
        {
            LogWriteLine(string.Format(format, args));
        }

        public static void LogWriteLine(string msg)
        {
            LogWrite(msg + Environment.NewLine);
        }

        public static void DebugWrite(string format, params object[] args)
        {
            DebugWrite(string.Format(format, args));
        }

        public static void DebugWrite(string msg)
        {
            if (DebugEnable)
            {
                LogWrite(msg);
            }
        }

        public static void DebugWriteLine(string format, params object[] args)
        {
            DebugWriteLine(string.Format(format, args));
        }

        public static void DebugWriteLine(string msg)
        {
            if (DebugEnable)
                DebugWrite(msg + Environment.NewLine);
        }

        public static void WarningWrite(string format, params object[] args)
        {
            WarningWrite(string.Format(format, args));
        }

        public static void WarningWrite(string msg)
        {
            Interlocked.Increment(ref s_warningCount);
            LogWrite("[WARNING]" + msg);
        }

        public static void WarningWriteLine(string format, params object[] args)
        {
            WarningWriteLine(string.Format(format, args));
        }

        public static void WarningWriteLine(string msg)
        {
            WarningWrite(msg + Environment.NewLine);
        }

        public static void ErrorWrite(string format, params object[] args)
        {
            ErrorWrite(string.Format(format, args));
        }

        public static void ErrorWrite(string msg)
        {
            Interlocked.Increment(ref s_errorCount);
            LogWrite("[ERROR]" + msg);
        }

        public static void ErrorWriteLine(string format, params object[] args)
        {
            ErrorWriteLine(string.Format(format, args));
        }

        public static void ErrorWriteLine(string msg)
        {
            ErrorWrite(msg + Environment.NewLine);
        }

        #endregion

        private static int Main()
        {
            if (CommandLine.ContainParameter("breakintodebugger"))
            {
                Console.WriteLine("Debugger requested. Please attach a debugger and press ENTER to continue");
                while (Console.ReadKey(true).Key != ConsoleKey.Enter)
                {
                    Console.WriteLine("Press ENTER to continue");
                }
                Debugger.Break();
            }
            // This GC gives a little bit better results than the other ones. "LowLatency" is giving really bad results(twice slower than the other ones).
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
            Trace.Assert(System.Runtime.GCSettings.IsServerGC, "Server GC is not active! Sharpmake will be much slower!");

            AppDomain currentDomain = AppDomain.CurrentDomain;

            Mutex oneInstanceMutex = null;
            Argument parameters = new Argument();
            ExitCode exitCode = ExitCode.Success;
            try
            {
                DebugEnable = CommandLine.ContainParameter("verbose") || CommandLine.ContainParameter("debug") || CommandLine.ContainParameter("diagnostics");

                GetAssemblyInfo(Assembly.GetExecutingAssembly(), out var _, out var version, out var versionString, out var _);
  
                LogWriteLine($"sharpmake {versionString}");
                LogWriteLine("  arguments: {0}", CommandLine.GetProgramCommandLine());
                LogWriteLine("  directory: {0}", Directory.GetCurrentDirectory());
                LogWriteLine("  platform: {0} - {1}", Util.GetExecutingPlatform().ToString(), RuntimeInformation.OSDescription);
                LogWriteLine("  compiled with framework: {0}", Util.FrameworkDisplayName());
                LogWriteLine("  running on framework: {0}", RuntimeInformation.FrameworkDescription);
                LogWriteLine(string.Empty);

                // display help if wanted and quit
                if ((CommandLine.GetProgramCommandLine().Length == 0) || CommandLine.ContainParameter("help"))
                {
                    LogWriteLine(CommandLine.GetCommandLineHelp(typeof(Argument), false));
                    return CommandLine.ContainParameter("help") ? (int)ExitCode.Success : (int)ExitCode.Error;
                }

                AppDomain.CurrentDomain.AssemblyLoad += AppDomain_AssemblyLoad;

                // Log warnings and errors from builder
                Assembler.EventOutputError += ErrorWrite;
                Assembler.EventOutputWarning += WarningWrite;

                CommandLine.ExecuteOnObject(parameters);

                if (parameters.Exit)
                    return (int)ExitCode.Success;

                const string sharpmakeSymbolPrefix = "_SHARPMAKE";
                List<string> invalidSymbols = parameters.Defines.Where(define => define.StartsWith(sharpmakeSymbolPrefix)).ToList();
                if (invalidSymbols.Any())
                {
                    string invalidSymbolsString = string.Join(", ", invalidSymbols);
                    throw new Error($"Only Sharpmake process can define symbols starting with {sharpmakeSymbolPrefix}. Invalid symbols defined: {invalidSymbolsString}");
                }

                parameters.Defines.Add(sharpmakeSymbolPrefix); // A generic sharpmake define to allow scripts to exclude part of code if not used with sharpmake
                parameters.Defines.Add($"{sharpmakeSymbolPrefix}_{version.Major}_{version.Minor}_X");
                parameters.Defines.Add($"{sharpmakeSymbolPrefix}_{version.Major}_{version.Minor}_{version.Build}");

                parameters.Validate();

                // CommonPlatforms.dll is always loaded by default because those are shipped with
                // the Sharpmake package.
                PlatformRegistry.RegisterExtensionAssembly(typeof(Windows.Win32Platform).Assembly);

                // If any platform declares its own command line options, execute and validate
                // them as well.
                IEnumerable<Platform> platformsCmdLines = PlatformRegistry.GetAvailablePlatforms<ICommandLineInterface>();
                foreach (var platform in platformsCmdLines)
                {
                    var platformCmdLine = PlatformRegistry.Get<ICommandLineInterface>(platform);
                    CommandLine.ExecuteOnObject(platformCmdLine);
                    platformCmdLine.Validate();
                }

                bool oneInstanceMutexCreated;
                string mutexName = string.Format("SharpmakeSingleInstanceMutex{0}", parameters.MutexSuffix); // Allow custom mutex name suffix. Useful to debug concurrently multiple sharpmake running from different branches
                oneInstanceMutex = new Mutex(true, mutexName, out oneInstanceMutexCreated);

                if (!oneInstanceMutexCreated)
                {
                    try
                    {
                        if (!oneInstanceMutex.WaitOne(0))
                        {
                            LogWriteLine("wait for another instance(s) of sharpmake to terminate...");
                            oneInstanceMutex.WaitOne();
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        // This occurs if another sharpmake is killed in the debugger
                    }
                    finally
                    {
                        LogWriteLine("waiting done.");
                    }
                }

                if (parameters.RegexMatchCacheEnabled)
                {
                    GlobalRegexMatchCache.Init(parameters.RegexMatchCacheInitialCapacity);
                }

                switch (parameters.TestOption)
                {
                    case TestOptions.Regression:
                        {
                            var regressionTest = new BuildContext.RegressionTest(parameters.OutputDirectory, parameters.ReferenceDirectory, parameters.RemapRoot);
                            GenerateAll(regressionTest, parameters);
                            exitCode = ExitCode.Success;

                            var regressions = regressionTest.GetRegressions().ToList();
                            if (regressions.Count > 0)
                            {
                                exitCode = ExitCode.Error;
                                DebugWriteLine($"{regressions.Count} Regressions detected:");
                                List<BuildContext.RegressionTest.OutputInfo> fileChanges = regressions.Where(x => x.FileStatus == BuildContext.RegressionTest.FileStatus.Different).ToList();
                                LogFileChanges(fileChanges, parameters.RegressionDiff);

                                var fileMissing = regressions.Where(x => x.FileStatus == BuildContext.RegressionTest.FileStatus.NotGenerated).Select(x => x.ReferencePath).ToList();
                                if (fileMissing.Count > 0)
                                {
                                    fileMissing.Sort();
                                    DebugWriteLine($"  {fileMissing.Count} files are missing from the output:");
                                    fileMissing.ForEach(x => DebugWriteLine($"    {x}"));
                                }
                            }
                        }
                        break;
                    case TestOptions.QuickConfigure:
                        {
                            exitCode = AnalyzeConfigureOrder(parameters, true);
                        }
                        break;
                    case TestOptions.Configure:
                        {
                            exitCode = AnalyzeConfigureOrder(parameters, false);
                        }
                        break;
                    case TestOptions.None:
                    default:
                        {
                            if (parameters.OutputDirectory != null)
                            {
                                // output redirect mode
                                var redirectOutput = new BuildContext.RedirectOutput(parameters.OutputDirectory, parameters.RemapRoot);
                                GenerateAll(redirectOutput, parameters);
                                exitCode = ExitCode.Success;
                            }
                            else
                            {
                                var generateAll = new BuildContext.GenerateAll(parameters.DebugLog, parameters.WriteFiles);
                                GenerateAll(generateAll, parameters);
                                exitCode = ExitCode.Success;

                                Util.ExecuteFilesAutoCleanup();
                            }
                        }
                        break;
                }

                if (CSproj.AllCsProjSubTypesInfos.Any())
                    Util.SerializeAllCsprojSubTypes(CSproj.AllCsProjSubTypesInfos);

                if (parameters.RegexMatchCacheEnabled)
                {
                    int regexMatchCacheInitialCapacity = parameters.RegexMatchCacheInitialCapacity;
                    int regexMatchCacheSize = GlobalRegexMatchCache.Count;
                    if (regexMatchCacheInitialCapacity < regexMatchCacheSize)
                    {
                        WarningWriteLine("Warning (perf): Consider increasing regex match cache initial capacity from {0} to at least {1} ( /regexMatchCacheInitialCapacity({1}) ).", regexMatchCacheInitialCapacity, regexMatchCacheSize);
                    }

                    GlobalRegexMatchCache.UnInit();
                }
            }
            catch (Error e)
            {
                // Log error message
                Exception innerException = e;
                while (innerException.InnerException != null)
                    innerException = innerException.InnerException;
                ErrorWriteLine(Environment.NewLine + "Error:" + Environment.NewLine + innerException.Message);

                // Then log details
                LogWriteLine(Util.GetCompleteExceptionMessage(e, "\t"));
                exitCode = ExitCode.Error;
            }
            catch (InternalError e)
            {
                ErrorWriteLine(Environment.NewLine + "Internal Error:");
                LogWriteLine(Util.GetCompleteExceptionMessage(e, "\t"));
                exitCode = ExitCode.InternalError;
            }
            catch (Exception e)
            {
                LogWriteLine(Environment.NewLine + "Exception Error:");
                LogWriteLine(Util.GetCompleteExceptionMessage(e, "\t"));
                exitCode = ExitCode.UnknownError;
            }
            finally
            {
                if (oneInstanceMutex != null)
                {
                    oneInstanceMutex.ReleaseMutex();
                    GC.KeepAlive(oneInstanceMutex);
                }

                if (parameters.Debug)
                {
                    Console.WriteLine("DEBUG Sharpmake.Application: Press any key to exit...");
                    Console.ReadKey();
                }
            }

            if (exitCode <= ExitCode.Error) // Do not display summary in case of unknown exception or internal error
                LogWriteLine(@"{0} errors, {1} warnings", s_errorCount, s_warningCount);
            if (s_errorCount != 0)
            {
                if (Debugger.IsAttached)
                {
                    LogWriteLine("Please look at the errors.");
                    Debugger.Break();
                }
            }

            // Always return the same error code no matter the number of errors.
            if (exitCode == ExitCode.Success && s_errorCount != 0)
            {
                exitCode = ExitCode.GenerationError;
            }
            return (int)exitCode;
        }

        private static void AppDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            LogSharpmakeExtensionLoaded(args.LoadedAssembly);
        }

        /// <summary>
        /// Retrieve information from the assembly passed as argument
        /// </summary>
        /// <param name="assembly">The assembly to get information from</param>
        /// <param name="name">The name of the assembly</param>
        /// <param name="version">The version as read from the assembly (the 4th componant is not used by Sharpmake core, but may be used by Sharpmake extended assemblies)</param>
        /// <param name="versionString">A string of the format "<x.y.z[.a]> [(non-official)] [(<complete version string as read from the InformationalVersion attribute>)]"</param>
        /// <param name="location">The location of the assembly</param>
        private static void GetAssemblyInfo(Assembly assembly, out string name, out Version version, out string versionString, out string location)
        {
            // Name
            name = assembly.GetName().Name;

            // Version
            version = assembly.GetName().Version;
            versionString = "(non-official)";
            string informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informationalVersion))
            {
                var endOfVersionIndex = informationalVersion.IndexOf("+", StringComparison.Ordinal);
                if (endOfVersionIndex != -1)
                {
                    versionString = informationalVersion[..endOfVersionIndex];
                    if (informationalVersion.IndexOf("-g", 0, endOfVersionIndex, StringComparison.Ordinal) != -1
                        && informationalVersion.IndexOf(".Commits.0.", 0, endOfVersionIndex, StringComparison.Ordinal) == -1)
                    {
                        versionString += " (non-official)";
                    }
                }

                if (DebugEnable)
                    versionString += $" ({informationalVersion})";
            }

            // Location
            location = assembly.Location;
        }

        private static void LogSharpmakeExtensionLoaded(Assembly extensionAssembly)
        {
            if (extensionAssembly == null)
                return;

            if (!ExtensionLoader.ExtensionChecker.IsSharpmakeExtension(extensionAssembly))
                return;

            GetAssemblyInfo(extensionAssembly, out var extensionName, out var _, out var extensionVersion, out var extensionLocation);
            LogWriteLine("    {0} {1} loaded from '{2}' in assembly load context '{3}'", extensionName, extensionVersion, extensionLocation, AssemblyLoadContext.GetLoadContext(extensionAssembly).Name);
        }

        private static void CreateBuilderAndGenerate(BuildContext.BaseBuildContext buildContext, Argument parameters, bool generateDebugSolution)
        {
            string cleanupSuffixOldValue = Util.FilesAutoCleanupDBSuffix;
            bool cleanupActiveOldValue = Util.FilesAutoCleanupActive;

            if (generateDebugSolution)
            {
                // Set a cleanup context exclusive to debug solution
                Util.FilesAutoCleanupDBSuffix = "_debugsolution";
                Util.FilesAutoCleanupActive = true;
                if (!string.IsNullOrEmpty(parameters.DebugSolutionPath))
                    Util.FilesAutoCleanupDBPath = parameters.DebugSolutionPath;
                else
                    Util.FilesAutoCleanupDBPath = Path.GetDirectoryName(parameters.Sources[0]);
            }

            using (Builder builder = CreateBuilder(buildContext, parameters, allowCleanBlobs: true, generateDebugSolution: generateDebugSolution))
            {
                if (parameters.CleanBlobsOnly)
                {
                    LogWriteLine("success:");
                    LogWriteLine("    blobs               {0,4} cleaned, {1,3} already cleaned", Project.BlobCleaned, Project.BlobAlreadyCleaned);
                }
                else if (parameters.BlobOnly)
                {
                    LogWriteLine("success:");
                    LogWriteLine("    blobs               {0,4} generated, {1,3} up-to-date", Project.BlobGenerated, Project.BlobUpdateToDate);
                }
                else
                {
                    IDictionary<Type, GenerationOutput> outputs;
                    using (Builder.Instance.CreateProfilingScope("Generate"))
                        outputs = builder.Generate();

                    foreach (var output in outputs)
                    {
                        if (output.Value.Exception != null)
                            throw new Error(output.Value.Exception, "Error encountered while generating {0}", output.Key);
                    }

                    if (!string.IsNullOrEmpty(parameters.ProfileFile))
                        builder.DumpTraceFile(parameters.ProfileFile);

                    if (parameters.DumpDependency)
                        DependencyTracker.Instance.DumpGraphs(outputs);

                    LogWriteGenerateResults(outputs);
                }
            }

            LogWriteLine("  time: {0:0.00} sec.", (DateTime.Now - Util.ProgramStartTime).TotalSeconds);
            LogWriteLine("  completed on {0}.", DateTime.Now);

            if (generateDebugSolution)
            {
                // Execute cleanup for debug solution generation
                Util.ExecuteFilesAutoCleanup(true);

                // Restore original cleanup context
                Util.FilesAutoCleanupDBSuffix = cleanupSuffixOldValue;
                Util.FilesAutoCleanupActive = cleanupActiveOldValue;
            }
        }

        private static void GenerateAll(BuildContext.BaseBuildContext buildContext, Argument parameters)
        {
            if (parameters.GenerateDebugSolution)
            {
                CreateBuilderAndGenerate(buildContext, parameters, generateDebugSolution: true);
                // because the debug solution generation runs before the user code,
                // we need to do some cleanup so we don't pollute the subsequent generation
                ExtensionMethods.ClearVisualStudioDirCaches();
            }

            if (!parameters.GenerateDebugSolutionOnly)
            {
                CreateBuilderAndGenerate(buildContext, parameters, generateDebugSolution: false);
            }
        }

        private static ExitCode AnalyzeConfigureOrder(Argument parameters, bool stopOnFirstError)
        {
            // Analyze .sharpmake code
            var report = Analyzer.Analyzer.AnalyzeConfigure(context => CreateBuilder(context, parameters, false), stopOnFirstError);

            Console.WriteLine("{0} faulty root methods.", report.Count());
            using (var stream = File.Open("faultymethods.txt", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                if (report.Any())
                {
                    Console.WriteLine("Faulty methods are dependent on a specific Configure() call order. See faultymethods.txt");

                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("You can use www.yuml.me to view this in a diagram.");
                        var set = new SortedSet<string>();
                        foreach (var root in report.OrderBy(x => x.Method.Name))
                        {
                            RecursivePrintMethodInfo(root, set);
                        }
                        foreach (var line in set)
                        {
                            writer.WriteLine(line);
                        }
                    }


                    return ExitCode.Error;
                }
            }

            return ExitCode.Success;
        }

        public static void LogWriteGenerateResults(IDictionary<Type, GenerationOutput> outputs)
        {
            var projects = outputs.Where(o => typeof(Project).IsAssignableFrom(o.Key));
            var solutions = outputs.Where(o => typeof(Solution).IsAssignableFrom(o.Key));
            var others = outputs.Where(o => ((!typeof(Project).IsAssignableFrom(o.Key)) && (!typeof(Solution).IsAssignableFrom(o.Key))));

            var generatedProjectFiles = new List<string>(projects.SelectMany(o => o.Value.Generated));
            var skippedProjectFiles = new List<string>(projects.SelectMany(o => o.Value.Skipped));
            var generatedSolutionFiles = new List<string>(solutions.SelectMany(o => o.Value.Generated));
            var skippedSolutionFiles = new List<string>(solutions.SelectMany(o => o.Value.Skipped));

            var generatedOtherFiles = new List<string>(others.SelectMany(o => o.Value.Generated));
            var skippedOtherFiles = new List<string>(others.SelectMany(o => o.Value.Skipped));

            if (generatedProjectFiles.Count != 0)
            {
                LogWriteLine("  " + generatedProjectFiles.Count + " project" + (generatedProjectFiles.Count > 1 ? "s" : "") + " generated:");
                generatedProjectFiles.Sort();
                foreach (string file in generatedProjectFiles)
                    LogWriteLine("    {0}", file);
            }

            if (generatedSolutionFiles.Count != 0)
            {
                LogWriteLine("  " + generatedSolutionFiles.Count + " solution" + (generatedSolutionFiles.Count > 1 ? "s" : "") + " generated:");
                generatedSolutionFiles.Sort();
                foreach (string file in generatedSolutionFiles)
                    LogWriteLine("    {0}", file);
            }

            if (Project.FastBuildMasterGeneratedFiles.Count > 0)
            {
                LogWriteLine("  " + Project.FastBuildMasterGeneratedFiles.Count + " fastbuild master bff generated:");
                Project.FastBuildMasterGeneratedFiles.Sort();
                foreach (string file in Project.FastBuildMasterGeneratedFiles)
                    LogWriteLine("    {0}", file);
            }

            if (generatedOtherFiles.Count != 0)
            {
                LogWriteLine("  " + generatedOtherFiles.Count + " other file" + (generatedOtherFiles.Count > 1 ? "s" : "") + " generated:");
                generatedOtherFiles.Sort();
                foreach (string file in generatedOtherFiles)
                    LogWriteLine("    {0}", file);
            }

            LogWriteLine("  Results:");
            if (generatedProjectFiles.Count > 0 || skippedProjectFiles.Count > 0)
            {
                LogWriteLine("    projects  ({0,5} configurations) {1,5} generated, {2,5} up-to-date", Project.Configuration.Count, generatedProjectFiles.Count, skippedProjectFiles.Count);
                if (Project.FastBuildGeneratedFileCount > 0 || Project.FastBuildUpToDateFileCount > 0)
                    LogWriteLine("    fastbuild                        {0,5} generated, {1,5} up-to-date", Project.FastBuildGeneratedFileCount, Project.FastBuildUpToDateFileCount);
            }
            if (generatedSolutionFiles.Count > 0 || skippedSolutionFiles.Count > 0)
                LogWriteLine("    solutions ({0,5} configurations) {1,5} generated, {2,5} up-to-date", Solution.Configuration.Count, generatedSolutionFiles.Count, skippedSolutionFiles.Count);

            if (Project.BlobGenerated > 0 || Project.BlobUpdateToDate > 0)
                LogWriteLine("    blobs                            {0,5} generated, {1,5} up-to-date", Project.BlobGenerated, Project.BlobUpdateToDate);

            if (generatedOtherFiles.Count > 0 || skippedOtherFiles.Count > 0)
                LogWriteLine("    other files                      {0,5} generated, {1,5} up-to-date", generatedOtherFiles.Count, skippedOtherFiles.Count);
        }

        public static Builder CreateBuilder(BuildContext.BaseBuildContext context, Argument parameters, bool allowCleanBlobs, bool generateDebugSolution = false)
        {
            Builder builder = new Builder(
                context,
                parameters.Multithreaded,
                parameters.DumpDependency,
                allowCleanBlobs && parameters.CleanBlobsOnly,
                parameters.BlobOnly,
                parameters.SkipInvalidPath,
                parameters.Diagnostics,
                debugScripts: true, // warning: some code that rely on callstacks misbehaves in release, because methods can completely disappear due to optimizations, so force disable for now
                getGeneratorsManagerCallBack: GetGeneratorsManager,
                defines: parameters.Defines
            );

            // Allow message log from builder.
            builder.EventOutputError += ErrorWrite;
            builder.EventOutputWarning += WarningWrite;
            builder.EventOutputMessage += LogWrite;
            builder.EventOutputDebug += DebugWrite;

            if (!string.IsNullOrEmpty(parameters.ProfileFile))
                builder.EnableProfiling();

            try
            {
                // Generate debug solution
                using (Builder.Instance.CreateProfilingScope("Debug solution"))
                {
                    if (generateDebugSolution)
                    {
                        LogWriteLine("Generate debug solution...");
                        DebugProjectGenerator.GenerateDebugSolution(parameters.Sources, parameters.DebugSolutionPath, builder.Arguments, parameters.DebugSolutionStartArguments, parameters.DebugSolutionDevEnv);
                        builder.BuildProjectAndSolution();
                        return builder;
                    }
                }

                // Load user input (either files or pre-built assemblies)
                using (Builder.Instance.CreateProfilingScope("EntryPoints"))
                {
                    switch (parameters.Input)
                    {
                        case Argument.InputType.File:
                            builder.ExecuteEntryPointInAssemblies<Main>(builder.LoadSharpmakeFiles(parameters.Sources));
                            break;
                        case Argument.InputType.Assembly:
                            builder.ExecuteEntryPointInAssemblies<Main>(builder.LoadAssemblies(parameters.Assemblies));
                            break;
                        case Argument.InputType.Undefined:
                        default:
                            throw new Error("Sharpmake input missing, use /sources() or /assemblies()");
                    }
                }

                if (builder.Arguments.TypesToGenerate.Count == 0)
                    throw new Error("Sharpmake has nothing to generate!" + Environment.NewLine
                        + $"  Make sure to have a static entry point method flagged with [{typeof(Main).FullName}] attribute, and add 'arguments.Generate<[your_class]>();' in it.");
                builder.Context.ConfigureOrder = builder.Arguments.ConfigureOrder;

                // Call all configuration's methods and resolve project/solution member's values
                using (Builder.Instance.CreateProfilingScope("Build"))
                    builder.BuildProjectAndSolution();

                return builder;
            }
            catch
            {
                builder.Dispose();
                throw;
            }
        }

        private static void RecursivePrintMethodInfo(Analyzer.ConfigureMethodInfo method, ISet<string> set, int nested = 0)
        {
            ++nested;
            foreach (var dependent in method.Dependents.OrderBy(x => x.Method.ToString()))
            {
                set.Add(string.Format("[{0}]->[{1}]", dependent, method));

                if (dependent.Dependents.Any())
                    RecursivePrintMethodInfo(dependent, set, nested);
            }
        }

        private static string LocateDiffExecutable()
        {
            var candidateDirectories = new List<string>();

            if (!Util.UsesUnixSeparator) // poor way to test the OS...
            {
                try
                {
                    candidateDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Git\usr\bin"));
                    candidateDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Git\usr\bin"));
                }
                catch { }
            }
            candidateDirectories.AddRange(Environment.GetEnvironmentVariable("PATH").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            foreach (var candidateDirectory in candidateDirectories)
            {
                foreach (var candidateExeName in new[] { "diff", "diff.exe" })
                {
                    var candidatePath = Path.Combine(candidateDirectory, candidateExeName);
                    if (File.Exists(candidatePath))
                        return candidatePath;
                }
            }

            return null;
        }

        private static void LogFileChanges(List<BuildContext.RegressionTest.OutputInfo> fileChanges, bool showRegressionDiff)
        {
            if (fileChanges.Count == 0)
                return;

            var diffs = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            object dictionaryAccess = new object();


            string diffExecutable = LocateDiffExecutable();
            if (!showRegressionDiff || diffExecutable == null)
            {
                DebugWriteLine($"  {fileChanges.Count} files have changed from the reference:");
                fileChanges.ForEach(x =>
                {
                    DebugWriteLine($"    Exp: {x.ReferencePath}");
                    DebugWriteLine($"    Was: {x.OutputPath}");
                });
            }
            else
            {
                DebugWriteLine($"  {fileChanges.Count} files have changed from the reference. Aggregating diff using '{diffExecutable}'");
                Parallel.ForEach(fileChanges, x =>
                {
                    bool refFileExists = File.Exists(x.ReferencePath);
                    bool outFileExists = File.Exists(x.OutputPath);

                    if (refFileExists && outFileExists)
                    {
                        Process process = new Process();
                        process.StartInfo.FileName = diffExecutable;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.RedirectStandardOutput = true;

                        // -i, --ignore-case               ignore case differences in file contents
                        // -u, -U NUM, --unified[=NUM]   output NUM (default 3) lines of unified context
                        // -w, --ignore-all-space          ignore all white space
                        process.StartInfo.Arguments = $"-i -u -w {x.ReferencePath} {x.OutputPath}";

                        process.Start();

                        var output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        var lines = output.Split('\n').Where(l => (l.Length > 1 && (l[0] == '+' || l[0] == '-') && !l.StartsWith("--- ") && !l.StartsWith("+++ ")));
                        var diff = string.Concat(lines);
                        lock (dictionaryAccess)
                        {
                            List<string> currentList = null;
                            if (!diffs.TryGetValue(diff, out currentList))
                                diffs[diff] = new List<string> { x.OutputPath };
                            else
                                currentList.Add(x.OutputPath);
                        }
                    }
                    else if (!refFileExists)
                        DebugWriteLine($"    ExtraFileGenerated: {x.OutputPath}");
                    else if (!outFileExists)
                        DebugWriteLine($"    MissingFileInOutput: {x.ReferencePath}");
                });

                int i = 0;
                foreach (var diff in diffs.OrderByDescending(d => d.Value.Count))
                {
                    DebugWriteLine(
                        $"    Diff block {++i}/{diffs.Count}"
                        + (diff.Value.Count > 1 ? $" shared by {diff.Value.Count} files:" : " only in '" + diff.Value.First() + "':")
                    );
                    int j = 0;
                    foreach (var file in diff.Value.OrderBy(f => f))
                        DebugWriteLine($"      {++j}/{diff.Value.Count}  {file}");

                    var diffLines = diff.Key.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (diffLines.Length == 0)
                    {
                        DebugWriteLine("        // only whitespace or casing changes");
                    }
                    else
                    {
                        foreach (var diffLine in diffLines)
                            DebugWriteLine($"    {diffLine}");
                    }
                }
            }
        }


        public static IGeneratorManager GetGeneratorsManager()
        {
            return new GeneratorManager();
        }
    }
}
