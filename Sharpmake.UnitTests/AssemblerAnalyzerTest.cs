// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Sharpmake.UnitTests;

[TestFixture]
public class AssemblerAnalyzerTest
{
    private const string WarningDiagnosticId = "TEST001";
    private const string ErrorDiagnosticId = "TEST002";

    private string _tempDir;
    private string _warningAnalyzerDll;
    private string _errorAnalyzerDll;
    private string _scriptFile;

    [OneTimeSetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SharpmakeAnalyzerTests");

        // Clean potential previous run's assembly that were not properly clean
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);

        Directory.CreateDirectory(_tempDir);

        _warningAnalyzerDll = Path.Combine(_tempDir, "WarningAnalyzer.dll");
        CompileCustomAnalyzerDll(_warningAnalyzerDll, WarningDiagnosticId, DiagnosticSeverity.Warning);

        _errorAnalyzerDll = Path.Combine(_tempDir, "ErrorAnalyzer.dll");
        CompileCustomAnalyzerDll(_errorAnalyzerDll, ErrorDiagnosticId, DiagnosticSeverity.Error);

        _scriptFile = Path.Combine(_tempDir, "TestScript.sharpmake.cs");
        File.WriteAllText(_scriptFile, "namespace TestScript { public class TestProject { } }");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
            // loaded DLLs are locked until process exit; next run will clean up
        }
    }

    // Without any analyzer DLL in the assembler's references, no analyzer diagnostics are emitted.
    [Test]
    public void WithoutAnalyzerReferences_NoAnalyzerDiagnosticsEmitted()
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        Assembler.OutputDelegate warnHandler = (msg, args) => warnings.Add(string.Format(msg, args));
        Assembler.OutputDelegate errHandler = (msg, args) => errors.Add(string.Format(msg, args));
        Assembler.EventOutputWarning += warnHandler;
        Assembler.EventOutputError += errHandler;
        try
        {
            new Assembler().BuildAssembly(_scriptFile);

            Assert.That(warnings, Has.None.Contains(WarningDiagnosticId));
            Assert.That(errors, Has.None.Contains(ErrorDiagnosticId));
        }
        finally
        {
            Assembler.EventOutputWarning -= warnHandler;
            Assembler.EventOutputError -= errHandler;
        }
    }

    // When an analyzer DLL producing a warning is in the assembler's references, the warning is reported via EventOutputWarning.
    [Test]
    public void WithAnalyzerInReferences_WarningDiagnosticIsReported()
    {
        var warnings = new List<string>();
        Assembler.OutputDelegate handler = (msg, args) => warnings.Add(string.Format(msg, args));
        Assembler.EventOutputWarning += handler;
        try
        {
            var assembler = new Assembler();
            assembler.Assemblies.Add(Assembly.LoadFrom(_warningAnalyzerDll));
            assembler.BuildAssembly(_scriptFile);

            Assert.That(warnings, Has.Some.Contains(WarningDiagnosticId));
        }
        finally
        {
            Assembler.EventOutputWarning -= handler;
        }
    }

    // When an analyzer DLL producing an error is in the assembler's references, the error is reported via EventOutputError and compilation throws.
    [Test]
    public void WithAnalyzerInReferences_ErrorDiagnosticIsReportedAndThrows()
    {
        var errors = new List<string>();
        Assembler.OutputDelegate handler = (msg, args) => errors.Add(string.Format(msg, args));
        Assembler.EventOutputError += handler;
        try
        {
            var assembler = new Assembler();
            assembler.Assemblies.Add(Assembly.LoadFrom(_errorAnalyzerDll));

            Assert.Throws<Error>(() => assembler.BuildAssembly(_scriptFile));
            Assert.That(errors, Has.Some.Contains(ErrorDiagnosticId));
        }
        finally
        {
            Assembler.EventOutputError -= handler;
        }
    }

    // Compiles a minimal DiagnosticAnalyzer to a DLL on disk.
    // The analyzer fires on every class declaration with the given diagnostic id and severity.
    private static void CompileCustomAnalyzerDll(string outputPath, string diagnosticId, DiagnosticSeverity severity)
    {
        string source = $@"
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestAnalyzer : DiagnosticAnalyzer
{{
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        ""{diagnosticId}"", ""Test"", ""Test diagnostic"", ""Testing"",
        DiagnosticSeverity.{severity}, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {{
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(
            ctx => ctx.ReportDiagnostic(Diagnostic.Create(Rule, ctx.Node.GetLocation())),
            SyntaxKind.ClassDeclaration);
    }}
}}";

        var objectAssembly = typeof(object).Assembly.Location;
        var runtimeAssembly = Path.Combine(Path.GetDirectoryName(objectAssembly), "System.Runtime.dll");
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(objectAssembly),
            MetadataReference.CreateFromFile(runtimeAssembly),
            MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Diagnostic).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CSharpSyntaxTree).Assembly.Location),
        };

        var assemblyName = Path.GetFileNameWithoutExtension(outputPath);
        var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(source) };
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, references, compilationOptions);

        using var stream = File.Create(outputPath);
        var result = compilation.Emit(stream);
        if (!result.Success)
            throw new InvalidOperationException($"Failed to compile test analyzer '{outputPath}': {string.Join(", ", result.Diagnostics)}");
    }
}
