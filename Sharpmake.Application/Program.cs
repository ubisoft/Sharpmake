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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Sharpmake.Generators;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Application
{
    public enum ExitCode
    {
        Success = 0,
        Error = -3,
        InternalError = -3,
        UnknownError = -10
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

        private static DateTime s_startTime = DateTime.Now;
        public static bool DebugEnable = false;
        private static int s_errorCount = 0;
        private static int s_warningCount = 0;

        public static void LogWrite(string msg, params object[] args)
        {
            string message = string.Format(msg, args);
            string prefix = String.Empty;

            if (DebugEnable)
            {
                TimeSpan span = DateTime.Now - s_startTime;
                prefix = String.Format("[{0:00}:{1:00}] ", span.Minutes, span.Seconds);
                message = prefix + message;
            }

            Console.Write(message);
            if (Debugger.IsAttached)
            {
                message = message.Replace(prefix + Util.CallerInfoTag, String.Empty);
                Debug.Write(message);
            }
        }

        public static void LogWriteLine(string msg, params object[] args)
        {
            LogWrite(msg + Environment.NewLine, args);
        }


        public static void DebugWrite(string msg, params object[] args)
        {
            if (DebugEnable)
            {
                string message = args.Length > 0 ? string.Format(msg, args) : msg;
                TimeSpan span = DateTime.Now - s_startTime;

                string prefix = String.Format("[{0:00}:{1:00}] ", span.Minutes, span.Seconds);
                message = prefix + message;

                Console.Write(message);
                if (Debugger.IsAttached)
                    Debug.Write(message);
            }
        }

        public static void DebugWriteLine(string msg, params object[] args)
        {
            if (DebugEnable)
                DebugWrite(msg + Environment.NewLine, args);
        }

        public static void WarningWrite(string msg, params object[] args)
        {
            Interlocked.Increment(ref s_warningCount);
            Console.Write(msg, args);
            if (Debugger.IsAttached)
                Debug.Write(args.Length > 0 ? string.Format(msg, args) : msg);
        }

        public static void ErrorWrite(string msg, params object[] args)
        {
            Interlocked.Increment(ref s_errorCount);
            Console.Write(msg, args);
            if (Debugger.IsAttached)
                Debug.Write(args.Length > 0 ? string.Format(msg, args) : msg);
        }

        public static void ErrorWriteLine(string msg, params object[] args)
        {
            ErrorWrite(msg + Environment.NewLine, args);
        }

        #endregion

        private static int Main()
        {
            if (CommandLine.ContainParameter("-breakintodebugger"))
            {
                System.Windows.Forms.MessageBox.Show("Debugger requested. Please attach a debugger and press OK");
                Debugger.Break();
            }
            // This GC gives a little bit better results than the other ones. "LowLatency" is giving really bad results(twice slower than the other ones).
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;

            Mutex oneInstanceMutex = null;
            Argument parameters = new Argument();
            ExitCode exitCode = ExitCode.Success;

            try
            {
                DebugEnable = CommandLine.ContainParameter("verbose") || CommandLine.ContainParameter("debug") || CommandLine.ContainParameter("diagnostics");
                LogWriteLine("sharpmake");
                LogWriteLine("  arguments : {0}", CommandLine.GetProgramCommandLine());
                LogWriteLine("  directory : {0}", Directory.GetCurrentDirectory());
                LogWriteLine(string.Empty);

                // display help if wanted and quit
                if ((CommandLine.GetProgramCommandLine().Length == 0) || CommandLine.ContainParameter("help"))
                {
                    LogWriteLine(CommandLine.GetCommandLineHelp(typeof(Argument), false));
                    return (int)ExitCode.Success;
                }

                if (DebugEnable)
                    PlatformRegistry.PlatformImplementationExtensionRegistered += LogPlatformImplementationExtensionRegistered;

                CommandLine.ExecuteOnObject(parameters);

                if (parameters.Exit)
                    return (int)ExitCode.Success;

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
                    catch (System.Threading.AbandonedMutexException)
                    {
                        // This occurs if another sharpmake is killed in the debugger
                    }
                    finally
                    {
                        LogWriteLine("waiting done.");
                    }
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
                                var fileChanges = regressions.Where(x => x.FileStatus == BuildContext.RegressionTest.FileStatus.Different).ToList();
                                if (fileChanges.Count > 0)
                                {
                                    DebugWriteLine($"  {fileChanges.Count} files have changed from the reference:");
                                    fileChanges.ForEach(x =>
                                    {
                                        DebugWriteLine($"    Exp: {x.ReferencePath}");
                                        DebugWriteLine($"    Was: {x.OutputPath}");
                                    });
                                }

                                var fileMissing = regressions.Where(x => x.FileStatus == BuildContext.RegressionTest.FileStatus.NotGenerated).ToList();
                                if (fileMissing.Count > 0)
                                {
                                    DebugWriteLine($"  {fileMissing.Count} files are missing from the output:");
                                    fileMissing.ForEach(x => DebugWriteLine($"    {x.ReferencePath}"));
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
                Environment.Exit((int)exitCode);
            }
            catch (InternalError e)
            {
                ErrorWriteLine(Environment.NewLine + "Internal Error:");
                LogWriteLine(Util.GetCompleteExceptionMessage(e, "\t"));
                exitCode = ExitCode.InternalError;
                Environment.Exit((int)exitCode);
            }
#if !DEBUG // Use this to catch right away if an exception throw
            catch (Exception e)
            {
                LogWriteLine(Environment.NewLine + "Exception Error:");
                LogWriteLine(Util.GetCompleteExceptionMessage(e, "\t"));
                exitCode = ExitCode.UnknownError;
            }
#endif
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

            LogWriteLine(@"{0} errors, {1} warnings", s_errorCount, s_warningCount);
            if (s_errorCount != 0)
            {
                if (Debugger.IsAttached)
                {
                    LogWriteLine("Please look at the errors.");
                    Debugger.Break();
                }
            }

            // returning exit code and error count separately because they can result in an exit code of 0 if they are added together.
            if (s_errorCount != 0)
                return s_errorCount;
            return (int)exitCode;
        }

        private static void LogPlatformImplementationExtensionRegistered(object sender, PlatformImplementationExtensionRegisteredEventArgs e)
        {
            LogWriteLine("Loaded platform extension {0} (Found {1} implementation class{2})", e.ExtensionAssembly.Location, e.Interfaces.Count, e.Interfaces.Count > 1 ? "es" : string.Empty);
        }

        private static void GenerateAll(BuildContext.BaseBuildContext buildContext, Argument parameters)
        {
            if (parameters.GenerateDebugSolution)
            {
                using (Builder builder = CreateBuilder(buildContext, parameters, true, true))
                {
                    LogWriteLine("Generate debug solution...");

                    var outputs = builder.Generate();
                    foreach (var output in outputs)
                    {
                        if (output.Value.Exception != null)
                            throw new Error(output.Value.Exception, "Error encountered while generating {0}", output.Key);
                    }

                    LogWriteGenerateResults(outputs);
                    Console.WriteLine("");
                }
            }

            using (Builder builder = CreateBuilder(buildContext, parameters, true))
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
                    var outputs = builder.Generate();
                    foreach (var output in outputs)
                    {
                        if (output.Value.Exception != null)
                            throw new Error(output.Value.Exception, "Error encountered while generating {0}", output.Key);
                    }

                    if (parameters.DumpDependency)
                        Sharpmake.DependencyTracker.Instance.DumpGraphs(outputs);

                    LogWriteGenerateResults(outputs);
                }
            }

            LogWriteLine("  time: {0:0.00} sec.", (DateTime.Now - s_startTime).TotalSeconds);
            LogWriteLine("  completed on {0}.", DateTime.Now);
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

            PackageReferences.LogPackagesVersionsDiscrepancy();

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
                Program.GetGeneratorsManager);

            // Allow message log from builder.
            builder.EventOutputError += ErrorWrite;
            builder.EventOutputWarning += WarningWrite;
            builder.EventOutputMessage += LogWrite;
            builder.EventOutputDebug += DebugWrite;

            if (parameters.ProfileOutput)
                builder.EventOutputProfile += LogWrite;

            // generate debug solution
            if (generateDebugSolution)
            {
                DebugProjectGenerator.GenerateDebugSolution(parameters.Sources, builder.Arguments);
                builder.BuildProjectAndSolution();
                return builder;
            }

            switch (parameters.Input)
            {
                case Argument.InputType.File:
                    {
                        try
                        {
                            builder.LoadSharpmakeFiles(parameters.Sources);
                        }
                        catch (Exception)
                        {
                            builder.Dispose();
                            throw;
                        }
                    }
                    break;
                case Argument.InputType.Assembly:
                    {
                        try
                        {
                            builder.LoadAssemblies(parameters.Assemblies);
                        }
                        catch (Exception)
                        {
                            builder.Dispose();
                            throw;
                        }
                    }
                    break;
                default:
                    builder.Dispose();
                    throw new Error("sharpmake input missing, use /sources() or /assemblies()");
            }

            return builder;
        }

        private static void RecursivePrintMethodInfo(Analyzer.ConfigureMethodInfo method, ISet<string> set, int nested = 0)
        {
            ++nested;
            foreach (var dependent in method.Dependents.OrderBy(x => x.Method.ToString()))
            {
                set.Add(String.Format("[{0}]->[{1}]", dependent, method));

                if (dependent.Dependents.Any())
                    RecursivePrintMethodInfo(dependent, set, nested);
            }
        }

        public static IGeneratorManager GetGeneratorsManager()
        {
            return new GeneratorManager();
        }
    }
}
