using Get.EasyCSharp.GeneratorTools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Get.Lexer.SourceGenerator;

[Generator]
[AddAttributeConverter(typeof(CompileTimeConflictCheckAttribute))]
[DiagnosticAnalyzer(LanguageNames.CSharp)]
partial class RegexConflictCheckerAnalyzer() : AttributeBaseAnalyzer<
    CompileTimeConflictCheckAttribute,
    RegexConflictCheckerAnalyzer.CompileTimeConflictCheckAttributeWarpper,
    EnumDeclarationSyntax,
    INamedTypeSymbol
>(SyntaxKind.EnumDeclaration)
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => LexerConflictCheckerAnalyzer.StaticSupportedDiagnostics;

    protected override void OnPointVisit(OnPointVisitArguments args)
    {
        //Debugger.Launch();
        LexerConflictCheckerAnalyzer.OnPointVisitShared(args.Symbol, args.Context, args.CancellationToken);
    }
    protected override CompileTimeConflictCheckAttributeWarpper? TransformAttribute(AttributeData attributeData, Compilation compilation)
    {
        return AttributeDataToCompileTimeConflictCheckAttribute(attributeData, compilation);
    }
}