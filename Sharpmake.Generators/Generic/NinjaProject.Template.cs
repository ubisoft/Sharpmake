using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpmake.Generators.Generic
{
    public partial class NinjaProject
    {
        public static class Template
        {
            public static class Project
            {
                public static string ConfigurationBegin = "CONFIGURATION";

                public static string RuleBegin = "rule ";

                public static string BuildBegin = "build ";
                public static string BuildCPPFileName = "COMPILE_CPP_FILE_[project_name]_[config_name]_[config_compiler]";
                public static string BuildLinkExeName = "LINK_CPP_FILE_EXE_[project_name]_[config_name]_[config_compiler]";
                public static string Clean = "Clean_[config_name]_[config_compiler]";

                public static string CommandBegin = "  command = ";
                public static string DescriptionBegin = "  description = ";

                public static readonly string Defines = "[project_name]_[config_name]_[config_compiler]_DEFINES";
                public static readonly string Includes = "[project_name]_[config_name]_[config_compiler]_INCLUDES";
                public static readonly string SystemIncludes = "[project_name]_[config_name]_[config_compiler]_SYSTEM_INCLUDES";
                public static readonly string DepFile = "[project_name]_[config_name]_[config_compiler]_DEP_FILE";
                public static readonly string CompilerImplicitFlags = "[project_name]_[config_name]_[config_compiler]_COMPILER_IMPLICIT_FLAGS";
                public static readonly string LinkerImplicitFlags = "[project_name]_[config_name]_[config_compiler]_LINKER_IMPLICIT_FLAGS";
                public static readonly string CompilerFlags = "[project_name]_[config_name]_[config_compiler]_COMPILER_FLAGS";
                public static readonly string LinkerFlags = "[project_name]_[config_name]_[config_compiler]_LINKER_FLAGS";
                public static readonly string TargetPdb = "[project_name]_[config_name]_[config_compiler]_TARGET_PDB";
                public static readonly string ImplicitLinkPaths = "[project_name]_[config_name]_[config_compiler]_IMPLICIT_LINK_PATHS";
                public static readonly string ImplicitLinkLibraries = "[project_name]_[config_name]_[config_compiler]_IMPLICIT_LINK_LIBRARIES";
                public static readonly string LinkPaths = "[project_name]_[config_name]_[config_compiler]_LINK_PATHS";
                public static readonly string LinkLibraries = "[project_name]_[config_name]_[config_compiler]_LINK_LIBRARIES";
                public static readonly string PostBuild = "[project_name]_[config_name]_[config_compiler]_POST_BUILD";
                public static readonly string PreBuild = "[project_name]_[config_name]_[config_compiler]_PRE_BUILD";
                public static readonly string TargetFile = "[project_name]_[config_name]_[config_compiler]_TARGET_FILE";
            }   
        }
    }
}
