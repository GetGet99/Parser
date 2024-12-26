using Get.EasyCSharp.GeneratorTools;
using Get.EasyCSharp.GeneratorTools.SyntaxCreator.Members;
using Get.RegexMachine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Data;
using System.Linq;
using System.Text;

namespace Get.Lexer.SourceGenerator;
[Generator]
[AddAttributeConverter(typeof(LexerAttribute<TempType>))]
[AddAttributeConverter(typeof(RegexAttribute), ParametersAsString = "\"\"")]
[AddAttributeConverter(typeof(StringAttribute), ParametersAsString = "\"\"")]
[AddAttributeConverter(typeof(TypeAttribute<TempType>))]
[AddAttributeConverter(typeof(RegexAttribute<int>), ParametersAsString = "\"\", \"\"", StructName = "RegexAttributeGenericWrapper")]
partial class LexerGenerator : AttributeBaseGenerator<LexerAttributeBase, LexerGenerator.LexerAttributeWarpper, TypeDeclarationSyntax, INamedTypeSymbol>
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
        new NonLocalizableString($"{nameof(RegexAttribute.ShouldReturnToken)} is false for typed attribute"),
        new NonLocalizableString($"{nameof(RegexAttribute.ShouldReturnToken)} must be true for typed attribute"),
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );
    const string ITokenType = "global::Get.PLShared.IToken";
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
        var baseType = lexerSymbol.BaseType!;
        var termType = new FullType(baseType.TypeArguments[1]);
        var stateType = new FullType(baseType.TypeArguments[0]);
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
                    if (!attr2.ShouldReturnToken)
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
            foreach (var (attrdata, regex) in Regexes)
            {
                if (regex.RawImplementationMethodName is not { } name) continue;
                args.CancellationToken.ThrowIfCancellationRequested();
                if (!lexerSymbol.GetMembers(name).Any())
                {
                    var syntax = attrdata.ApplicationSyntaxReference;
                    diagnostics.Add(Diagnostic.Create(
                        ImplementationMissing,
                        syntax is null ? null : Location.Create(syntax.SyntaxTree, syntax.Span),
                        name
                    ));
                }
                if (regex.ShouldReturnToken)
                    yield return
                    $"""
                    private partial {ITokenType}<{termType}> {name}();
                    """;
                else
                    yield return
                    $"""
                    private partial void {name}();
                    """;
            }
            if (failedTypeCheck)
                continue;
            static string EscapeRegex(string s)
            {
                // if there is a quote character, double it
                return s.Replace("\"", "\"\"");
            }
            foreach (var (_, r) in typedRegexes)
            {
                if (!genereatedRegexes.TryGetValue(r.State, out var sb))
                {
                    genereatedRegexes[r.State] = sb = new();
                }
                sb.AppendLine($"""
                    new(
                        @"{EscapeRegex(r.Regex)}",
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
                if (r.ShouldReturnToken)
                {
                    if (r.RawImplementationMethodName is { } name)
                        sb.AppendLine($"""
                        new(
                            @"{EscapeRegex(r.Regex)}",
                            {name}, // raw implementation
                            {r.Order}
                        ),
                        """);
                    else
                        sb.AppendLine($"""
                        new(
                            @"{EscapeRegex(r.Regex)}",
                            MakeFunc({new FullType(lexerTokensType)}.{token.Name}),
                            {r.Order}
                        ),
                        """);
                }
                else
                {
                    if (r.RawImplementationMethodName is { } name)
                        sb.AppendLine($"""
                        new(
                            @"{EscapeRegex(r.Regex)}",
                            Empty({name}), // raw implementation
                            {r.Order}
                        ),
                        """);
                    else
                        sb.AppendLine($"""
                        new(
                            @"{EscapeRegex(r.Regex)}",
                            Empty(),
                            {r.Order}
                        ),
                        """);
                }
            }
        }
        StringBuilder sb2 = new();
        foreach (var kvp in genereatedRegexes)
        {
            var (state, rules) = (kvp.Key, kvp.Value);
            sb2.AppendLine(
                $$"""
                dict[({{stateType}}){{state}}] = Get.RegexMachine.RegexCompiler<Func<{{ITokenType}}<{{termType}}>?>>.GenerateDFA([
                    {{rules.ToString().IndentWOF()}}
                ], Get.RegexMachine.RegexConflictBehavior.Throw);
                """
            );
        }
        yield return $$"""
        public override Dictionary<{{stateType}}, Get.RegexMachine.RegexCompiler<Func<{{ITokenType}}<{{termType}}>?>>.DFAState> DFASourceGenOutput()
        {
            Dictionary<{{stateType}}, Get.RegexMachine.RegexCompiler<Func<{{ITokenType}}<{{termType}}>?>>.DFAState> dict = [];
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
[Generator]
[AddAttributeConverter(typeof(LexerAttribute<TempType>))]
[AddAttributeConverter(typeof(RegexAttribute), ParametersAsString = "\"\"")]
[AddAttributeConverter(typeof(StringAttribute), ParametersAsString = "\"\"")]
[AddAttributeConverter(typeof(TypeAttribute<TempType>))]
[AddAttributeConverter(typeof(RegexAttribute<int>), ParametersAsString = "\"\", \"\"", StructName = "RegexAttributeGenericWrapper")]
partial class LexerConflictChecker : AttributeBaseGenerator<LexerAttributeBase, LexerConflictChecker.LexerAttributeWarpper, TypeDeclarationSyntax, INamedTypeSymbol>
{
    protected override bool ShouldEmitFiles => false;
    class NonLocalizableString(string s) : LocalizableString
    {
        public string DiagonosticString { get; } = s;
        protected override bool AreEqual(object? other) => other is NonLocalizableString l && l.DiagonosticString == DiagonosticString;
        protected override int GetHash() => DiagonosticString.GetHashCode();
        protected override string GetText(IFormatProvider? formatProvider) => DiagonosticString;
    }
    public readonly static DiagnosticDescriptor MalformedRegexes = new(
        "GR1001",
        new NonLocalizableString("Malformed Regex"),
        new NonLocalizableString("The regex {0} is malformed: {1}"),
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );
    public readonly static DiagnosticDescriptor ConflictFound = new(
        "GR1002",
        new NonLocalizableString("Conflict Found"),
        new NonLocalizableString("A conflict between following regex has been found between the current rule ({0}) and {1} other rules ({2}). Please specify the order or rewrite the rule."),
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );
    protected override string? OnPointVisit(OnPointVisitArguments args)
    {
        if (!(args.Symbol.BaseType?.ToString().StartsWith("Get.Lexer.LexerBase") ?? false))
        {
            // type check diagonostic is already done at another generator
            return null;
        }
        if (args.Symbol.BaseType.TypeArguments.Length != 2)
        {
            // type check diagonostic is already done at another generator
            return null;
        }

        var genContext = args.GenContext;
        var diagnostics = args.Diagnostics;
        var lexerTokensType = args.AttributeDatas[0].Wrapper.TLexerTokens;
        // this check is not perfect, but it's probably enough
        if (!lexerTokensType.GetAttributes().Any(x => x.AttributeClass?.Name is nameof(CompileTimeConflictCheckAttribute)))
        {
            OnPointVisitShared(lexerTokensType, genContext, args.CancellationToken, diagnostics);
        }
        return null;
    }
    public static void OnPointVisitShared(ITypeSymbol lexerTokensType, GeneratorSyntaxContext genContext, CancellationToken CancellationToken, List<Diagnostic> diagnostics)
    {
        var members = lexerTokensType.GetMembers();
        Dictionary<int, List<RegexVal<SyntaxReference>>> regexesByState = [];
        foreach (var token in members)
        {
            CancellationToken.ThrowIfCancellationRequested();
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
                    diagnostics.Add(Diagnostic.Create(
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
                diagnostics.Add(Diagnostic.Create(
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
[Generator]
[AddAttributeConverter(typeof(CompileTimeConflictCheckAttribute))]
partial class RegexConflictChecker : AttributeBaseGenerator<
    CompileTimeConflictCheckAttribute,
    RegexConflictChecker.CompileTimeConflictCheckAttributeWarpper,
    EnumDeclarationSyntax,
    INamedTypeSymbol
>
{
    protected override bool TryRecurseParent => true;
    protected override bool ShouldEmitFiles => false;
    protected override string? OnPointVisit(OnPointVisitArguments args)
    {
        LexerConflictChecker.OnPointVisitShared(args.Symbol, args.GenContext, args.CancellationToken, args.Diagnostics);
        return null;
    }
    protected override CompileTimeConflictCheckAttributeWarpper? TransformAttribute(AttributeData attributeData, Compilation compilation)
    {
        return AttributeDataToCompileTimeConflictCheckAttribute(attributeData, compilation);
    }
}