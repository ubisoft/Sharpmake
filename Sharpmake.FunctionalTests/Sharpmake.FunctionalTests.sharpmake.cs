using System;
using Sharpmake;

namespace SharpmakeGen
{
    namespace FunctionalTests
    {
        public abstract class FunctionalTestProject : Common.SharpmakeBaseProject
        {
            public FunctionalTestProject()
                : base(excludeSharpmakeFiles: false, generateXmlDoc: false)
            {
                // same a samples, tests are special, the class is here instead of in the subfolder
                SourceRootPath = @"[project.SharpmakeCsPath]\[project.Name]";
                SourceFilesExcludeRegex.Add(
                    @"\\codebase\\",
                    @"\\projects\\",
                    @"\\reference\\"
                );

                AddTargets(Common.GetDefaultTargets());
            }

            public override void ConfigureAll(Configuration conf, Target target)
            {
                base.ConfigureAll(conf, target);

                conf.SolutionFolder = "FunctionalTests";
                conf.TargetPath = @"[project.RootPath]\bin\[target.Optimization]\functionaltests";

                conf.AddPrivateDependency<SharpmakeProject>(target);
                conf.AddPrivateDependency<SharpmakeApplicationProject>(target);
                conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
                conf.AddPrivateDependency<Platforms.CommonPlatformsProject>(target);

                conf.CsprojUserFile = new Project.Configuration.CsprojUserFileSettings
                {
                    StartAction = Project.Configuration.CsprojUserFileSettings.StartActionSetting.Program,
                    StartProgram = @"[conf.TargetPath]\Sharpmake.Application.exe",
                    StartArguments = "/sources(\"[project.Name].sharpmake.cs\")",
                    WorkingDirectory = "[project.SourceRootPath]",
                    OverwriteExistingFile = false
                };
            }
        }

        [Generate]
        public class FastBuildFunctionalTest : FunctionalTestProject
        {
            public FastBuildFunctionalTest()
            {
                Name = "FastBuildFunctionalTest";
            }
        }

        [Generate]
        public class NoAllFastBuildProjectFunctionalTest : FunctionalTestProject
        {
            public NoAllFastBuildProjectFunctionalTest()
            {
                Name = "NoAllFastBuildProjectFunctionalTest";
            }
        }
    }
}
