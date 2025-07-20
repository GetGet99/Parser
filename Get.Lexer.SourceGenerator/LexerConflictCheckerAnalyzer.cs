using Get.EasyCSharp.GeneratorTools;
using Get.RegexMachine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Get.Lexer.SourceGenerator;

//[Generator]
[AddAttributeConverter(typeof(LexerAttribute<TempType>))]
[AddAttributeConverter(typeof(RegexAttribute), ParametersAsString = "\"\"")]
[AddAttributeConverter(typeof(StringAttribute), ParametersAsString = "\"\"")]
[AddAttributeConverter(typeof(TypeAttribute<TempType>))]
[AddAttributeConverter(typeof(RegexAttribute<int>), ParametersAsString = "\"\", \"\"", StructName = "RegexAttributeGenericWrapper")]
//[DiagnosticAnalyzer(LanguageNames.CSharp)]
abstract partial class LexerConflictCheckerAnalyzer() : AttributeBaseAnalyzer<LexerAttributeBase, LexerConflictCheckerAnalyzer.LexerAttributeWarpper, TypeDeclarationSyntax, INamedTypeSymbol>(SyntaxKind.ClassDeclaration)
{
    public readonly static DiagnosticDescriptor MalformedRegexes = new(
        "GR1001",
        "Malformed Regex",
        "The regex {0} is malformed: {1}",
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );
    public readonly static DiagnosticDescriptor ConflictFound = new(
        "GR1002",
        "Conflict Found",
        "A conflict between following regex has been found between the current rule ({0}) and {1} other rules ({2}). Please specify the order or rewrite the rule.",
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );

    public static ImmutableArray<DiagnosticDescriptor> StaticSupportedDiagnostics => ImmutableArray.Create(MalformedRegexes, ConflictFound);
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => StaticSupportedDiagnostics;

    protected override void OnPointVisit(OnPointVisitArguments args)
    {
        if (!(args.Symbol.BaseType?.ToString().StartsWith("Get.Lexer.LexerBase") ?? false))
        {
            // type check diagonostic is already done at another generator
            return;
        }
        if (args.Symbol.BaseType.TypeArguments.Length != 2)
        {
            // type check diagonostic is already done at another generator
            return;
        }

        var genContext = args.Context;
        var lexerTokensType = args.AttributeDatas[0].Wrapper.TLexerTokens;
        // this check is not perfect, but it's probably enough
        if (!lexerTokensType.GetAttributes().Any(x => x.AttributeClass?.Name is nameof(CompileTimeConflictCheckAttribute)))
        {
            OnPointVisitShared(lexerTokensType, genContext, args.CancellationToken);
        }
    }
    public static void OnPointVisitShared(ITypeSymbol lexerTokensType, SyntaxNodeAnalysisContext context, CancellationToken CancellationToken)
    {
        var members = lexerTokensType.GetMembers();
        Dictionary<int, List<RegexVal<SyntaxReference>>> regexesByState = [];
        foreach (var token in members)
        {
            CancellationToken.ThrowIfCancellationRequested();
            var TypeAttr = AttributeHelper.TryGetAttributeAnyGeneric<TypeAttribute<TempType>, TypeAttributeWarpper>(context.SemanticModel, token, AttributeDataToTypeAttribute);
            var Regexes = AttributeHelper.GetAttributesAnyGeneric<RegexAttribute, RegexAttributeWarpper>(context.SemanticModel, token, (attrdata, compilation) =>
            {
                if ((!attrdata.AttributeClass?.IsGenericType) ?? false)
                    return AttributeDataToRegexAttribute(attrdata, compilation);
                return null;
            }).ToArray();
            var typedRegexes = AttributeHelper.GetAttributesAnyGeneric<RegexAttribute<TempType>, RegexAttributeGenericWrapper>(context.SemanticModel, token, (attrdata, compilation) =>
            {
                if (attrdata.AttributeClass?.IsGenericType ?? false)
                    return AttributeDataToRegexAttributeGenericWrapper(attrdata, compilation);
                return null;
            }).ToArray();
            // no type check is required as we did it in other generator
            // we just care about checking conflicts
            foreach (var (a, r) in typedRegexes)
            {
                if (!regexesByState.TryGetValue(r.State, out var list))
                {
                    regexesByState[r.State] = list = [];
                }
                // we can specify the genearted value as any value
                // that is useful as we don't care about this value
                // all we care is that there are no conflicts
                list.Add(new RegexVal<SyntaxReference>(r.Regex, a.ApplicationSyntaxReference ?? throw new NullReferenceException(), Order: r.Order));
            }
            CancellationToken.ThrowIfCancellationRequested();
            foreach (var (a, r) in Regexes)
            {
                if (!regexesByState.TryGetValue(r.State, out var list))
                {
                    regexesByState[r.State] = list = [];
                }
                // we can specify the genearted value as any value
                // that is useful as we don't care about this value
                // all we care is that there are no conflicts
                list.Add(new RegexVal<SyntaxReference>(r.Regex, a.ApplicationSyntaxReference ?? throw new NullReferenceException(), Order: r.Order));
            }
        }
        foreach (var kvp in regexesByState)
        {
            CancellationToken.ThrowIfCancellationRequested();
            var (state, rules) = (kvp.Key, kvp.Value);
            try
            {
                // we ignore the dfa if compilation is successful
                _ = RegexCompiler<SyntaxReference>.GenerateDFA(rules, conflictBehavior: RegexConflictBehavior.Throw);
            }
            catch (RegexConflictCompilerException ex)
            {
                var ids = ex.ConflictIds;
                // if it fails due to a conflict, report the error
                for (int i = 0; i < ids.Length; i++)
                {
                    static string Display(string s) => $"@\"{s}\"";
                    context.ReportDiagnostic(Diagnostic.Create(
                        ConflictFound,
                        Location.Create(rules[ids[i]].Value!.SyntaxTree, rules[ids[i]].Value!.Span),
                        Display(rules[ids[i]].Regex),
                        ids.Length - 1,
                        string.Join(
                            ", ",
                            from j in Enumerable.Range(0, ids.Length)
                            where j != i
                            select Display(rules[ids[j]].Regex)
                        )
                    ));
                }
            }
            catch (MultiRegexCompilerException ex)
            {
                var rule = rules[ex.RuleId];
                static string Display(string s) => $"@\"{s}\"";
                context.ReportDiagnostic(Diagnostic.Create(
                    MalformedRegexes,
                    Location.Create(rule.Value!.SyntaxTree, rule.Value!.Span),
                    Display(rule.Regex),
                    ex.InnerException.Message
                ));
            }
        }
    }
    protected override LexerAttributeWarpper? TransformAttribute(AttributeData attributeData, Compilation compilation)
    {
        return AttributeDataToLexerAttribute(attributeData, compilation);
    }
}
