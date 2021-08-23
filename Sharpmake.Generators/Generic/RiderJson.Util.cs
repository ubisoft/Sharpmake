using System;
using System.Linq;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators.Generic
{
    public static class RiderJsonUtil
    {
        private static class CppLanguageStandard
        {
            public const string Cpp14 = "Cpp14";
            public const string Cpp17 = "Cpp17";
            public const string Latest = "Latest";
            public const string Default = "Default";
        }

        private static class PchAction
        {
            public const string None = "None";
            public const string Include = "Include";
            public const string Create = "Create";
        }

        public static string GetCppStandard(this IGenerationContext context)
        {
            var res = CppLanguageStandard.Default;
            if (Options.HasOption<Options.Vc.Compiler.CppLanguageStandard>(context.Configuration))
            {
                context.SelectOptionWithFallback
                (
                () => res = CppLanguageStandard.Default,
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.CPP14, () => res = CppLanguageStandard.Cpp14),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.GNU14, () => res = CppLanguageStandard.Cpp14),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.CPP17, () => res = CppLanguageStandard.Cpp17),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.GNU17, () => res = CppLanguageStandard.Cpp17),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.Latest, () => res = CppLanguageStandard.Latest)
                );
                return res;
            }
            if (Options.HasOption<Options.Clang.Compiler.CppLanguageStandard>(context.Configuration))
            {
                context.SelectOptionWithFallback
                (
                () => res = CppLanguageStandard.Default,
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.Cpp14, () => res = CppLanguageStandard.Cpp14),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.GnuCpp14, () => res = CppLanguageStandard.Cpp14),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.Cpp17, () => res = CppLanguageStandard.Cpp17),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.GnuCpp17, () => res = CppLanguageStandard.Cpp17),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.Cpp2a, () => res = CppLanguageStandard.Latest),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.GnuCpp2a, () => res = CppLanguageStandard.Latest)
                );
                return res;
            }
            if (Options.HasOption<Options.Makefile.Compiler.CppLanguageStandard>(context.Configuration))
            {
                context.SelectOptionWithFallback
                (
                () => res = CppLanguageStandard.Default,
                Options.Option(Options.Makefile.Compiler.CppLanguageStandard.Cpp14, () => res = CppLanguageStandard.Cpp14),
                Options.Option(Options.Makefile.Compiler.CppLanguageStandard.GnuCpp14, () => res = CppLanguageStandard.Cpp14),
                Options.Option(Options.Makefile.Compiler.CppLanguageStandard.Cpp17, () => res = CppLanguageStandard.Cpp17),
                Options.Option(Options.Makefile.Compiler.CppLanguageStandard.GnuCpp17, () => res = CppLanguageStandard.Cpp17),
                Options.Option(Options.Makefile.Compiler.CppLanguageStandard.Cpp2a, () => res = CppLanguageStandard.Latest),
                Options.Option(Options.Makefile.Compiler.CppLanguageStandard.GnuCpp2a, () => res = CppLanguageStandard.Latest)
                );
                return res;
            }
            if (Options.HasOption<Options.XCode.Compiler.CppLanguageStandard>(context.Configuration))
            {
                context.SelectOptionWithFallback
                (
                () => res = CppLanguageStandard.Default,
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP14, () => res = CppLanguageStandard.Cpp14),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU14, () => res = CppLanguageStandard.Cpp14),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP17, () => res = CppLanguageStandard.Cpp17),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU17, () => res = CppLanguageStandard.Cpp17)
                );
                return res;
            }

            context.SelectOptionWithFallback
            (
            () => res = CppLanguageStandard.Default,
            Options.Option(Options.Android.Compiler.CppLanguageStandard.Cpp14, () => res = CppLanguageStandard.Cpp14),
            Options.Option(Options.Android.Compiler.CppLanguageStandard.Cpp1y, () => res = CppLanguageStandard.Cpp14),
            Options.Option(Options.Android.Compiler.CppLanguageStandard.GNU_Cpp14, () => res = CppLanguageStandard.Cpp14),
            Options.Option(Options.Android.Compiler.CppLanguageStandard.GNU_Cpp1y, () => res = CppLanguageStandard.Cpp14),
            Options.Option(Options.Android.Compiler.CppLanguageStandard.Cpp17, () => res = CppLanguageStandard.Cpp17),
            Options.Option(Options.Android.Compiler.CppLanguageStandard.Cpp1z, () => res = CppLanguageStandard.Cpp17),
            Options.Option(Options.Android.Compiler.CppLanguageStandard.GNU_Cpp17, () => res = CppLanguageStandard.Cpp17),
            Options.Option(Options.Android.Compiler.CppLanguageStandard.GNU_Cpp1z, () => res = CppLanguageStandard.Cpp17),
            Options.Option(Options.Android.Compiler.CppLanguageStandard.Cpp2a, () => res = CppLanguageStandard.Latest),
            Options.Option(Options.Android.Compiler.CppLanguageStandard.GNU_Cpp2a, () => res = CppLanguageStandard.Latest)
            );
            
            return res;
        }

        public static string GetArchitecture(this IGenerationContext context)
        {
            var architecture = Util.GetPlatformString(context.Configuration.Platform, context.Project, context.Configuration.Target);
            if (architecture == "Any CPU")
            {
                architecture = "x64";
            }

            return architecture.ToLower();
        }
        
        public static bool IsRttiEnabled(this IGenerationContext context)
        {
            if (!context.Configuration.CheckOptions(Options.Vc.Compiler.RTTI.Disable,
                Options.Makefile.Compiler.Rtti.Disable,
                Options.XCode.Compiler.RTTI.Disable,
                Options.Clang.Compiler.Rtti.Disable))
            {
                // Check default value
                return Options.GetObject<Options.Vc.Compiler.RTTI>(context.Configuration) !=
                       Options.Vc.Compiler.RTTI.Disable;
            }

            return false;
        }
        
        public static bool IsExceptionEnabled(this IGenerationContext context)
        {
            if (!context.Configuration.CheckOptions(Options.Vc.Compiler.Exceptions.Disable,
                Options.Makefile.Compiler.Exceptions.Disable,
                Options.XCode.Compiler.Exceptions.Disable,
                Options.Clang.Compiler.Exceptions.Disable,
                Options.Android.Compiler.Exceptions.Disable))
            {
                // Check default value
                return Options.GetObject<Options.Vc.Compiler.Exceptions>(context.Configuration) != Options.Vc.Compiler.Exceptions.Disable;
            }

            return false;
        }
        
        public static bool IsOptimizationEnabled(this IGenerationContext context)
        {
            if (!context.Configuration.CheckOptions(
                Options.Vc.Compiler.Optimization.Disable,
                Options.Makefile.Compiler.OptimizationLevel.Disable,
                Options.XCode.Compiler.OptimizationLevel.Disable,
                Options.Clang.Compiler.OptimizationLevel.Disable))
            {
                // Check default value
                return Options.GetObject<Options.Vc.Compiler.Optimization>(context.Configuration) != Options.Vc.Compiler.Optimization.Disable;
            }

            return false;
        }

        public static bool IsInliningEnabled(this IGenerationContext context)
        {
            return Options.GetObject<Options.Vc.Compiler.Inline>(context.Configuration) !=
                   Options.Vc.Compiler.Inline.Disable;
        }
        
        public static bool IsBlob(this IGenerationContext context)
        {
            return context.Configuration.IsBlobbed || context.Configuration.FastBuildBlobbed;
        }
        
        public static bool IsDebugInfo(this IGenerationContext context)
        {
            if (!context.Configuration.CheckOptions(
                Options.Vc.General.DebugInformation.Disable,
                Options.Makefile.Compiler.GenerateDebugInformation.Disable,
                Options.XCode.Compiler.GenerateDebuggingSymbols.Disable,
                Options.Clang.Compiler.GenerateDebugInformation.Disable,
                Options.Android.Compiler.DebugInformationFormat.None))
            {
                // Check default option
                return Options.GetObject<Options.Vc.General.DebugInformation>(context.Configuration) !=
                       Options.Vc.General.DebugInformation.Disable;
            }

            return false;
        }
        
        public static bool IsAvx(this IGenerationContext context)
        {
            bool res = false;
            context.SelectOptionWithFallback(
            () => res = false,
            Options.Option(Options.Vc.Compiler.EnhancedInstructionSet.AdvancedVectorExtensions, () => res = true),
            Options.Option(Options.Vc.Compiler.EnhancedInstructionSet.AdvancedVectorExtensions2, () => res = true),
            Options.Option(Options.Vc.Compiler.EnhancedInstructionSet.AdvancedVectorExtensions512, () => res = true)
            );

            return res;
        }
        
        public static bool IsConformanceMode(this IGenerationContext context)
        {
            return Options.GetObject<Options.Vc.Compiler.ConformanceMode>(context.Configuration) ==
                   Options.Vc.Compiler.ConformanceMode.Enable;
        }
        
        public static string GetPchAction(this IGenerationContext context)
        {
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);

            if (!Options.HasOption<Options.Vc.SourceFile.PrecompiledHeader>(context.Configuration))
            {
                return platformVcxproj.HasPrecomp(context) ? PchAction.Include : PchAction.None;
            }
            
            var pchOption = Options.GetObject<Options.Vc.SourceFile.PrecompiledHeader>(context.Configuration);
            switch (pchOption)
            {
                case Options.Vc.SourceFile.PrecompiledHeader.UsePrecompiledHeader:
                    return PchAction.Include;
                case Options.Vc.SourceFile.PrecompiledHeader.CreatePrecompiledHeader:
                    return PchAction.Create;
                default:
                    return PchAction.None;
            }
        }
        
        /// <summary>
        /// Checks if one of the <paramref name="options"/> presents in <paramref name="config"/> without trying to get default values.
        /// Comparing to IGenerationContext.SelectOption(), can check options of different types.
        /// </summary>
        private static bool CheckOptions(this Project.Configuration config, params object[] options)
        {
            var optionsType = typeof(Options);
            var getObjectArgsTypes = new Type[] { typeof(Project.Configuration) };
            var configArg = new object[] { config };
        
            var getObjectMethod = optionsType.GetMethod("GetObject", getObjectArgsTypes);
            var hasObjectMethod = optionsType.GetMethod("HasOption");
            
            foreach (var option in options)
            {
                var genericHasOption = hasObjectMethod.MakeGenericMethod(option.GetType());
                if (!(bool)genericHasOption.Invoke(null, configArg))
                {
                    continue;
                }
                
                var genericGetObj = getObjectMethod.MakeGenericMethod(option.GetType());
                return genericGetObj.Invoke(null, configArg).Equals(option);
            }
        
            var genericGetFirstObj = getObjectMethod.MakeGenericMethod(options.First().GetType());
            return genericGetFirstObj.Invoke(null, configArg).Equals(options.First());
        }
    }
}
