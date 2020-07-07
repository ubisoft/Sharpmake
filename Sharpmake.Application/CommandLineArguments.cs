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
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Sharpmake.Application
{
    public static partial class Program
    {
        public class Argument
        {
            [Serializable]
            public class Error : Exception
            {
                public Error(string message, params object[] args)
                    : base(string.Format(message, args))
                { }

                protected Error(SerializationInfo info, StreamingContext context)
                    : base(info, context)
                { }
            }

            public enum InputType
            {
                File,
                Assembly,
                Undefined
            }

            public string[] Sources = new string[0];
            public string[] Assemblies = new string[0];
            public HashSet<string> Defines = new HashSet<string>();
            public InputType Input = InputType.Undefined;
            public bool Exit = false;
            public bool BlobOnly = false;
            public bool CleanBlobsOnly = false;
            public bool Multithreaded = true;
            public bool RegexMatchCacheEnabled = true;
            // Default capacity based on a big project numbers
            public int RegexMatchCacheInitialCapacity = (1 << 20) + 1;
            public bool SkipInvalidPath = false;
            public bool DebugLog = false;
            public bool Debug = false;
            public bool Diagnostics = false;
            public bool WriteFiles = true;
            public bool DumpDependency = false;
            private bool _testOptionValid = true;
            public bool ProfileOutput = false;
            internal TestOptions TestOption;
            internal bool RegressionDiff = true;
            public DirectoryInfo OutputDirectory;
            public DirectoryInfo ReferenceDirectory;
            public DirectoryInfo RemapRoot;
            public string MutexSuffix = string.Empty;
            public bool GenerateDebugSolution = false;
            public string DebugSolutionStartArguments = string.Empty;

            [CommandLine.Option("sources", @"sharpmake sources files: ex: /sources( ""project1.sharpmake"", ""..\..\project2.sharpmake"" )")]
            public void SetSources(params string[] files)
            {
                Sources = ValidateFiles(files);
                DebugWriteLine("input sources: ");
                Array.ForEach(Sources, (string source) => DebugWriteLine("  " + source));
            }

            [CommandLine.Option("assemblies")]
            public void SetAssembly(params string[] files)
            {
                Assemblies = ValidateFiles(files);
                DebugWriteLine("input assemblies: ");
                Array.ForEach(Assemblies, (string assembly) => DebugWriteLine("  " + assembly));
            }

            [CommandLine.Option("defines", @"sharpmake compilation defines: ex: /defines( ""SHARPMAKE_0_8_0"", ""GITLAB"" )")]
            public void SetDefines(params string[] defines)
            {
                Defines = ValidateDefines(defines);
                DebugWriteLine("compilation defines: ");
                foreach (string define in Defines)
                    DebugWriteLine("  " + define);
            }

            [CommandLine.Option("projectlogfiles", @"log files contained in a project for debug purpose: ex: /projectlogfiles( ""s:\p4\ac\dev\sharpmake\projects\win32\system\system.vcproj"" )")]
            public void ProjectLogFiles(string projectFile)
            {
                Tools.ProjectLogFiles(projectFile);
                Exit = true;
            }

            [CommandLine.Option("blobonly", @"Only generate blob and work blob files: ex: /blobonly")]
            public void CommandLineBlobOnly()
            {
                BlobOnly = true;
            }

            [CommandLine.Option("breakintodebugger", @"Trigger a debugger break at the beginning of the program.")]
            public void CommandLineBreakIntoDebugger()
            {
                // Validated in the main for priority
            }

            [CommandLine.Option("cleanblobsonly", @"Only clean blob and work blob files: ex: /cleanblobsonly")]
            public void CommandLineCleanBlobsOnly()
            {
                CleanBlobsOnly = true;
            }

            [CommandLine.Option("multithreaded", @"Run multithreaded sharpmake: ex: /multithreaded(<true|false>)")]
            public void CommandLineMultithreaded(bool value)
            {
                Multithreaded = value;
            }

            [CommandLine.Option("regexMatchCache", @"Enables/disables regex match cache optimization. Might improve performance on large projects. Enabled by default. ex: /regexMatchCache(false)")]
            public void CommandLineRegexMatchCacheEnabled(bool value)
            {
                RegexMatchCacheEnabled = value;
            }

            [CommandLine.Option("regexMatchCacheInitialCapacity", @"Initial capacity of regex match cache. Should be set to a value higher or equal to the size which the cache is expected to reach over a run, otherwise performance might suffer. ex: /regexMatchCacheInitialCapacity(1048577)")]
            public void CommandLineRegexMatchCacheInitialCapacity(int value)
            {
                RegexMatchCacheInitialCapacity = value;
            }

            [CommandLine.Option("DumpDependency", @"Dump projects dependencies in dot format: ex: /DumpDependency")]
            public void CommandLineDumpDependency()
            {
                DumpDependency = true;
            }

            [CommandLine.Option("debuglog", @"Write debug log ( slow ): ex: /debuglog(<true|false>)")]
            public void CommandLineDebugLog(bool value)
            {
                DebugLog = value;
            }

            [CommandLine.Option("profileoutput", @"Write profiling information ( slow ): ex: /profileoutput(<true|false>)")]
            public void CommandLineProfileOutput(bool value)
            {
                ProfileOutput = value;
            }

            [CommandLine.Option("diagnostics", @"Output more errors and warnings (slow): ex: /diagnostics")]
            public void CommandLineDiagnostics()
            {
                Diagnostics = true;
            }

            [CommandLine.Option("debug", @"Run debug mode (implicit verbose and require a key to close console): ex: /debug")]
            public void CommandLineDebug()
            {
                Debug = true;
            }

            [CommandLine.Option("sharpmakemutexsuffix", @"Allow custom mutex name suffix. Useful to debug concurrently multiple sharpmake running from different branches. Ex: /sharpmakemutexsuffix(""Name"")")]
            public void CommandLineSharpmakeMutexSuffix(string name)
            {
                MutexSuffix = name;
            }

            [CommandLine.Option("skipinvalidpath", @"Skip invalid paths when resolving projects \ solutions : ex: /skipinvalidpath(<true|false>)")]
            public void CommandLineSkipInvalidPath(bool value)
            {
                SkipInvalidPath = value;
            }

            [CommandLine.Option("test", @"Validates .sharpmake input so it respect a minimal coding standard.
Regression: tests if the dir provided in output is equal to the reference dir after a generation. returns -1 if different
QuickConfigure: tests if the configure methods are reversible. returns -1 if it is not reversible
Configure: tests if the configure methods are reversible, track the problems. return -1 if it is not reversible
(validates configure order): ex: /test(<""Regression""|""QuickConfigure""|""Configure"">)")]
            public void CommandLineStrict(string option)
            {
                _testOptionValid = Enum.TryParse(option, out TestOption);
            }

            [CommandLine.Option("regressionDiff", @"Use diff tool if found to show regression differences (enabled by default): ex: /regressionDiff(<true|false>)")]
            public void CommandLineRegressionDiff(bool value)
            {
                RegressionDiff = value;
            }

            [CommandLine.Option("writefiles", @"Sets if the generated files should be written or not. Default value: true. ex: /writefiles(<true|false>)")]
            public void CommandLineWriteFiles(bool value)
            {
                WriteFiles = value;
            }

            [CommandLine.Option("filewritessecondarypath", @"Alternate path where to save modified files. This can be used to generate a reference directory when we want to compare changes made by sharpmake for example when upgrading it.")]
            public void CommandLineFileWritesSecondaryPath(string filePath)
            {
                BuildContext.GenerateAll.s_fileWritesSecondaryPath = filePath;
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);
            }

            [CommandLine.Option("outputdir", @"Redirect solutions and projects output folder.
If this is set, autocleanup will be ignored.
It defines where the files should be written: ex: /outputdir(""C:\outputdirectory"")")]
            public void CommandLineSetOutput(string output)
            {
                OutputDirectory = new DirectoryInfo(output);
                DebugWriteLine("output directory : " + OutputDirectory.FullName);
            }

            [CommandLine.Option("referencedir", @"solutions and projects reference folder.
Must be grouped with /outputdir(...) and /test(...).
It defines what should the files look like: ex: /referencedir(""C:\referencedirectory"")")]
            public void CommandLineSetReference(string reference)
            {
                ReferenceDirectory = new DirectoryInfo(reference);
                DebugWriteLine("reference directory : " + ReferenceDirectory.FullName);
            }

            [CommandLine.Option("remaproot", @"Path to remove from the beginning of default output paths when remapping.
Used when /outputdir is set.
ex: /remaproot(""C:\p4ws\projectRoot\"")")]
            public void CommandLineSetRemapRoot(string remapRoot)
            {
                RemapRoot = new DirectoryInfo(remapRoot);
                DebugWriteLine("remap root directory : " + RemapRoot.FullName);
            }

            [CommandLine.Option("fakesourcedirfile", @"path to a file containing the list of files in the source tree
This list will be used instead of the real source path
ex: /fakesourcedirfile( ""files.txt"" ")]
            public void CommandLineFakeSourceDirFile(string fakeSourceDirFile)
            {
                string fakeSourceDirFileFullPath = Path.GetFullPath(fakeSourceDirFile);
                if (File.Exists(fakeSourceDirFileFullPath))
                {
                    DebugWriteLine("fake source directory file:" + Environment.NewLine + fakeSourceDirFileFullPath);

                    // Detect "Everything" file format
                    bool everythingFileFormat = false;
                    string extension = Path.GetExtension(fakeSourceDirFileFullPath);
                    everythingFileFormat = extension == ".efu";

                    FileInfo fakeSourceDirFileInfo = new FileInfo(fakeSourceDirFileFullPath);
                    using (StreamReader projectFileStream = fakeSourceDirFileInfo.OpenText())
                    {
                        if (everythingFileFormat)
                        {
                            // Skip the first line, which is the legend:
                            // Filename,Size,Date Modified, Date Created,Attributes
                            projectFileStream.ReadLine();

                            // Get the folder prefix of everything on the second line
                            string folder = projectFileStream.ReadLine().Split(',')[0].Trim('"');
                            Util.FakePathPrefix = folder;

                            string line = projectFileStream.ReadLine();
                            while (line != null)
                            {
                                if (line != string.Empty)
                                {
                                    var splitLine = line.Split(',');
                                    uint attributes;
                                    bool success = uint.TryParse(splitLine[4], out attributes);
                                    if (success && (attributes & 0x10) != 0x10) // 16 is directory
                                    {
                                        string filePath = splitLine[0].Substring(folder.Length + 1).TrimEnd('"');
                                        int size;
                                        Util.AddNewFakeFile(filePath, int.TryParse(splitLine[1], out size) ? size : 0);
                                    }
                                }
                                line = projectFileStream.ReadLine();
                            }
                        }
                        else
                        {
                            string line = projectFileStream.ReadLine();
                            while (line != null)
                            {
                                if (line != string.Empty)
                                {
                                    string filePath = Util.PathMakeStandard(line);
                                    Util.AddNewFakeFile(filePath, 0);
                                }
                                line = projectFileStream.ReadLine();
                            }
                        }
                    }

                    DebugWriteLine("found " + Util.CountFakeFiles() + " fake files");
                }
                else
                {
                    throw new Error("Fake source directory file cannot be found! Please check the command line.");
                }
            }

            [CommandLine.Option("verbose", @"Writes time and action information during progress: ex:/verbose")]
            public void CommandLineVerbose()
            {
                // Validated in the main for priority
            }

            [CommandLine.Option("help", @"Prints other arguments (name and description): ex:/help")]
            public void CommandLineHelp()
            {
                // Validated in the main for priority
            }

            [CommandLine.Option("generateDebugSolution", @"Generate debug solution.: /generateDebugSolution")]
            public void CommandLineGenerateDebugSolution()
            {
                GenerateDebugSolution = true;
            }

            [CommandLine.Option("debugSolutionStartArguments", @"Adds arguments to the debug commandline 
of the project generated by /generateDebugSolution. ex: /debugSolutionStartArguments(""/diagnostics"")")]
            public void CommandLineDebugSolutionStartArguments(string arguments)
            {
                DebugSolutionStartArguments = arguments;
            }

            [CommandLine.Option("forcecleanup", @"Path to an autocleanup db.
If this is set, all the files listed in the DB will be removed, and sharpmake will exit.
ex: /forcecleanup( ""tmp/sharpmakeautocleanupdb.bin"" ")]
            public void CommandLineForceCleanup(string autocleanupDb)
            {
                if (!File.Exists(autocleanupDb))
                    throw new FileNotFoundException(autocleanupDb);

                Util.s_forceFilesCleanup = true;
                Util.s_overrideFilesAutoCleanupDBPath = autocleanupDb;

                Util.ExecuteFilesAutoCleanup();

                Exit = true;
            }

            public void Validate()
            {
                if (Assemblies.Length == 0 && Sources.Length == 0)
                    throw new Error("command line error, input missing, use /sources() or /assemblies()");

                if (Assemblies.Length != 0 && Sources.Length != 0)
                    throw new Error("command line error, parameters must not be used together: /sources() and /assemblies()");

                if (!_testOptionValid)
                    throw new Error("command line error, invalid test option given. See help for more details");

                if (TestOption == TestOptions.Regression)
                {
                    if (OutputDirectory == null || ReferenceDirectory == null)
                        throw new Error(@"command line error, /test(""Regression"") must define a reference and an output directory (/referencedir() and /outputdir())");

                    if (!ReferenceDirectory.Exists)
                        throw new Error("command line error, reference directory doesn't exist: {0}", ReferenceDirectory);
                }
                else if (ReferenceDirectory != null && TestOption == TestOptions.None)
                {
                    throw new Error("command line error, /referencedir can't be used without a /test()");
                }

                if (Sources.Length != 0)
                    Input = InputType.File;

                if (Assemblies.Length != 0)
                    Input = InputType.Assembly;
            }

            // Will check the presence of the given files and return the list with their full path
            public static string[] ValidateFiles(string[] files)
            {
                string[] fullPathFiles = new string[files.Length];
                for (int i = 0; i < files.Length; ++i)
                {
                    fullPathFiles[i] = Path.GetFullPath(files[i]);

                    if (!File.Exists(fullPathFiles[i]))
                        throw new Error("error: input file not found: {0}", files[i]);
                }
                return fullPathFiles;
            }

            // Will check that the given compilation defines are valid
            public static HashSet<string> ValidateDefines(string[] defines)
            {
                Regex defineValidationRegex = new Regex(@"^\w+$");

                HashSet<string> uniqueDefines = new HashSet<string>();
                foreach (string define in defines)
                {
                    if (!defineValidationRegex.IsMatch(define))
                        throw new Error("error: invalid define '{0}', a define must be a single word", define);
                    if (uniqueDefines.Contains(define))
                        throw new Error("error: define '{0}' already defined", define);
                    uniqueDefines.Add(define);
                }
                return uniqueDefines;
            }
        }
    }
}
