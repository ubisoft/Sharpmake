// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

// -----------------------------------------------------------------------------
// Custom Roslyn Analyzer Sample
// -----------------------------------------------------------------------------
// This sample shows how to plug a custom DiagnosticAnalyzer into Sharpmake so that it runs against your build scripts at compile time.
//
// How to run:
//   1. Build the analyzer first: dotnet build analyzer/CustomAnalyzer.csproj
//   2. Run this script with Sharpmake: Sharpmake.Application.exe /sources(@'CustomAnalyzer.sharpmake.cs')
//
// The [module: Sharpmake.Reference(...)] attribute below tells Sharpmake to load the analyzer DLL as a reference when it compiles this script.
// Sharpmake then discovers every DiagnosticAnalyzer type in that DLL and runs it against the compiled Roslyn compilation, reporting any diagnostics it produces.
//
// Expected output:
//   The analyzer (SHARPMAKE001) will fire for 'ForgottenProject' because that class is missing the [Sharpmake.Generate] attribute.
//   'SampleProject' and 'CustomAnalyzerSolution' are correctly attributed and produce no warning.
// -----------------------------------------------------------------------------

using Sharpmake;

[module: Reference(@"analyzer\bin\Release\netstandard2.0\CustomAnalyzer.dll")]

namespace CustomAnalyzer;

// Correctly attributed, no SHARPMAKE001 warning
[Generate]
public class SampleProject : Project
{
    public SampleProject()
    {
        Name = "CustomAnalyzerSample";
        AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
        SourceRootPath = @"[project.SharpmakeCsPath]\codebase";
    }

    [Configure]
    public void ConfigureAll(Configuration conf, Target target)
    {
        conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
        conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
    }
}

// Missing [Sharpmake.Generate]
// The analyzer emits:
//   warning SHARPMAKE001: 'ForgottenProject' appears to be a Sharpmake project or solution class but is missing the [Sharpmake.Generate] attribute; it will not be generated.
// Add [Sharpmake.Generate] above this class to fix the warning
public class ForgottenProject : Project
{
    public ForgottenProject()
    {
        Name = "Forgotten";
        AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
        SourceRootPath = @"[project.SharpmakeCsPath]\codebase";
    }

    [Configure]
    public void ConfigureAll(Configuration conf, Target target)
    {
        conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
        conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
    }
}

[Generate]
public class CustomAnalyzerSolution : Sharpmake.Solution
{
    public CustomAnalyzerSolution()
    {
        Name = "CustomAnalyzer";
        AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
    }

    [Configure]
    public void ConfigureAll(Configuration conf, Target target)
    {
        conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
        conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
        conf.AddProject<SampleProject>(target);
    }
}

public static class Main
{
    [Sharpmake.Main]
    public static void SharpmakeMain(Sharpmake.Arguments arguments)
    {
        KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.Latest);
        arguments.Generate<CustomAnalyzerSolution>();
    }
}
