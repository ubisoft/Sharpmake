// Copyright (c) 2022-2023 Ubisoft Entertainment
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

using Sharpmake;
using System.IO;

namespace XCodeProjects
{
    [Sharpmake.Generate]
    public class MetalNoStoryboardProject : CommonProject
    {
        public MetalNoStoryboardProject()
        {
            AddTargets(CommonTarget.GetMacTargets());
            AddTargets(CommonTarget.GetIosTargets());
            AddTargets(CommonTarget.GetTvosTargets());
            AddTargets(CommonTarget.GetCatalystTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.AppleApp;
            conf.XcodeSystemFrameworks.Add(
                "Metal",
                "MetalKit",
                "GameController"
            );

            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleVersion(@"1.0"));
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleShortVersion(@"1"));
        }

        public override void ConfigureMacOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureMacOS(conf, target);
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.InfoPListFile(Path.Join(SourceRootPath, "Info.plist")));
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(@"com.ubisoft.sharpmake.sample.metalnostoryboard.macos"));
        }

        public override void ConfigureIOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureIOS(conf, target);
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.InfoPListFile(Path.Join(SourceRootPath, "Info-ios.plist")));
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(@"com.ubisoft.sharpmake.sample.metalnostoryboard.ios"));
        }

        public override void ConfigureTVOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureTVOS(conf, target);
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.InfoPListFile(Path.Join(SourceRootPath, "Info-tvos.plist")));
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(@"com.ubisoft.sharpmake.sample.metalnostoryboard.tvos"));
        }

        public override void ConfigureCatalyst(Configuration conf, CommonTarget target)
        {
            base.ConfigureCatalyst(conf, target);
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.InfoPListFile(Path.Join(SourceRootPath, "Info-catalyst.plist")));
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(@"com.ubisoft.sharpmake.sample.metalnostoryboard.catalyst"));
        }
    }
}
