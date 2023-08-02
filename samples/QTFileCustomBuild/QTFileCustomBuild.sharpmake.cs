// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sharpmake;
using ProjConfiguration = Sharpmake.Project.Configuration;

namespace QTFileCustomBuild
{
    public class AdditionalDefinition
    {
        public Sharpmake.Platform Platform;
        public Sharpmake.DevEnv DevEnv;
        public Strings Defines;

        public AdditionalDefinition(Sharpmake.Platform platform, Sharpmake.DevEnv dev, params string[] defines)
        {
            Platform = platform;
            DevEnv = dev;
            Defines = new Strings(defines);
        }
    }

    public class QtSharpmakeMocTool
    {
        // Mapping of target name to all the files that should generate a moc call.
        public Dictionary<ProjConfiguration, List<MocSourceAndTargetFile>> MocTargetsPerConfiguration = new Dictionary<ProjConfiguration, List<MocSourceAndTargetFile>>();
        // Mapping of target name to all the files that should generate a rcc call.
        public Dictionary<ProjConfiguration, List<RccSourceAndTargetFile>> RccTargetsPerConfiguration = new Dictionary<ProjConfiguration, List<RccSourceAndTargetFile>>();
        // Mapping of target name to all the files that should generate a uic call.
        public Dictionary<ProjConfiguration, List<UicSourceAndTargetFile>> UicTargetsPerConfiguration = new Dictionary<ProjConfiguration, List<UicSourceAndTargetFile>>();
        // Files that should be moc'd but should not be compiled alone (they will be included in another cpp file).
        public Strings ExcludeMocFromCompileRegex = new Strings();
        // Files that should not be moc'd, skip scanning them.   They may have a Q_OBJECT, but it's fake.
        public Strings ExcludeMocRegex = new Strings();
        // A way to give defines to moc.
        public List<AdditionalDefinition> AdditionalDefines = new List<AdditionalDefinition>();

        public QtSharpmakeMocTool()
        {
            AdditionalDefines.Add(new AdditionalDefinition(Sharpmake.Platform.win64 | Sharpmake.Platform.win32, Sharpmake.DevEnv.vs2017, "WIN32", "_MSC_VER=1910"));
            AdditionalDefines.Add(new AdditionalDefinition(Sharpmake.Platform.win64 | Sharpmake.Platform.win32, Sharpmake.DevEnv.vs2019, "WIN32", "_MSC_VER=1920"));
        }

        // Stores the source file and target file of a moc operation.
        public class MocSourceAndTargetFile : ProjConfiguration.CustomFileBuildStep
        {
            // Relative path of the input file.
            public string SourceFile;
            // Intermediate file used for a custom build step to produce the output from the input.
            public string IntermediateFile;
            // True if source file, false if header file.
            public bool IsCPPFile;
            // True if the output target file should never be compiled (For !IsCPPFile)
            public bool TargetFileNotCompiled;
            // List of includes to use.
            public Strings IncludePaths = new Strings();
            // List of force-includes.  The order matters.
            public List<string> ForceIncludes = new List<string>();
            // Defines
            public string CombinedDefines = "";

            public MocSourceAndTargetFile(string targetName, string mocExe, string baseOutputFolder, string outputFolder, string sourceFile)
            {
                Executable = mocExe;
                SourceFile = sourceFile;
                KeyInput = SourceFile;
                IsCPPFile = sourceFile.EndsWith(".cpp", StringComparison.InvariantCultureIgnoreCase);
                TargetFileNotCompiled = false;
                if (IsCPPFile)
                {
                    // Put this one up one level, as it's just a boot strap file.
                    IntermediateFile = baseOutputFolder + Path.GetFileNameWithoutExtension(sourceFile) + ".inl";
                    Output = outputFolder + Path.GetFileNameWithoutExtension(sourceFile) + ".moc";
                    // BFF only.
                    Filter = ProjectFilter.BFFOnly;
                }
                else
                {
                    ForceIncludes.Add(sourceFile);
                    Output = outputFolder + "moc_" + Path.GetFileNameWithoutExtension(sourceFile) + ".cpp";
                }

                Description = string.Format("Moc {0} {1}", targetName, Path.GetFileName(sourceFile));
                ExecutableArguments = "[input] -o [output]";
            }

            // Makes a Vcxproj rule from a BFFOnly rule.
            protected MocSourceAndTargetFile(MocSourceAndTargetFile reference)
            {
                Filter = ProjectFilter.ExcludeBFF;
                Executable = reference.Executable;
                // Input is SourceFile not KeyInput
                ExecutableArguments = " -o [output]";
                // Input is the intermediate file.
                KeyInput = reference.IntermediateFile;
                // We also depend on the actual input file.
                AdditionalInputs.Add(reference.KeyInput);
                Output = reference.Output;
                Description = reference.Description;

                SourceFile = reference.SourceFile;
                IntermediateFile = reference.IntermediateFile;
                IsCPPFile = reference.IsCPPFile;
                TargetFileNotCompiled = reference.TargetFileNotCompiled;

                IncludePaths.AddRange(reference.IncludePaths);
                ForceIncludes.AddRange(reference.ForceIncludes);
                CombinedDefines = reference.CombinedDefines;
            }

            // We get built too late to handle the initial resolve (as we need the files built afterwards), but we get the other two events.
            public override ProjConfiguration.CustomFileBuildStepData MakePathRelative(Resolver resolver, Func<string, bool, string> MakeRelativeTool)
            {
                var relativeData = base.MakePathRelative(resolver, MakeRelativeTool);

                if (Filter == ProjectFilter.ExcludeBFF)
                {
                    // Need to use the right input.
                    relativeData.ExecutableArguments = MakeRelativeTool(SourceFile, true) + relativeData.ExecutableArguments;
                }

                // These are command line relative.
                Strings RelativeIncludePaths = new Strings();
                foreach (string key in IncludePaths)
                    RelativeIncludePaths.Add(MakeRelativeTool(key, true));
                // These should be compiler relative instead of command line relative, but generally they're the same.
                var RelativeForceIncludes = new System.Text.StringBuilder(ForceIncludes.Count * 64);
                foreach (string key in ForceIncludes)
                {
                    RelativeForceIncludes.Append(" -f");
                    RelativeForceIncludes.Append(MakeRelativeTool(key, false));
                }

                RelativeIncludePaths.InsertPrefix("-I");
                relativeData.ExecutableArguments = CombinedDefines + " " + RelativeIncludePaths.JoinStrings(" ") + " " + RelativeForceIncludes.ToString() + " " + relativeData.ExecutableArguments;

                return relativeData;
            }
        }

        // Needed for vcx when the input is a source file, we need to specify a different rule based on an intermediate file.
        // MocSourceAndTargetFile already setups up this data, we just need a non-bff rule.
        public class MocVcxprojBuildStep : MocSourceAndTargetFile
        {
            public MocVcxprojBuildStep(MocSourceAndTargetFile reference)
                : base(reference)
            {
            }
        }

        // Stores the source file and target file of a rcc operation
        public class RccSourceAndTargetFile : ProjConfiguration.CustomFileBuildStep
        {
            public RccSourceAndTargetFile(string targetName, string rccExe, string outputFolder, string sourceFile)
            {
                Executable = rccExe;
                KeyInput = sourceFile;
                string ResourceName = Path.GetFileNameWithoutExtension(sourceFile);
                Output = outputFolder + "qrc_" + ResourceName + ".cpp";

                Description = string.Format("Rcc {0} {1}", targetName, Path.GetFileName(sourceFile));
                ExecutableArguments = "-name " + ResourceName + " [input] -o [output]";
            }
        }

        // Stores the source file and target file of a uic operation
        public class UicSourceAndTargetFile : ProjConfiguration.CustomFileBuildStep
        {
            public UicSourceAndTargetFile(string targetName, string uicExe, string outputFolder, string sourceFile)
            {
                Executable = uicExe;
                KeyInput = sourceFile;
                Output = outputFolder + "ui_" + Path.GetFileNameWithoutExtension(sourceFile) + ".h";
                Description = string.Format("Uic {0} {1}", targetName, Path.GetFileName(sourceFile));
                ExecutableArguments = "[input] -o [output]";
            }
        }

        private bool FileIsPrecompiledHeader(string file, ProjConfiguration conf)
        {
            return (conf.PrecompHeader != null && file.EndsWith(conf.PrecompHeader, StringComparison.InvariantCultureIgnoreCase))
                     || (conf.PrecompSource != null && file.EndsWith(conf.PrecompSource, StringComparison.InvariantCultureIgnoreCase));
        }

        private static int GetIndexMatchedAtEnd(string fileBuffer, string stringToFind)
        {
            int len = stringToFind.Length;
            int indexOfMatch = len > 0 ? len - 1 : 0;
            for (; indexOfMatch > 0; --indexOfMatch)
            {
                if (fileBuffer.EndsWith(stringToFind.Substring(0, indexOfMatch)))
                    return indexOfMatch;
            }
            return 0;
        }

        private async Task<bool> FileContainsQObject(string file)
        {
            try
            {
                const int numBytesPerPage = 0x1000;
                using (StreamReader sourceStream = new StreamReader(new FileStream(file,
                       FileMode.Open, FileAccess.Read, FileShare.Read,
                       bufferSize: numBytesPerPage, useAsync: true)))
                {
                    string[] stringsToFind = new string[2];
                    stringsToFind[0] = "Q_OBJECT";
                    stringsToFind[1] = "Q_GADGET";
                    int[] fractionsMatched = new int[2];
                    fractionsMatched[0] = 0;
                    fractionsMatched[1] = 0;

                    char[] buffer = new char[numBytesPerPage];
                    int numRead;
                    while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        string text = new string(buffer);
                        // If we partially matched the previous block, see if we match the rest.
                        for (int i = 0; i < fractionsMatched.Length; ++i)
                        {
                            if (fractionsMatched[i] != 0)
                            {
                                if (text.StartsWith(stringsToFind[i].Substring(fractionsMatched[i])))
                                    return true;
                            }

                            if (text.Contains(stringsToFind[i]))
                                return true;
                            fractionsMatched[i] = GetIndexMatchedAtEnd(text, stringsToFind[i]);
                        }
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("While looking at file {0} encountered exception {1}, not mocing the file!", file, ex.Message);
                return false;
            }
        }

        // Call this from Project::ExcludeOutputFiles() to find the list of files we need to moc.
        // This is after resolving files, but before filtering them, and before they get mapped to
        // configurations, so this is a good spot to add additional files.
        public void GenerateListOfFilesToMoc(Project project, string QTExecFolder)
        {
            string mocExe = QTExecFolder + "moc.exe";
            string rccExe = QTExecFolder + "rcc.exe";
            string uicExe = QTExecFolder + "uic.exe";

            // Filter all the files by the filters we've already specified, so we don't moc a file that's excluded from the solution.
            RegexOptions filterOptions = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
            List<Regex> filters = project.SourceFilesExcludeRegex.Select(filter => new Regex(filter, filterOptions)).ToList();
            filters.AddRange(ExcludeMocRegex.Select(filter => new Regex(filter, filterOptions)));

            var preFilteredFiles = project.ResolvedSourceFiles.Where(file => !filters.Any(filter => filter.IsMatch(file)) && !project.Configurations.Any(conf => FileIsPrecompiledHeader(file, conf))).ToList();

            // Async load all the source files and look for Q_OBJECT that we want to keep.
            var answerSetTask = Task.WhenAll(preFilteredFiles.Select(async file => new { file = file, runMoc = await FileContainsQObject(file) }));
            // Compile a list of qrc and ui files.
            Strings qrcFiles = new Strings(preFilteredFiles.Where(file => file.EndsWith(".qrc", StringComparison.InvariantCultureIgnoreCase)));
            Strings uiFiles = new Strings(preFilteredFiles.Where(file => file.EndsWith(".ui", StringComparison.InvariantCultureIgnoreCase)));
            // Wait for the moc files.
            answerSetTask.Wait();
            var filterPass = answerSetTask.Result;
            // These are the files we want to moc.
            Strings FilteredResolvedSourceFiles = new Strings(filterPass.Where(result => result.runMoc).Select(result => result.file));

            // Compile a list of files where we don't want to compile the moc output.
            List<Regex> filesToExclude = ExcludeMocFromCompileRegex.Select(filter => new Regex(filter, filterOptions)).ToList();


            foreach (ProjConfiguration conf in project.Configurations)
            {
                // Setup exclusions.
                string QTMocOutputBase = Path.GetDirectoryName(conf.IntermediatePath);
                string targetName = conf.Target.Name;
                string outputFolder = QTMocOutputBase + @"\qt\" + targetName.ToLowerInvariant() + @"\";

                // We make the current output folder included directly so you can use the same #include directive to get the correct cpp file.
                conf.IncludePrivatePaths.Add(outputFolder);
                // Also include the project file folder, since the moc tool generates includes from this location.
                conf.IncludePrivatePaths.Add(conf.ProjectPath);

                // We need to exclude the generation files folder from the build on all targets except our own.
                string rootFolderForRegex = Util.GetCapitalizedPath(conf.ProjectPath);
                string outputRegex = Util.PathGetRelative(rootFolderForRegex, outputFolder);
                outputRegex = outputRegex.Replace("..\\", "").Replace("\\", "\\\\") + @"\\";
                foreach (ProjConfiguration confToExclude in project.Configurations)
                {
                    if (confToExclude == conf || confToExclude.ProjectFullFileNameWithExtension != conf.ProjectFullFileNameWithExtension)
                        continue;
                    confToExclude.SourceFilesBuildExcludeRegex.Add(outputRegex);
                }

                // Build a list of all files to moc in this configuration.
                var mocTargets = new List<MocSourceAndTargetFile>();
                foreach (string file in FilteredResolvedSourceFiles)
                {
                    var target = new MocSourceAndTargetFile(targetName, mocExe, outputFolder, outputFolder, file);
                    if (filesToExclude.Any(filter => filter.IsMatch(file)))
                    {
                        target.TargetFileNotCompiled = true;
                    }
                    mocTargets.Add(target);
                    if (target.IsCPPFile)
                    {
                        mocTargets.Add(new MocVcxprojBuildStep(target));
                    }
                }
                if (mocTargets.Count > 0)
                {
                    MocTargetsPerConfiguration.Add(conf, mocTargets);
                }

                if (qrcFiles.Count > 0)
                {
                    RccTargetsPerConfiguration.Add(conf, qrcFiles.Select(file => new RccSourceAndTargetFile(targetName, rccExe, outputFolder, file)).ToList());
                }
                if (uiFiles.Count > 0)
                {
                    UicTargetsPerConfiguration.Add(conf, uiFiles.Select(file => new UicSourceAndTargetFile(targetName, uicExe, outputFolder, file)).ToList());
                }
            }

            // Add all the new source files to the project file.
            foreach (var values in MocTargetsPerConfiguration)
            {
                foreach (var target in values.Value)
                {
                    // We only need to include outputs that have build steps.  For source files, that's the intermediate file, for
                    // header files, that's the target file, if it wasn't excluded.
                    values.Key.CustomFileBuildSteps.Add(target);
                    if (target.IsCPPFile)
                        project.ResolvedSourceFiles.Add(target.IntermediateFile);
                    else if (!target.TargetFileNotCompiled)
                        project.ResolvedSourceFiles.Add(target.Output);
                }
            }

            foreach (var values in RccTargetsPerConfiguration)
            {
                var conf = values.Key;
                foreach (var target in values.Value)
                {
                    conf.CustomFileBuildSteps.Add(target);
                    // disable precomp in the files generated by rcc since they lack its include
                    conf.PrecompSourceExclude.Add(target.Output);
                    project.ResolvedSourceFiles.Add(target.Output);
                }
            }

            foreach (var values in UicTargetsPerConfiguration)
            {
                foreach (var target in values.Value)
                {
                    values.Key.CustomFileBuildSteps.Add(target);
                    // uic files generate header files - we don't need to run a build step on them, so don't include them in the vcxproj listing.
                    //project.ResolvedSourceFiles.Add(target.Output);
                }
            }
        }

        private void CreateIntermediateFile(string sourceFile, string intermediateFile)
        {
            // Create the intermediate file if it doesn't already exist.   Visual studio seems to ignore the custom build step unless the file already exists.
            if (!File.Exists(intermediateFile))
            {
                try
                {
                    string directory = Path.GetDirectoryName(intermediateFile);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    if (!File.Exists(intermediateFile))
                    {
                        StreamWriter writer = File.CreateText(intermediateFile);
                        writer.WriteLineAsync(sourceFile).ContinueWith(a => writer.Close());
                        System.Console.WriteLine("  Created {0}", intermediateFile);
                    }
                }
                catch (IOException e)
                {
                    // Sharing violation is fine, it means we're about to create the file on another thread.
                    const int SharingViolation = 0x20;
                    if ((e.HResult & 0xFFFF) != SharingViolation)
                    {
                        Console.WriteLine("Unable to generate intermediate file {0}: {1}", intermediateFile, e);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to generate intermediate file {0}: {1}", intermediateFile, e);
                }
            }
        }

        private void GenerateMocFileStepsForConfiguration(ProjConfiguration conf)
        {
            // Build a set of custom build steps from the source-target pairs.
            List<MocSourceAndTargetFile> mocTargets;
            if (!MocTargetsPerConfiguration.TryGetValue(conf, out mocTargets))
                return;

            // Copy the defines and add -D in front of them.
            Strings confDefines = new Strings(conf.Defines.Select(define => define.Replace(" ", "")));
            foreach (var additionalDefine in AdditionalDefines)
            {
                if ((conf.Target.GetPlatform() & additionalDefine.Platform) != 0 && (conf.Target.GetFragment<DevEnv>() & additionalDefine.DevEnv) != 0)
                {
                    confDefines.AddRange(additionalDefine.Defines);
                }
            }
            confDefines.InsertPrefix("-D");
            string combinedDefines = confDefines.JoinStrings(" ");

            // Combine all the different includes into a single string pool.
            Strings confIncludes = new Strings(conf.IncludePaths);
            confIncludes.AddRange(conf.DependenciesIncludePaths);
            confIncludes.AddRange(conf.IncludePrivatePaths);
            // Quote the include strings, if need be.
            List<string> includeValues = confIncludes.Values;
            foreach (string path in includeValues)
            {
                if (path.Contains(' '))
                {
                    confIncludes.UpdateValue(path, "\"" + path + "\"");
                }
            }

            string precompiledHeader = null;

            // Build the string we need to pass to moc for all calls.
            if (conf.PrecompHeader != null)
            {
                // If we have a precompiled header, we need the new cpp file to include this also.
                // Technically we don't need to do this if the file is in ExcludeMocFromCompileRegex
                precompiledHeader = conf.PrecompHeader;
            }

            // Apply these settings to all Moc targets.
            foreach (var target in mocTargets)
            {
                target.CombinedDefines = combinedDefines;
                target.IncludePaths.AddRange(confIncludes);
                // Precompiled header must go first in the force include list.
                if (!target.IsCPPFile && precompiledHeader != null)
                    target.ForceIncludes.Insert(0, precompiledHeader);
                // VCX CPP file should create the intermediate file.
                else if (target.Filter == ProjConfiguration.CustomFileBuildStepData.ProjectFilter.ExcludeBFF)
                    CreateIntermediateFile(target.SourceFile, target.KeyInput);
            }
        }

        // Call this in Project::PostLink().   We will build a list of custom build steps based on the resolved includes and defines.
        // At this point all of our includes and defines have been resolved, so now we can compute the arguments to moc.
        public void GenerateMocFileSteps(Project project)
        {
            foreach (ProjConfiguration conf in project.Configurations)
            {
                // Compute all the define and include parameters for this configuration.
                GenerateMocFileStepsForConfiguration(conf);
            }
        }
    }

    [Sharpmake.Generate]
    public class QTFileCustomBuildProject : Project
    {
        // Tool for generation moc commands.
        public QtSharpmakeMocTool mocTool;
        // Path the qt executables
        public string QTExeFolder;
        // Path to QT
        public string QTPath;

        protected override void ExcludeOutputFiles()
        {
            base.ExcludeOutputFiles();
            mocTool.GenerateListOfFilesToMoc(this, QTExeFolder);
        }

        // At this point all of our includes and defines have been resolved, so now we can compute the arguments to moc.
        public override void PostLink()
        {
            mocTool.GenerateMocFileSteps(this);
            base.PostLink();
        }

        public QTFileCustomBuildProject()
        {
            Name = "QTFileCustomBuild";
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase";
            QTPath = Globals.Qt5_Dir;
            QTExeFolder = @"[project.QTPath]\bin\";

            mocTool = new QtSharpmakeMocTool();
            mocTool.ExcludeMocFromCompileRegex.Add("floatcosanglespinbox.h");
            mocTool.ExcludeMocFromCompileRegex.Add("privatewidget.h");

            SourceFilesExtensions.Add(".qrc", ".ui");

            AddTargets(new Target(
                    Platform.win64,
                    DevEnv.vs2017,
                    Optimization.Debug | Optimization.Release | Optimization.Retail,
                    OutputType.Dll
            ));

            // Fast build fails regression tests because it embeds system and user paths, which aren't
            // the same on each user's machine.
            //AddTargets(new Target(
            //        Platform.win64,
            //        DevEnv.vs2017,
            //        Optimization.Debug | Optimization.Release | Optimization.Retail,
            //        OutputType.Dll,
            //        Blob.FastBuildUnitys,
            //        BuildSystem.FastBuild
            //));
        }

        [Configure()]
        public void Configure(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            if (target.BuildSystem == BuildSystem.FastBuild)
                conf.ProjectFileName += "_fast";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.Output = ProjConfiguration.OutputType.Exe;

            // if not set, no precompile option will be used.
            conf.PrecompHeader = "stdafx.h";
            conf.PrecompSource = "stdafx.cpp";

            conf.Defines.Add("_HAS_EXCEPTIONS=0");

            conf.Defines.Add("QT_SHARED");

            if (target.Optimization != Optimization.Debug)
            {
                conf.Defines.Add("QT_NO_DEBUG");
            }

            conf.IncludePaths.Add(Path.Combine(QTPath, "include"));
            conf.LibraryPaths.Add(Path.Combine(QTPath, "lib"));

            conf.LibraryFiles.Add(
                "Qt5Core",
                "Qt5Gui",
                "Qt5Widgets"
            );
        }

        [Configure(BuildSystem.FastBuild)]
        public void ConfigureFast(Configuration conf, Target target)
        {
            conf.IsFastBuild = true;
        }
    }

    [Sharpmake.Generate]
    public class QTFileCustomBuildSolution : Sharpmake.Solution
    {
        public QTFileCustomBuildSolution()
        {
            Name = "QTFileCustomBuild";

            AddTargets(new Target(
                    Platform.win64,
                    DevEnv.vs2017,
                    Optimization.Debug | Optimization.Release | Optimization.Retail,
                    OutputType.Dll
            ));

            // Fast build fails regression tests because it embeds system and user paths, which aren't
            // the same on each user's machine.
            //AddTargets(new Target(
            //        Platform.win64,
            //        DevEnv.vs2017,
            //        Optimization.Debug | Optimization.Release | Optimization.Retail,
            //        OutputType.Dll,
            //        Blob.FastBuildUnitys,
            //        BuildSystem.FastBuild
            //));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            if (target.BuildSystem == BuildSystem.FastBuild)
                conf.SolutionFileName += "_mf";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<QTFileCustomBuildProject>(target);
        }
    }

    public static class Globals
    {
        public static string Qt5_Dir;
    }

    public static class Main
    {
        private static void ConfigureQt5Directory()
        {
            Util.GetEnvironmentVariable("Qt5_Dir", @"[project.SharpmakeCsPath]\qt\5.9.2\msvc2017_64", ref Globals.Qt5_Dir, silent: true);
            Util.LogWrite($"Qt5_Dir '{Globals.Qt5_Dir}'");
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            ConfigureQt5Directory();

            FastBuildSettings.FastBuildMakeCommand = @"\tools\FastBuild\start-fbuild.bat";

            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2017, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_17763_0);

            arguments.Generate<QTFileCustomBuildSolution>();
        }
    }
}
