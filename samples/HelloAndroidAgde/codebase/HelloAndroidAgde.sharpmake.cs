// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

namespace HelloAndroidAgde
{
    [Sharpmake.Generate]
    public class HelloAndroidAgdeSolution : CommonSolution
    {
        public HelloAndroidAgdeSolution()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "HelloAndroidAgde";
        }

        private bool _hasCopiedResources = false;
        public override void ConfigureAgde(Configuration conf, CommonTarget target)
        {
            base.ConfigureAgde(conf, target);

            conf.AddProject<ExeProject>(target);

            if (!_hasCopiedResources)
            {
                //copy top-level build gradle files to root dir
                AndroidUtil.DirectoryCopy(Path.Combine(conf.Solution.SharpmakeCsPath, @"..\gradle\root"), conf.SolutionPath);
                _hasCopiedResources = true;

                var gradlePropertiesFile = Path.Combine(conf.SolutionPath, "gradle.properties");
                if (File.Exists(gradlePropertiesFile))
                {
                    using (StreamWriter sw = File.AppendText(gradlePropertiesFile))
                    {
                        sw.WriteLine(string.Format("ndkRoot={0}", Android.GlobalSettings.NdkRoot.Replace("\\", "/")));
                    }
                }
            }
        }
    }
}
