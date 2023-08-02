// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System.Text;
using System.Xml;
using Sharpmake;

namespace HelloAndroidAgde
{
    [Sharpmake.Generate]
    public class ExeProject : CommonProject
    {
        public ExeProject()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "exe";

            SourceFilesExtensions.Add(".xml");
            SourceFilesExtensions.Add(".gradle");

            SourceFiles.Add(Path.Combine(Android.GlobalSettings.NdkRoot, @"sources\android\native_app_glue\android_native_app_glue.c"));

            // Show resource files in project
            var projectPath = Path.Combine(Globals.TmpDirectory, "projects", Name);
            AdditionalSourceRootPaths.Add(projectPath);
            CopyAndroidResources(projectPath);
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

        private string GetABI(CommonTarget target)
        {
            switch (target.AndroidBuildTargets)
            {
                case Android.AndroidBuildTargets.arm64_v8a:
                    return "arm64-v8a";
                case Android.AndroidBuildTargets.x86_64:
                    return "x86_64";
                case Android.AndroidBuildTargets.armeabi_v7a:
                    return "armeabi-v7a";
                case Android.AndroidBuildTargets.x86:
                    return "x86";
                default:
                    throw new System.Exception($"Something wrong? {target.AndroidBuildTargets} is not supported Android target for AGDE.");
            }
        }

        public override void ConfigureAgde(Configuration conf, CommonTarget target)
        {
            base.ConfigureAgde(conf, target);

            conf.IncludePaths.Add(Path.Combine(Android.GlobalSettings.NdkRoot, @"sources\android")); // For android_native_app_glue.h

            //It is an error if the `OutDir` does not end with one of the following Android ABI name.
            //  - x86
            //  - x86_64
            //  - armeabi-v7a
            //  - arm64-v8a
            conf.TargetPath = Path.Combine(conf.TargetPath, GetABI(target));
            // There's a bug in AGDE which fail to copy dependencies when using conf.LibraryFiles.Add("liblog.so", "libandroid.so")
            conf.AdditionalLinkerOptions.Add("-llog", "-landroid");

            // The short name(libxxx.so, xxx is the short name) of App executable .so has to match the name of the project
            // because it is set to project.LowerName in AndroidManifest.xml.
            conf.TargetFileName = LowerName;
        }

        public override void PostResolve()
        {
            base.PostResolve();

            foreach (var conf in Configurations)
            {
                if (conf.IsFastBuild)
                {
                    CommonTarget target = conf.Target as CommonTarget;
                    var apkFilename = Name + "_" + target.DirectoryName + ".apk";

                    //Define where to find the apk for debugging
                    var apkFileWithAbsolutePath = Util.SimplifyPath(Path.Combine(conf.TargetPath, @"..\..", apkFilename));
                    conf.Options.Add(new Options.Agde.General.AndroidApkLocation(apkFileWithAbsolutePath));

                    SetupPackageBat(conf, target, apkFilename);
                }
            }
        }

        private void SetupPackageBat(Configuration conf, CommonTarget target, string apkFilename)
        {
            bool libCppShared = conf.Options.Contains(Options.Agde.General.UseOfStl.LibCpp_Shared) ? true : false;

            var ndkVersion = Android.Util.GetNdkVersion(Android.GlobalSettings.NdkRoot);
            var minSdkVersion = Android.Util.GetAndroidApiLevelString(AndroidMinSdkVersion);
            var targetLibsPath = Util.SimplifyPath(Path.Combine(conf.TargetPath, ".."));

            //It must be a relative path as gradle required,
            // the following relative path is against "HelloAndroidAgde\codebase\temp\projects\exe\build\outputs\apk\[optimization]" folder
            var apkTargetFile = "../../../../../../bin/" + apkFilename;

            var gradlewParam = "clean :" + Name + ":assemble" + target.Name;
            targetLibsPath = targetLibsPath.Replace("\\", "/");
            gradlewParam += string.Format(@" ""-PNDK_VERSION={0}"" ""-PMIN_SDK_VERSION={1}"" ""-PJNI_LIBS_SRC_DIR={2}"" ""-PANDROID_OUTPUT_APK_NAME={3}"" ""-PFastBuild=True"" ""-PLibCppShared={4}"" ""-PABI={5}""", ndkVersion, minSdkVersion, targetLibsPath, apkTargetFile, libCppShared, GetABI(target));
            var gradleBuildRoot = Util.SimplifyPath(Path.Combine(conf.ProjectPath, ".."));
            var packageBatFile = "gradlew.bat ";
            var cmd = $@"pushd {gradleBuildRoot} &amp;&amp; {packageBatFile} {gradlewParam}";
            conf.Options.Add(new Options.Agde.General.AndroidPreApkInstallCommands(cmd));
        }

        private void CopyAndroidResources(string projectPath)
        {
            string dstPath = Path.Combine(projectPath, @"src\main");
            if (!Directory.Exists(dstPath))
            {
                Directory.CreateDirectory(dstPath);
            }
            string srcManifestFile = Path.Combine(SharpmakeCsProjectPath, @"..\..\resources\AndroidManifest.xml");
            string dstManifestFile = Path.Combine(dstPath, "AndroidManifest.xml");
            Util.ForceCopy(srcManifestFile, dstManifestFile);

            // Copy module build gradle file to project folder
            string srcModulePath = Path.Combine(SharpmakeCsProjectPath, @"..\..\gradle\app");
            AndroidUtil.DirectoryCopy(srcModulePath, projectPath);
        }
    }
}
