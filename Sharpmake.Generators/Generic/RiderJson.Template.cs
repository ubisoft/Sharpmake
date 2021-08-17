namespace Sharpmake.Generators.Generic
{
    public partial class RiderJson
    {

        // See Sharpmake.BasePlatform.GenerateProjectConfigurationFastBuildMakeFile()
        public static class Template
        {
            public static string FastBuildBuildCommand = @"cd $(SolutionDir)
[BeforeBuildCommand]
[BuildCommand]";
            
            public static string FastBuildReBuildCommand = @"cd $(SolutionDir)
[BeforeBuildCommand]
[RebuildCommand]";
            
            public static string FastBuildCleanCommand = @"del ""[IntermediateDirectory]\*unity*.cpp"" >NUL 2>NUL
del ""[IntermediateDirectory]\*.obj"" >NUL 2>NUL
del ""[IntermediateDirectory]\*.a"" >NUL 2>NUL
del ""[IntermediateDirectory]\*.lib"" >NUL 2>NUL
del ""[OutputDirectory]\[TargetFileFullName].exe"" >NUL 2>NUL
del ""[OutputDirectory]\[TargetFileFullName].elf"" >NUL 2>NUL
del ""[OutputDirectory]\[TargetFileFullName].exp"" >NUL 2>NUL
del ""[OutputDirectory]\[TargetFileFullName].ilk"" >NUL 2>NUL
del ""[OutputDirectory]\[TargetFileFullName].lib"" >NUL 2>NUL
del ""[OutputDirectory]\[TargetFileFullName].pdb"" >NUL 2>NUL";
        }
    }
}
