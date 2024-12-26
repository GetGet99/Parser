using Get.EasyCSharp.GeneratorTools;
using Get.EasyCSharp.GeneratorTools.SyntaxCreator.Members;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Data;
using System.Linq;
using System.Text;

namespace Get.Parser.SourceGenerator;
[Generator]
[AddAttributeConverter(typeof(ParserAttribute<TempType>))]
[AddAttributeConverter(typeof(RuleAttribute))]
[AddAttributeConverter(typeof(TypeAttribute<TempType>))]
[AddAttributeConverter(typeof(RegexAttribute<int>), ParametersAsString = "\"\", \"\"", StructName = "RegexAttributeGenericWrapper")]
partial class ParserGenerator : AttributeBaseGenerator<LexerAttributeBase, LexerGenerator.LexerAttributeWarpper, TypeDeclarationSyntax, INamedTypeSymbol>
{
    class NonLocalizableString(string s) : LocalizableString
    {
        public string DiagonosticString { get; } = s;
        protected override bool AreEqual(object? other) => other is NonLocalizableString l && l.DiagonosticString == DiagonosticString;
        protected override int GetHash() => DiagonosticString.GetHashCode();
        protected override string GetText(IFormatProvider? formatProvider) => DiagonosticString;
    }
    readonly static DiagnosticDescriptor NotLexerBase = new(
        "GL1001",
        new NonLocalizableString("Type does not implement Get.Lexer.LexerBase"),
        new NonLocalizableString("Type must not implement Get.Lexer.LexerBase"),
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );
    readonly static DiagnosticDescriptor RegexReturnWrongType = new(
        "GL1002",
        new NonLocalizableString("Regex does not return the correct type"),
        new NonLocalizableString("Regex must return the value of type {0}"),
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );
    readonly static DiagnosticDescriptor RegexReturnTypeAmbiguous = new(
        "GL1003",
        new NonLocalizableString("Regex return type is ambiguous"),
        new NonLocalizableString("Regex return type is ambiguous. Please specify type using Type<T>."),
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );
    readonly static DiagnosticDescriptor ImplementationMissing = new(
        "GL1004",
        new NonLocalizableString("The implementation of the method is missing"),
        new NonLocalizableString("The implementation of the method {0} is missing. Please implement it."),
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );
    readonly static DiagnosticDescriptor NoOutputOnTypedRegex = new(
        "GL1005",
        new NonLocalizableString($"{nameof(RegexAttribute.ShouldOutputToken)} is false for typed attribute"),
        new NonLocalizableString($"{nameof(RegexAttribute.ShouldOutputToken)} must be true for typed attribute"),
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );
    protected override string? OnPointVisit(OnPointVisitArguments args)
    {
        if (!(args.Symbol.BaseType?.ToString().StartsWith("Get.Lexer.LexerBase") ?? false))
        {
            args.Diagnostics.Add(Diagnostic.Create(NotLexerBase, args.SyntaxNode.BaseList?.GetLocation() ?? args.SyntaxNode.Identifier.GetLocation()));
            return null;
        }
        if (args.Symbol.BaseType.TypeArguments.Length != 2)
        {
            args.Diagnostics.Add(Diagnostic.Create(NotLexerBase, args.SyntaxNode.BaseList?.GetLocation() ?? args.SyntaxNode.Identifier.GetLocation()));
            return null;
        }
        return OnPointVisit2(args).JoinDoubleNewLine();
    }
    protected IEnumerable<string> OnPointVisit2(OnPointVisitArguments args)
    {
        var genContext = args.GenContext;
        var diagnostics = args.Diagnostics;
        var lexerSymbol = args.Symbol;
        var lexerTokensType = args.AttributeDatas[0].Wrapper.TLexerTokens;
        var members = lexerTokensType.GetMembers();
        Dictionary<int, StringBuilder> genereatedRegexes = [];
        foreach (var token in members)
        {
            args.CancellationToken.ThrowIfCancellationRequested();
            var TypeAttr = AttributeHelper.TryGetAttributeAnyGeneric<TypeAttribute<TempType>, TypeAttributeWarpper>(genContext, token, AttributeDataToTypeAttribute);
            var Regexes = AttributeHelper.GetAttributesAnyGeneric<RegexAttribute, RegexAttributeWarpper>(genContext, token, (attrdata, compilation) =>
            {
                if ((!attrdata.AttributeClass?.IsGenericType) ?? false)
                    return AttributeDataToRegexAttribute(attrdata, compilation);
                return null;
            }).ToArray();
            var typedRegexes = AttributeHelper.GetAttributesAnyGeneric<RegexAttribute<TempType>, RegexAttributeGenericWrapper>(genContext, token, (attrdata, compilation) =>
            {
                if (attrdata.AttributeClass?.IsGenericType ?? false)
                    return AttributeDataToRegexAttributeGenericWrapper(attrdata, compilation);
                return null;
            }).ToArray();
            bool failedTypeCheck = false;
            var regexType = TypeAttr?.Serialized.T;
        RegexTypeCheck:
            if (regexType is not null)
            {
                args.CancellationToken.ThrowIfCancellationRequested();
                // TYPE CHECKING
                foreach (var (RealAttributeData, _) in Regexes)
                {
                    failedTypeCheck = true;
                    var syntax = RealAttributeData.ApplicationSyntaxReference;
                    diagnostics.Add(Diagnostic.Create(
                        RegexReturnWrongType,
                        syntax is null ? null : Location.Create(syntax.SyntaxTree, syntax.Span),
                        regexType
                    ));
                }
                foreach (var (RealAttributeData, attr2) in typedRegexes)
                {
                    if (!attr2.ShouldOutputToken)
                    {
                        var syntax2 = RealAttributeData.ApplicationSyntaxReference;
                        diagnostics.Add(Diagnostic.Create(
                            NoOutputOnTypedRegex,
                            syntax2 is null ? null : Location.Create(syntax2.SyntaxTree, syntax2.Span)
                        ));
                    }
                    if (attr2.T.IsSubclassFrom(regexType))
                        continue;
                    failedTypeCheck = true;
                    var syntax = RealAttributeData.ApplicationSyntaxReference;
                    diagnostics.Add(Diagnostic.Create(
                        RegexReturnWrongType,
                        syntax is null ? null : Location.Create(syntax.SyntaxTree, syntax.Span),
                        regexType
                    ));
                }
            }
            else
            {
                // TYPE CHECKING
                // no type is declared
                var allTypes =
                    (from tr in typedRegexes
                     select tr.Serialized.T).ToArray();
                if (allTypes.Length > 0)
                {
                    if (allTypes.Any(x => !x.Equals(allTypes[0], SymbolEqualityComparer.Default)))
                    {
                        // the return type is ambiguous
                        failedTypeCheck = true;
                        diagnostics.Add(Diagnostic.Create(
                            RegexReturnTypeAmbiguous,
                            token.Locations[0]
                        ));
                    }
                    else
                    {
                        // set the type
                        regexType = allTypes[0];
                        // repeat the type check using the implicit type
                        goto RegexTypeCheck;
                    }
                }
            }
            foreach (var (attrdata, typedRegex) in typedRegexes)
            {
                args.CancellationToken.ThrowIfCancellationRequested();
                if (!lexerSymbol.GetMembers(typedRegex.ImplementationMethodName).Any())
                {
                    var syntax = attrdata.ApplicationSyntaxReference;
                    diagnostics.Add(Diagnostic.Create(
                        ImplementationMissing,
                        syntax is null ? null : Location.Create(syntax.SyntaxTree, syntax.Span),
                        typedRegex.ImplementationMethodName
                    ));
                }
                yield return
                    $"""
                    private partial {new FullType(typedRegex.T)} {typedRegex.ImplementationMethodName}();
                    """;
            }
            if (failedTypeCheck)
                continue;
            foreach (var (_, r) in typedRegexes)
            {
                if (!genereatedRegexes.TryGetValue(r.State, out var sb))
                {
                    genereatedRegexes[r.State] = sb = new();
                }
                sb.AppendLine($"""
                    new(
                        @"{r.Regex}",
                        MakeFunc(
                            {new FullType(lexerTokensType)}.{token.Name},
                            {r.ImplementationMethodName}
                        ),
                        {r.Order}
                    ),
                    """);
            }
            foreach (var (_, r) in Regexes)
            {
                if (!genereatedRegexes.TryGetValue(r.State, out var sb))
                {
                    genereatedRegexes[r.State] = sb = new();
                }
                if (r.ShouldOutputToken)
                {
                    sb.AppendLine($"""
                        new(
                            @"{r.Regex}",
                            MakeFunc({new FullType(lexerTokensType)}.{token.Name}),
                            {r.Order}
                        ),
                        """);
                }
                else
                {
                    sb.AppendLine($"""
                        new(
                            @"{r.Regex}",
                            Empty(),
                            {r.Order}
                        ),
                        """);
                }
            }
        }
        var baseType = lexerSymbol.BaseType!;
        var stateType = new FullType(baseType.TypeArguments[0]);
        var termType = new FullType(baseType.TypeArguments[1]);
        StringBuilder sb2 = new();
        foreach (var kvp in genereatedRegexes)
        {
            var (state, rules) = (kvp.Key, kvp.Value);
            sb2.AppendLine(
                $$"""
                dict[({{stateType}}){{state}}] = Get.RegexMachine.RegexCompiler<Func<Get.PLShared.IToken<{{termType}}>?>>.GenerateDFA([
                    {{rules.ToString().IndentWOF()}}
                ], Get.RegexMachine.RegexConflictBehavior.Throw);
                """
            );
        }
        yield return $$"""
        public override Dictionary<{{stateType}}, Get.RegexMachine.RegexCompiler<Func<Get.PLShared.IToken<{{termType}}>?>>.DFAState> DFASourceGenOutput()
        {
            Dictionary<{{stateType}}, Get.RegexMachine.RegexCompiler<Func<Get.PLShared.IToken<{{termType}}>?>>.DFAState> dict = [];
            {{sb2.ToString().IndentWOF()}}
            return dict;
        }
        """;
    }
    protected override LexerAttributeWarpper? TransformAttribute(AttributeData attributeData, Compilation compilation)
    {
        return AttributeDataToLexerAttribute(attributeData, compilation);
    }
}
// just for sake of being able to use AddAttributeConverter
enum TempType : byte { }