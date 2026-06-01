// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Example Roslyn DiagnosticAnalyzer that runs against Sharpmake scripts during compilation.
/// Rule: classes whose names end with "Project" or "Solution" must carry the [Sharpmake.Generate] attribute so that Sharpmake actually generates them
/// Forgetting it is a silent mistake that causes the project/solution to be skipped with no error
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingGenerateAttributeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SHARPMAKE001";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        title: "Missing [Sharpmake.Generate] attribute",
        messageFormat: "'{0}' appears to be a Sharpmake project or solution class but is missing the [Sharpmake.Generate] attribute; it will not be generated",
        category: "Sharpmake",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Sharpmake project and solution classes must carry [Sharpmake.Generate] to be included in code generation.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        string name = classDecl.Identifier.Text;

        if (!name.EndsWith("Project") && !name.EndsWith("Solution"))
            return;

        bool hasGenerateAttr = classDecl.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a =>
            {
                string attrName = a.Name.ToString();
                return attrName == "Generate" || attrName == "Sharpmake.Generate";
            });

        if (!hasGenerateAttr)
            context.ReportDiagnostic(Diagnostic.Create(Rule, classDecl.Identifier.GetLocation(), name));
    }
}
