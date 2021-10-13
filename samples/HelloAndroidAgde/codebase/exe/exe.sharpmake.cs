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

            // Show resource files in project
            AdditionalSourceRootPaths.Add(Path.Combine(Globals.TmpDirectory, @"projects\[project.Name]"));
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

        public override void ConfigureAgde(Configuration conf, CommonTarget target)
        {
            base.ConfigureAgde(conf, target);

            conf.IncludePaths.Add(@"$(AndroidNdkDirectory)\sources\android"); // For android_native_app_glue.h

            switch (target.AndroidBuildTargets)
            {
                case Android.AndroidBuildTargets.arm64_v8a:
                    conf.TargetPath = Path.Combine(conf.TargetPath, "arm64-v8a"); // The gradle script require the outdir end of "arm64-v8a"
                    break;
                case Android.AndroidBuildTargets.x86_64:
                    conf.TargetPath = Path.Combine(conf.TargetPath, "x86_64");
                    break;
                case Android.AndroidBuildTargets.armeabi_v7a:
                    conf.TargetPath = Path.Combine(conf.TargetPath, "armeabi-v7a");
                    break;
                case Android.AndroidBuildTargets.x86:
                    conf.TargetPath = Path.Combine(conf.TargetPath, "x86");
                    break;

                default:
                    throw new System.Exception($"TODO, {target.AndroidBuildTargets} is not supported Android target for AGDE.");
            }
            conf.AdditionalLinkerOptions.Add("-llog", "-landroid");

            // Make sure the path of project exists as it is set to SourceRootPath.
            Resolver resolver = new Resolver();
            resolver.SetParameter("project", this);
            string projectPath = resolver.Resolve(conf.ProjectPath);
            if (!Directory.Exists(projectPath))
            {
                Directory.CreateDirectory(projectPath);
            }

            if (!_hasCopiedResources)
                CopyAndroidResources(projectPath);

            // The short name(libxxx.so, xxx is the short name) of App executable .so has to match the name of the project
            // because the native activity name is set to project.LowerName in AndroidManifest.xml.
            conf.TargetFileName = LowerName;
        }

        private bool _hasCopiedResources = false;
        private void CopyAndroidResources(string projectPath)
        {
            _hasCopiedResources = true;

            string dstPath = Path.Combine(projectPath, @"src\main");
            if (!Directory.Exists(dstPath))
            {
                Directory.CreateDirectory(dstPath);
            }
            string srcManifestFile = Path.Combine(SharpmakeCsProjectPath, @"..\..\resources\AndroidManifest.xml");
            string dstManifestFile = Path.Combine(dstPath, "AndroidManifest.xml");
            Util.ForceCopy(srcManifestFile, dstManifestFile);
            UpdateManifestXML(dstManifestFile, "HelloAndroidAgde");

            // Copy module build gradle file to project folder
            string srcModulePath = Path.Combine(SharpmakeCsProjectPath, @"..\..\gradle\app");
            AndroidUtil.DirectoryCopy(srcModulePath, projectPath);
        }

        private void UpdateManifestXML(string destManifest, string packageName)
        {
            #region Remove the read only attribute of the manifest file
            FileAttributes attributes = File.GetAttributes(destManifest);
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                attributes &= ~FileAttributes.ReadOnly;
                File.SetAttributes(destManifest, attributes);
            }
            #endregion

            #region Update the contents
            XmlDocument androidManifestDoc = new XmlDocument();
            androidManifestDoc.Load(destManifest);
            XmlElement root = androidManifestDoc.DocumentElement;

            // Change package name
            root.SetAttribute("package", string.Format("com.android.{0}", packageName.ToLowerInvariant()));

            // Change application label
            string nsURI = root.GetAttribute("xmlns:android");
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(androidManifestDoc.NameTable);
            nsmgr.AddNamespace("ns", nsURI);
            XmlAttribute xmlAndroidLabel = (XmlAttribute)androidManifestDoc.SelectSingleNode("//manifest/application/@ns:label", nsmgr);
            if (xmlAndroidLabel != null)
            {
                xmlAndroidLabel.Value = packageName;
            }
            xmlAndroidLabel = (XmlAttribute)androidManifestDoc.SelectSingleNode("manifest/application/activity/@ns:label", nsmgr);
            if (xmlAndroidLabel != null)
            {
                xmlAndroidLabel.Value = packageName;
            }

            #endregion

            androidManifestDoc.Save(destManifest);
        }
    }
}
