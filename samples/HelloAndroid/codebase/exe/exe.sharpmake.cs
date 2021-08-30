// Copyright (c) 2021 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.IO;
using Sharpmake;

namespace HelloAndroid
{
    [Sharpmake.Generate]
    public class ExeProject : CommonProject
    {
        public ExeProject()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "exe";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.Exe;

            conf.PrecompHeader = "stdafx.h";
            conf.PrecompSource = "stdafx.cpp";

            conf.AddPrivateDependency<StaticLib1Project>(target);
            conf.AddPrivateDependency<StaticLib2Project>(target);

            conf.Defines.Add("CREATION_DATE=\"April 2021\"");
        }

        public override void ConfigureAndroid(Configuration conf, CommonTarget target)
        {
            base.ConfigureAndroid(conf, target);
            conf.Output = Configuration.OutputType.Dll;
            // The short name(libxxx.so, xxx is the short name) of App executable .so has to match the name of the project
            // because we use one AndroidManifest.xml for all configuration in the sample.
            conf.TargetFileName = Name.ToLowerInvariant();

            conf.PrecompHeader = "";
            conf.PrecompSource = "";
        }
    }


    [Sharpmake.Generate]
    public class ExePackaging : AndroidPackageProject
    {
        public static readonly string ProjectRootPath = Path.Combine(Globals.TmpDirectory, @"..\..");

        public string ResourceRootPath = Path.Combine(ProjectRootPath, "resources");

        public string GradleAppRootPath = Path.Combine(ProjectRootPath, "gradle/app");

        public static readonly string AndroidPackageProjectsPath = Path.Combine(Globals.TmpDirectory, @"projects");

        public ExePackaging() : base(typeof(CommonTarget))
        {
            DeployProject = true;

            Name = "exepackaging";

            SourceRootPath = Path.Combine(ProjectRootPath, @"codebase\temp\projects\" + Name);

            if (!Directory.Exists(SourceRootPath))
            {
                Directory.CreateDirectory(SourceRootPath);
            }

            AndroidManifest = "AndroidManifest.xml";
            AntBuildXml = "build.xml";
            AntProjectPropertiesFile = "project.properties";

            SourceFilesExtensions.Add(".xml");
            SourceFilesExclude.Add("AndroidManifest.xml", "build.xml");

            AddTargets(CommonTarget.GetAndroidTargets());

            GradlePlugin = "gradle:4.1.3";
            GradleVersion = "6.5";

            // Path to the Gradle template files
            GradleTemplateFiles.Add(@"app\src\main\AndroidManifest.xml.template");
            GradleTemplateFiles.Add(@"app\build.gradle.template");
            GradleTemplateFiles.Add(@"build.gradle.template");
            GradleTemplateFiles.Add(@"settings.gradle.template");
            GradleTemplateFiles.Add(@"gradle\wrapper\gradle-wrapper.properties.template");

            ResourceFiles.Add(@"app\src\main\res\values\strings.xml");
        }

        [Configure(Platform.android)]
        public void ConfigureAndroid(Project.Configuration conf, CommonTarget target)
        {
            conf.Name = target.Name + "_[target.AndroidBuildTargets]";
            conf.ProjectPath = Path.Combine(ProjectRootPath, @"codebase\temp\projects\[project.Name]");
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";

            conf.Output = Configuration.OutputType.Lib;

            conf.IntermediatePath = Path.Combine(ProjectRootPath, @"codebase\temp\[target.Platform]\[project.Name]\[target.Optimization]");
            conf.TargetPath = Path.Combine(ProjectRootPath, @"codebase\temp\bin\apk_[target.AndroidBuildTargets]_pack");

            conf.Options.Add(Options.Android.General.AndroidAPILevel.Latest);
            conf.Options.Add(Options.Android.General.UseOfStl.LibCpp_Static);

            conf.SolutionFolder = "apk";

            conf.AddPublicDependency<ExeProject>(target);
        }

        public override void PostResolve()
        {
            base.PostResolve();

            DirectoryCopyResourceFiles(GradleAppRootPath, Path.Combine(AndroidPackageProjectsPath, Name + "/app"));

            string srcAppGradleFile = Path.Combine(AndroidPackageProjectsPath, Name + "/app/build.app.gradle.template");
            string destAppGradleFile = Path.Combine(AndroidPackageProjectsPath, Name + "/app/build.gradle.template");

            if (File.Exists(destAppGradleFile))
            {
                File.Delete(destAppGradleFile);
            }

            // rename gradle template file in app folder
            File.Move(srcAppGradleFile, destAppGradleFile);

            string MainFolderPath = Path.Combine(AndroidPackageProjectsPath, Name + "/app/src/main");
            if (!Directory.Exists(MainFolderPath))
            {
                Directory.CreateDirectory(MainFolderPath);
            }
            DirectoryCopyResourceFiles(ResourceRootPath, MainFolderPath);
        }

        public static void DirectoryCopyResourceFiles(string sourceDirName, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new Error($"Source path does not exist {sourceDirName}");
                //LogErrorLine("Source path does not exist! {0}", sourceDirName);
            }
            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
            // Get the files in the directory and copy them to the new location.
            string[] files = Util.DirectoryGetFiles(sourceDirName);
            foreach (string file in files)
            {
                string srcFile = file;
                string relativePath = Util.PathGetRelative(sourceDirName, srcFile);
                string destFile = Path.Combine(destDirName, relativePath);
                string destDir = new FileInfo(destFile).DirectoryName;
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                Util.ForceCopy(srcFile, destFile);
            }
        }  
    }
}
