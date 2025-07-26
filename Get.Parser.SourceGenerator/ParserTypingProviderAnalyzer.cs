using Get.EasyCSharp.GeneratorTools;
using Get.EasyCSharp.GeneratorTools.SyntaxCreator.Members;
using Get.Lexer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Get.Parser.SourceGenerator;
[AddAttributeConverter(typeof(ParserAttribute), ParametersAsString = "startNode: 0")]
[AddAttributeConverter(typeof(RuleAttribute))]
[AddAttributeConverter(typeof(TypeAttribute<TempType>))]
[AddAttributeConverter(typeof(Lexer.TypeAttribute<TempType>), MethodName = "LexerTypeAttrGen", StructName = "LexerTypeAttributeWrapper")]
[AddAttributeConverter(typeof(RegexAttribute<TempType>), ParametersAsString = "\"\", \"\"")]
[AddAttributeConverter(typeof(PrecedenceAttribute))]
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class ParserTypingProviderAnalyzer : DiagnosticAnalyzer
{
    delegate Diagnostic DiagnosticCreator(Location loc);
    record ParserClass(INamedTypeSymbol Parser, ITypeSymbol Terminal, ITypeSymbol Nonterminal, Dictionary<object, string> TerminalNames, Dictionary<object, string> NonterminalNames);
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction((ctx =>
        {
            // Safe per-compilation cache
            var parserRemoveKeys = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var parserMapping = new Dictionary<ISymbol, ParserClass>(SymbolEqualityComparer.Default);
            var docCache = new ConcurrentDictionary<ISymbol, DiagnosticCreator?>(SymbolEqualityComparer.Default);
            var symbols = ctx.Compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type);
            foreach (var symbol in symbols)
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();
                if (symbol is INamedTypeSymbol typeSymbol && (typeSymbol.BaseType?.ToString().StartsWith("Get.Parser.ParserBase") ?? false))
                {
                    if (typeSymbol.BaseType.TypeArguments.Length != 3)
                        continue;
                    var terms = (from member in typeSymbol.BaseType.TypeArguments[0].GetMembers()
                                   let field = member is IFieldSymbol sym ? sym : null
                                   where field != null && field.ConstantValue != null
                                 select (field.ConstantValue, field.Name)
                         ).ToDictionary(x => x.ConstantValue, x => x.Name);
                    var nonTerms = (from member in typeSymbol.BaseType.TypeArguments[1].GetMembers()
                                 let field = member is IFieldSymbol sym ? sym : null
                                 where field != null && field.ConstantValue != null
                                 select (field.ConstantValue, field.Name)
                         ).ToDictionary(x => x.ConstantValue, x => x.Name);
                    ParserClass parserClass = new(typeSymbol, typeSymbol.BaseType.TypeArguments[0], typeSymbol.BaseType.TypeArguments[1], terms, nonTerms);
                    // add mapping for parser, terminal, and nonterminal
                    parserMapping[typeSymbol] = parserClass;
                    if (!parserMapping.ContainsKey(typeSymbol.BaseType.TypeArguments[0]))
                        parserMapping[typeSymbol.BaseType.TypeArguments[0]] = parserClass;
                    else
                        // ambiguous, we don't support it for now
                        parserRemoveKeys.Add(typeSymbol.BaseType.TypeArguments[0]);
                    if (!parserMapping.ContainsKey(typeSymbol.BaseType.TypeArguments[1]))
                        parserMapping[typeSymbol.BaseType.TypeArguments[1]] = parserClass;
                    else
                        // ambiguous, we don't support it for now
                        parserRemoveKeys.Add(typeSymbol.BaseType.TypeArguments[1]);
                }
            }
            foreach (var remKey in parserRemoveKeys)
            {
                parserMapping.Remove(remKey);
            }
            void syntaxAction(SyntaxNodeAnalysisContext ctx)
            {
                Location location;
                IFieldSymbol symbol;
                if (ctx.Node is not IdentifierNameSyntax syntaxNode)
                {
                    var node = ((EnumMemberDeclarationSyntax)ctx.Node);
                    location = node.Identifier.GetLocation();
                    if (ctx.SemanticModel.GetDeclaredSymbol(node) is not IFieldSymbol sym)
                        return;
                    symbol = sym;
                } else
                {
                    location = syntaxNode.GetLocation();
                    if (ctx.SemanticModel.GetSymbolInfo(syntaxNode).Symbol is not IFieldSymbol sym)
                        return;
                    symbol = sym;
                }
                if (symbol.ContainingType.TypeKind is not TypeKind.Enum)
                    return;
                var doc = docCache.GetOrAdd(symbol, delegate
                {
                    if (!parserMapping.TryGetValue(symbol.ContainingType, out var parserInfo))
                        return null;
                    return this.GetDoc(new(ctx, symbol, parserInfo, ctx.CancellationToken));
                });
                if (doc != null)
                    ctx.ReportDiagnostic(doc(location));
            }
            ctx.RegisterSyntaxNodeAction(syntaxAction, SyntaxKind.IdentifierName);
            ctx.RegisterSyntaxNodeAction(syntaxAction, SyntaxKind.EnumMemberDeclaration);
        }));
    }
    readonly record struct OnPointVisitArguments(
        SyntaxNodeAnalysisContext Context,
        IFieldSymbol Symbol,
        ParserClass ParserInfo,
        CancellationToken CancellationToken
    );
    static RuleAttrSyntaxParser RuleAttrSyntaxParser { get; } = new RuleAttrSyntaxParser();
    static PrecedenceAttrSyntaxParser PrecedenceAttrSyntaxParser { get; } = new PrecedenceAttrSyntaxParser();


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        ParserTerminalTypeInfo,
        ParserTerminalNoTypeInfo,
        ParserNonterminalTypeInfo,
        ParserNonterminalNoTypeInfo
    );
    readonly static DiagnosticDescriptor ParserNonterminalTypeInfo = new(
        "GPI001",
        "A nonterminal with type",
        "{0} is a nonterminal of type {1}\n\nItems:\n{2}",
        "Get.Parser",
        DiagnosticSeverity.Hidden,
        true
    );
    readonly static DiagnosticDescriptor ParserNonterminalNoTypeInfo = new(
        "GPI002",
        "A nonterminal without type",
        "{0} is a nonterminal with no type\n\nItems:\n{1}",
        "Get.Parser",
        DiagnosticSeverity.Hidden,
        true
    );
    readonly static DiagnosticDescriptor ParserTerminalTypeInfo = new(
        "GPI003",
        "A terminal with type",
        "{0} is a terminal of type {1}",
        "Get.Parser",
        DiagnosticSeverity.Hidden,
        true
    );
    readonly static DiagnosticDescriptor ParserTerminalNoTypeInfo = new(
        "GPI004",
        "A terminal without type",
        "{0} is a terminal with no type",
        "Get.Parser",
        DiagnosticSeverity.Hidden,
        true
    );
    enum FieldKind
    {
        Terminal, Nonterminal
    }
    DiagnosticCreator GetDoc(OnPointVisitArguments args)
    {
        var genContext = args.Context;
        var associativityType = genContext.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Associativity).FullName);
        var keywordType = genContext.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ParserSourceGeneratorKeywords).FullName)!;
        
        var fieldSymbol = args.Symbol;
        
        ITypeSymbol? fieldType = null;
        // Get.Lexer terminal info
        var attrs = fieldSymbol.GetAttributes();
        var type = AttributeHelper.TryGetAttributeAnyGeneric<Lexer.TypeAttribute<TempType>, LexerTypeAttributeWrapper>(genContext.SemanticModel, fieldSymbol, LexerTypeAttrGen);
        if (type.HasValue)
        {
            // on lexer, there is type checking
            // but we can just take the type here as
            // the user can see the error from the lexer side
            fieldType = type.Value.Serialized.T;
            goto endTyping;
        }
        // retry on regex attribute
        var typedRegexes = AttributeHelper.GetAttributesAnyGeneric<RegexAttribute<TempType>, RegexAttributeWarpper>(genContext.SemanticModel, fieldSymbol, (attrdata, compilation) =>
        {
            if (attrdata.AttributeClass?.IsGenericType ?? false)
                return AttributeDataToRegexAttribute(attrdata, compilation);
            return null;
        });
        var types = typedRegexes.Select(x => x.Serialized.T).Distinct(SymbolEqualityComparer.Default).ToList();
        if (types.Count is 1)
        {
            fieldType = (ITypeSymbol)types[0]!;
            goto endTyping;
        }
        if (types.Count > 1)
        {
            // the type is ambiguous, lexer won't allow it
            // but we will just say it has no type here
            fieldType = null;
            goto endTyping;
        }
        var type2 = AttributeHelper.TryGetAttributeAnyGeneric<TypeAttribute<TempType>, TypeAttributeWarpper>(genContext.SemanticModel, fieldSymbol, AttributeDataToTypeAttribute);
        if (type2.HasValue)
        {
            fieldType = type2.Value.Serialized.T;
            goto endTyping;
        }

        fieldType = null;
        goto endTyping;
    endTyping:
        var fieldKind = fieldSymbol.Type.Equals(args.ParserInfo.Terminal, SymbolEqualityComparer.Default) ? FieldKind.Terminal : FieldKind.Nonterminal;
        StringBuilder rules = new();
        if (fieldKind is FieldKind.Nonterminal)
        {
            foreach (var (raw, _) in AttributeHelper.GetAttributes<RuleAttribute, RuleAttributeWarpper>(genContext.SemanticModel, fieldSymbol, (_, comp) => new RuleAttributeWarpper(comp)))
            {
                var ruleargs = raw.ConstructorArguments[0].Values;
                Rule rule;
                try
                {
                    rule = RuleAttrSyntaxParser.Parse(ruleargs, args.ParserInfo.Terminal, args.ParserInfo.Nonterminal, keywordType);
                }
                catch (LRParserRuntimeUnexpectedInputException)
                {
                    var syn = raw.ApplicationSyntaxReference;
                    continue;
                }
                catch (LRParserRuntimeUnexpectedEndingException)
                {
                    var syn = raw.ApplicationSyntaxReference;
                    continue;
                }
                var (eles, constParams, red, ruleprec) = rule;
                rules.Append($"#{fieldSymbol.Name} -> ");
                foreach (var elem in eles)
                    if (elem.Raw.IsTerminal)
                        rules.Append($"!{args.ParserInfo.TerminalNames[elem.Raw.RawEnum]} ");
                    else
                        rules.Append($"#{args.ParserInfo.NonterminalNames[elem.Raw.RawEnum]} ");
                rules.AppendLine();
            }
            // remove last \n
            if (rules.Length > 0) rules.Remove(rules.Length - 1, 1);
        }
        return (loc) =>
        {
            if (fieldType is null)
            {
                if (fieldKind is FieldKind.Terminal)
                    return Diagnostic.Create(ParserTerminalNoTypeInfo, loc, fieldSymbol);
                else
                    return Diagnostic.Create(ParserNonterminalNoTypeInfo, loc, fieldSymbol, rules);
            } else
            {
                if (fieldKind is FieldKind.Terminal)
                    return Diagnostic.Create(ParserTerminalTypeInfo, loc, fieldSymbol, fieldType);
                else
                    return Diagnostic.Create(ParserNonterminalTypeInfo, loc, fieldSymbol, fieldType, rules);
            }
        };
    }
}