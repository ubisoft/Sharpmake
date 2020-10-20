using Sharpmake;

namespace HelloAndroid
{
    [Generate]
    public class HelloAndroidProject : Project
    {
        public HelloAndroidProject()
            : base(typeof(AndroidTarget))
        {
            RootPath = @"[project.SharpmakeCsPath]\codebase";
            SourceRootPath = @"[project.RootPath]";
            AddTargets(AndroidTarget.GetDefaultTargets());
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects\";
            conf.ProjectFileName = @"[project.Name].[target.DevEnv]";
            conf.IntermediatePath = @"[conf.ProjectPath]\temp\[target.DevEnv]\[target.Platform]\[target]";


            // Natice Android projects are compiled as dlls, not exes
            conf.Output = Configuration.OutputType.Dll;

            // Required options
            conf.Options.Add(Options.Android.General.ThumbMode.Disabled);
            conf.Options.Add(Options.Android.General.UseOfStl.GnuStl_Static);
            conf.Options.Add(Options.Android.General.AndroidAPILevel.Android21);
            conf.Options.Add(Options.Android.General.WarningLevel.EnableAllWarnings);
            conf.Options.Add(Options.Android.Compiler.CppLanguageStandard.Cpp11);

            conf.AdditionalLinkerOptions.Add("-lGLESv1_CM", "-lEGL");
        }
    }

    [Generate]
    class HelloAndroidPackage : AndroidPackageProject
    {
        public HelloAndroidPackage()
            : base(typeof(AndroidTarget))
        {
            RootPath = @"[project.SharpmakeCsPath]\package";
            SourceRootPath = @"[project.RootPath]";
            AddTargets(AndroidTarget.GetDefaultTargets());
            AppLibType = typeof(HelloAndroidProject);
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.Options.Add(Options.Android.General.AndroidAPILevel.Android21);

            // AndroidPackage projects MUST be in the same directory as their content
            conf.ProjectPath = @"[project.RootPath]";
            conf.ProjectFileName = @"[project.Name].[target.DevEnv]";
            conf.IntermediatePath = @"[conf.ProjectPath]\temp\[target.DevEnv]\[target.Platform]\[target]";
            conf.DeployProject = true;

            conf.AddPrivateDependency<HelloAndroidProject>(target);
        }
    }
}
