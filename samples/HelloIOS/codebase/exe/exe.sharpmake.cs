// Copyright (c) 2020 Ubisoft Entertainment
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

namespace HelloIOS
{
    [Sharpmake.Generate]
    public class ExeProject : CommonProject
    {
        public ExeProject()
        {
            Name = "exe";
            
            ResourceFilesExtensions.Add(".storyboard");

            #region XCTest
            XcodeUnitTestTargetName = "HelloXCTest";

            XcodeUnitTestSourceRootPath = Path.Combine(SourceRootPath, "ios", "unittest");

            string buildExcludedUnitTestFile = Path.Combine(SourceRootPath, "ios", "unittest", "excluded_from_build.mm");
            XcodeUnitTestSourceFilesBuildExclude.Add(buildExcludedUnitTestFile);

            // exclude unittest files from app, suppose the files under unittest folder are only for XCTest
            SourceFilesExcludeRegex.Add(@"\/ios\/unittest\/");
            #endregion XCTest
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.Exe;

            conf.PrecompHeader = "stdafx.h";
            conf.PrecompSource = "stdafx.cpp";

            conf.AddPrivateDependency<StaticLib1Project>(target);
            conf.AddPrivateDependency<StaticLib2Project>(target);

            conf.Defines.Add("CREATION_DATE=\"July 2020\"");
        }

        public override void ConfigureIos(Configuration conf, CommonTarget target)
        {
            base.ConfigureIos(conf, target);

            conf.Output = Configuration.OutputType.AppleApp;
            conf.PrecompHeader = null;
            conf.PrecompSource = null;

            conf.IncludePaths.Add(SourceRootPath + "/ios");

            string plistPath = SourceRootPath + "/ios/Info.plist";
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.InfoPListFile(plistPath));
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier("com.example.helloios"));

            {
                #region XCTest

                string unitTestPlistPath = SourceRootPath + "/ios/UnitTestInfo.plist";
                conf.Options.Add(new Sharpmake.Options.XCode.Compiler.UnitTestInfoPListFile(unitTestPlistPath));

                #endregion
            }

            conf.XcodeSystemFrameworks.Add("UIKit");
            conf.XcodeSystemFrameworks.Add("Foundation");
        }
    }
}
