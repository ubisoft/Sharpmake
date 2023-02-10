namespace Sharpmake.Generators.Generic
{
    public partial class NinjaProject
    {
        private static class Template
        {
            // Ninja syntax
            public static string RuleBegin = "rule ";
            public static string BuildBegin = "build ";
            public static string CommandBegin = "  command = ";
            public static string DescriptionBegin = "  description = ";
            public static string Input = "$in";
            public static string Output = "$out";

            public static string PerConfigFormat(GenerationContext context)
            {
                return $"{context.Project.Name}_{context.Configuration.Name}_{context.Compiler}".ToLower();
            }
            public static string PerConfigFolderFormat(GenerationContext context)
            {
                return System.IO.Path.Combine(context.Compiler.ToString(), context.Configuration.Name);
            }

            public static string CleanBuildStatement(GenerationContext context)
            {
                return $"clean_{PerConfigFormat(context)}";
            }
            public static string CompDBBuildStatement(GenerationContext context)
            {
                return $"compdb_{PerConfigFormat(context)}";
            }

            public static class RuleStatement
            {
                private static readonly string RulePrefix = "rule_";

                public static string CompileCppFile(GenerationContext context)
                {
                    return $"{RulePrefix}compile_cpp_file_{PerConfigFormat(context)}";
                }

                public static string LinkExe(GenerationContext context)
                {
                    return $"{RulePrefix}link_exe_{PerConfigFormat(context)}";
                }

                public static string LinkLib(GenerationContext context)
                {
                    return $"{RulePrefix}link_static_lib_{PerConfigFormat(context)}";
                }

                public static string LinkDll(GenerationContext context)
                {
                    return $"{RulePrefix}link_dynamic_lib_{PerConfigFormat(context)}";
                }

                public static string LinkToUse(GenerationContext context)
                {
                    switch (context.Configuration.Output)
                    {
                        case Project.Configuration.OutputType.Exe:
                            return LinkExe(context);
                        case Project.Configuration.OutputType.Lib:
                            return LinkLib(context);
                        case Project.Configuration.OutputType.Dll:
                            return LinkDll(context);
                        default:
                            throw new Error("Invalid output type for rule");
                    }
                }

                public static string Clean(GenerationContext context)
                {
                    return $"{RulePrefix}clean_{PerConfigFormat(context)}";
                }
                public static string CompilerDB(GenerationContext context)
                {
                    return $"{RulePrefix}compdb_{PerConfigFormat(context)}";
                }
            }

            public static class BuildStatement
            {
                public static string Defines(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_DEFINES";
                }
                public static string Includes(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_INCLUDES";
                }
                public static string SystemIncludes(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_SYSTEM_INCLUDES";
                }
                public static string DepFile(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_DEP_FILE";
                }
                public static string CompilerImplicitFlags(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_COMPILER_IMPLICIT_FLAGS";
                }
                public static string CompilerFlags(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_COMPILER_FLAGS";
                }
                public static string LinkerImplicitFlags(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_LINKER_IMPLICIT_FLAGS";
                }
                public static string LinkerFlags(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_LINKER_FLAGS";
                }
                public static string TargetPdb(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_TARGET_PDB";
                }
                public static string ImplicitLinkerPaths(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_LINKER_IMPLICIT_PATHS";
                }
                public static string ImplicitLinkerLibraries(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_LINKER_IMPLICIT_LIBRARIES";
                }
                public static string LinkerPaths(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_LINKER_PATHS";
                }
                public static string LinkerLibraries(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_LINKER_LIBRARIES";
                }
                public static string PreBuild(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_PRE_BUILD";
                }
                public static string PostBuild(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_POST_BUILD";
                }
                public static string TargetFile(GenerationContext context)
                {
                    return $"{PerConfigFormat(context)}_TARGET_FILE";
                }
            }
        }
    }
}
