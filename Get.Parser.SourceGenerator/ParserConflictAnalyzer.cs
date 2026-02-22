using Get.EasyCSharp.GeneratorTools;
using Get.EasyCSharp.GeneratorTools.SyntaxCreator.Members;
using Get.Lexer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Get.Parser.SourceGenerator;
[AddAttributeConverter(typeof(ParserAttribute), ParametersAsString = "startNode: 0")]
[AddAttributeConverter(typeof(RuleAttribute))]
[AddAttributeConverter(typeof(TypeAttribute<TempType>))]
[AddAttributeConverter(typeof(Lexer.TypeAttribute<TempType>), MethodName = "LexerTypeAttrGen", StructName = "LexerTypeAttributeWrapper")]
[AddAttributeConverter(typeof(RegexAttribute<TempType>), ParametersAsString = "\"\", \"\"")]
[AddAttributeConverter(typeof(PrecedenceAttribute))]
[Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer(LanguageNames.CSharp)]
partial class ParserConflictAnalyzer() : AttributeBaseAnalyzer<ParserAttribute, ParserConflictAnalyzer.ParserAttributeWarpper, TypeDeclarationSyntax, INamedTypeSymbol>(SyntaxKind.ClassDeclaration)
{
    static RuleAttrSyntaxParser RuleAttrSyntaxParser { get; } = new RuleAttrSyntaxParser();
    static PrecedenceAttrSyntaxParser PrecedenceAttrSyntaxParser { get; } = new PrecedenceAttrSyntaxParser();


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        ShiftReduceConflict, ReduceReduceConflict
    );
    readonly static DiagnosticDescriptor ShiftReduceConflict = new(
        "GPC001",
        "Shift-Reduce Conflict",
        "{0}",
        "Get.Parser",
        DiagnosticSeverity.Error,
        true
    );
    readonly static DiagnosticDescriptor ReduceReduceConflict = new(
        "GPC002",
        "Reduce-Reduce Conflict",
        "{0}",
        "Get.Parser",
        DiagnosticSeverity.Error,
        true
    );
    protected override void OnPointVisit(OnPointVisitArguments args)
    {
        if (!(args.Symbol.BaseType?.ToString().StartsWith("Get.Parser.ParserBase") ?? false))
        {
            return;
        }
        if (args.Symbol.BaseType.TypeArguments.Length != 3)
        {
            return;
        }
        OnPointVisit2(args);
    }
    protected void OnPointVisit2(OnPointVisitArguments args)
    {
        var genContext = args.Context;
        var lexerSymbol = args.Symbol;
        var thisType = args.Symbol;
        var baseType = args.Symbol.BaseType!;
        var terminalType = baseType.TypeArguments[0];
        var nonTerminalType = baseType.TypeArguments[1];
        var associativityType = genContext.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Associativity).FullName);
        var keywordType = genContext.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ParserSourceGeneratorKeywords).FullName);
        var nonTerminalFT = new FullType(nonTerminalType);
        var terminalFT = new FullType(terminalType);

        // PASS 1: COLLECT TYPE INFORMATION
        Dictionary<object, ITypeSymbol?> TerminalTypes = [];
        Dictionary<object, ITypeSymbol?> NonTerminalTypes = [];
        Dictionary<object, string> TerminalNames = [];
        Dictionary<object, string> NonTerminalNames = [];
        if (args.AttributeDatas[0].Wrapper.UseGetLexerTypeInformation)
        {
            foreach (var t in terminalType.GetMembers())
            {
                if (t is IFieldSymbol fieldSymbol)
                {
                    var value = fieldSymbol.ConstantValue;
                    if (value is null) continue;
                    var attrs = t.GetAttributes();
                    var type = AttributeHelper.TryGetAttributeAnyGeneric<Lexer.TypeAttribute<TempType>, LexerTypeAttributeWrapper>(genContext.SemanticModel, t, LexerTypeAttrGen);
                    if (type.HasValue)
                        // on lexer, there is type checking
                        // but we can just take the type here as
                        // the user can see the error from the lexer side
                        TerminalTypes[value] = type.Value.Serialized.T;
                    else
                    {
                        // retry on regex attribute
                        var typedRegexes = AttributeHelper.GetAttributesAnyGeneric<RegexAttribute<TempType>, RegexAttributeWarpper>(genContext.SemanticModel, t, (attrdata, compilation) =>
                        {
                            if (attrdata.AttributeClass?.IsGenericType ?? false)
                                return AttributeDataToRegexAttribute(attrdata, compilation);
                            return null;
                        });
                        var types = typedRegexes.Select(x => x.Serialized.T).Distinct(SymbolEqualityComparer.Default).ToList();
                        if (types.Count is 1)
                            TerminalTypes[value] = (ITypeSymbol)types[0]!;
                        else
                            // the type is ambiguous, lexer won't allow it
                            // but we will just say it has no type here
                            TerminalTypes[value] = null;
                        TerminalNames[value] = fieldSymbol.Name;
                    }
                }
            }
        }
        else
        {
            foreach (var t in terminalType.GetMembers())
            {
                if (t is IFieldSymbol fieldSymbol)
                {
                    var value = fieldSymbol.ConstantValue;
                    if (value is null) continue;
                    var attrs = t.GetAttributes();
                    var type = AttributeHelper.TryGetAttributeAnyGeneric<TypeAttribute<TempType>, TypeAttributeWarpper>(genContext.SemanticModel, t, AttributeDataToTypeAttribute);
                    if (type.HasValue)
                        TerminalTypes[value] = type.Value.Serialized.T;
                    else
                        TerminalTypes[value] = null;
                    TerminalNames[value] = fieldSymbol.Name;
                }
            }
        }
        foreach (var nt in nonTerminalType.GetMembers())
        {
            if (nt is IFieldSymbol fieldSymbol)
            {
                var value = fieldSymbol.ConstantValue;
                if (value is null) continue;
                var attrs = nt.GetAttributes();
                var type = AttributeHelper.TryGetAttributeAnyGeneric<TypeAttribute<TempType>, TypeAttributeWarpper>(genContext.SemanticModel, nt, AttributeDataToTypeAttribute);
                if (type.HasValue)
                    NonTerminalTypes[value] = type.Value.Serialized.T;
                else
                    NonTerminalTypes[value] = null;
                NonTerminalNames[value] = fieldSymbol.Name;
            }
        }
        // PASS 2: DO STUFF
        // PRECEDENCE
        List<PrecedenceItem> precedenceList = [];
        var pd = AttributeHelper.TryGetAttribute<PrecedenceAttribute, PrecedenceAttributeWarpper>(genContext.SemanticModel, thisType, (_, comp) => new PrecedenceAttributeWarpper(comp));
        if (pd.HasValue)
        {
            var (raw, _) = pd.Value;
            var precedenceArgs = raw.ConstructorArguments[0].Values;
            if (associativityType is null)
                goto exit;
            try
            {
                precedenceList = PrecedenceAttrSyntaxParser.Parse(precedenceArgs, terminalType, associativityType);
            }
            catch (LRParserRuntimeUnexpectedInputException e)
            {
                goto exit;
            }
            catch (LRParserRuntimeUnexpectedEndingException e)
            {
                goto exit;
            }
        }
    exit:
        StringBuilder sb = new();
        List<ICFGRule> rules = [];
        foreach (var nt in nonTerminalType.GetMembers())
        {
            if (nt is not IFieldSymbol fieldSymbol) continue;
            var value = fieldSymbol.ConstantValue;
            if (value is null) continue;

            foreach (var (raw, _) in AttributeHelper.GetAttributes<RuleAttribute, RuleAttributeWarpper>(genContext.SemanticModel, nt, (_, comp) => new RuleAttributeWarpper(comp)))
            {
                var ruleargs = raw.ConstructorArguments[0].Values;
                Rule rule;
                try
                {
                    rule = RuleAttrSyntaxParser.Parse(ruleargs, terminalType, nonTerminalType, keywordType);
                }
                catch (LRParserRuntimeUnexpectedInputException e)
                {
                    continue;
                }
                catch (LRParserRuntimeUnexpectedEndingException e)
                {
                    continue;
                }
                var nttype = NonTerminalTypes[value];
                var (eles, constParams, red, ruleprec) = rule;
                //eles[0].Raw.
                List<ISyntaxElement> expr = [];
                foreach (var ele in eles)
                {
                    if (ele.Raw.IsTerminal)
                    {
                        if (ele.Raw.RawEnum == ErrorTerminal.Singleton)
                        {
                            expr.Add(ErrorTerminal.Singleton);
                        }
                        else
                        {
                            expr.Add(new Terminal(ele.Raw.RawEnum) { Name = TerminalNames[ele.Raw.RawEnum] });
                        }
                    }
                    else
                        expr.Add(new NonTerminal(ele.Raw.RawEnum) { Name = NonTerminalNames[ele.Raw.RawEnum] });
                }
                var syn = raw.ApplicationSyntaxReference;
                rules.Add(new CFGRule(syn is null ? nt.Locations[0] : Location.Create(syn.SyntaxTree, syn.Span), new(value) { Name = NonTerminalNames[value] }, expr, ruleprec is not null ? new(ruleprec) { Name = TerminalNames[ruleprec] } : null));
            }
        }
        try
        {
            var startNode = args.AttributeDatas[0].Wrapper.startNode;
            new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal>.Default).CreateDFA(
                rules, new NonTerminal(startNode) { Name = NonTerminalNames[startNode] },
                precedenceList.Count is 0 ? [] :
                [.. from prec in precedenceList select
                    ((ITerminal[])[.. from term in prec.RawEnumTerminals select (ITerminal)new Terminal(term) { Name = TerminalNames[term] }],
                    prec.Associativity)]
            );
        }
        catch (LRConflictException conflict)
        {
            foreach (var item in conflict.ConflictedItems)
            {
                var rule = (CFGRule)item.OriginalCFGRule;
                genContext.ReportDiagnostic(Diagnostic.Create(
                    conflict.ConflictType == ConflictType.ShiftReduce ? ShiftReduceConflict : ReduceReduceConflict,
                    rule.Location,
                    conflict.Message
                ));
            }
        }
    }
    record Terminal(object Value) : ITerminal
    {
        public required string Name { get; init; }
        public override string ToString() => $"!{Name}";
    }
    record NonTerminal(object Value) : INonTerminal
    {
        public required string Name { get; init; }
        public override string ToString() => $"#{Name}";
    }
    record CFGRule(Location Location, NonTerminal Target, IReadOnlyList<ISyntaxElement> Expressions, Terminal? Precedence) : ICFGRuleWithPrecedence
    {
        ITerminal? ICFGRuleWithPrecedence.PrecedenceTerminal => Precedence;

        INonTerminal ICFGRule.Target => Target;

        INonTerminalValue ICFGRule.GetValue(ISyntaxElementValue[] value) => throw new NotImplementedException();
    }
    protected override ParserAttributeWarpper? TransformAttribute(AttributeData attributeData, Compilation compilation)
    {
        return AttributeDataToParserAttribute(attributeData, compilation);
    }
}
