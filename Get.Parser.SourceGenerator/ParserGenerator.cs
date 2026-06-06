using Get.EasyCSharp.GeneratorTools;
using Get.Lexer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Get.Parser.SourceGenerator;

[Generator]
[AddAttributeConverter(typeof(ParserAttribute), ParametersAsString = "startNode: 0")]
[AddAttributeConverter(typeof(RuleAttribute))]
[AddAttributeConverter(typeof(TypeAttribute<TempType>))]
[AddAttributeConverter(typeof(Lexer.TypeAttribute<TempType>), MethodName = "LexerTypeAttrGen", StructName = "LexerTypeAttributeWrapper")]
[AddAttributeConverter(typeof(RegexAttribute<TempType>), ParametersAsString = "\"\", \"\"")]
[AddAttributeConverter(typeof(PrecedenceAttribute))]
partial class ParserGenerator : AttributeBaseGenerator<ParserAttribute, ParserGenerator.ParserAttributeWarpper, TypeDeclarationSyntax, INamedTypeSymbol>
{
    protected override string? OnPointVisit(OnPointVisitArguments args)
    {
        if (!ParserBaseHelper.TryGetParserBaseTypes(args.Symbol, out _, out _))
            return null;
        return OnPointVisit2(args).JoinDoubleNewLine();
    }
    protected IEnumerable<string> OnPointVisit2(OnPointVisitArguments args)
    {
        var genContext = args.GenContext;
        var diagnostics = args.Diagnostics;
        var thisType = args.Symbol;
        var baseType = args.Symbol.BaseType!;
        var (terminalType, nonTerminalType, associativityType, keywordType, terminalFT, nonTerminalFT) =
            ParserBaseHelper.SetupParserVariables(genContext.SemanticModel, baseType);

        // PASS 1: COLLECT TYPE INFORMATION
        Dictionary<object, ITypeSymbol?> TerminalTypes = [];
        Dictionary<object, ITypeSymbol?> NonTerminalTypes = [];
        ParserBaseHelper.CollectTerminalTypes(terminalType, TerminalTypes, t =>
        {
            if (args.AttributeDatas[0].Wrapper.UseGetLexerTypeInformation)
            {
                var type = AttributeHelper.TryGetAttributeAnyGeneric<Lexer.TypeAttribute<TempType>, LexerTypeAttributeWrapper>(genContext.SemanticModel, t, LexerTypeAttrGen);
                if (type.HasValue)
                    return type.Value.Serialized.T;
                var typedRegexes = AttributeHelper.GetAttributesAnyGeneric<RegexAttribute<TempType>, RegexAttributeWarpper>(genContext.SemanticModel, t, (attrdata, compilation) =>
                {
                    if (attrdata.AttributeClass?.IsGenericType ?? false)
                        return AttributeDataToRegexAttribute(attrdata, compilation);
                    return null;
                });
                var types = typedRegexes.Select(x => x.Serialized.T).Distinct(SymbolEqualityComparer.Default).ToList();
                if (types.Count is 1)
                    return (ITypeSymbol)types[0]!;
                return null;
            }
            else
            {
                var type = AttributeHelper.TryGetAttributeAnyGeneric<TypeAttribute<TempType>, TypeAttributeWarpper>(genContext.SemanticModel, t, AttributeDataToTypeAttribute);
                return type?.Serialized.T;
            }
        });
        ParserBaseHelper.CollectNonTerminalTypes(nonTerminalType, NonTerminalTypes, nt =>
        {
            var type = AttributeHelper.TryGetAttributeAnyGeneric<TypeAttribute<TempType>, TypeAttributeWarpper>(genContext.SemanticModel, nt, AttributeDataToTypeAttribute);
            return type?.Serialized.T;
        });
        // PASS 2: DO STUFF
        // PRECEDENCE
        List<PrecedenceItem> precedenceList = [];
        var pd = AttributeHelper.TryGetAttribute<PrecedenceAttribute, PrecedenceAttributeWarpper>(genContext.SemanticModel, thisType, (_, comp) => new PrecedenceAttributeWarpper(comp));
        if (pd.HasValue)
        {
            var (raw, _) = pd.Value;
            try
            {
                precedenceList = ParserBaseHelper.ParsePrecedenceCore(genContext.SemanticModel, terminalType, associativityType, raw);
            }
            catch (LRParserRuntimeUnexpectedInputException)
            {
            }
            catch (LRParserRuntimeUnexpectedEndingException)
            {
            }
        }
        StringBuilder sb = new();

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
                    rule = ParserBaseHelper.RuleAttrSyntaxParser.Parse(ruleargs, terminalType, nonTerminalType, keywordType);
                }
                catch (LRParserRuntimeUnexpectedInputException)
                {
                    continue;
                }
                catch (LRParserRuntimeUnexpectedEndingException)
                {
                    continue;
                }
                var nttype = NonTerminalTypes[value];
                var (eles, constParams, red, ruleprec) = rule;
                string creation;
                if (red is ReduceMethod or ReduceConstructor)
                {
                    StringBuilder reduceArgs = new();
                    foreach (var (idx, ele) in eles.WithIndex())
                    {
                        if (ele.AsArg is null) continue;
                        if (ele.AsArg is StringArgument sp)
                        {
                            var type = (ele.Raw.IsTerminal ? TerminalTypes : NonTerminalTypes)[ele.Raw.RawEnum];
                            if (type is not null)
                            {
                                reduceArgs.AppendLine(
                                    $"""
                                    {sp.ArgName}: GetValue<{new FullType(type)}>(x[{idx}]),
                                    """
                                );
                            }
                            else
                            {

                                reduceArgs.AppendLine(
                                    $"""
                                    // the given token does not have a type
                                    {sp.ArgName}: default,
                                    """
                                );
                            }
                        }
                        else if (ele.AsArg is ParserFuncsArgument pfa)
                        {

                        }
                    }
                    foreach (var opt in constParams)
                    {
                        if (opt.ArgumentName is ParserFuncsArgument pfa)
                        {

                            continue;
                        }
                        else if (opt.ArgumentName is StringArgument s)
                        {
                            reduceArgs.AppendLine(
                                $"""
                                {s.ArgName}: {ParserBaseHelper.ConstantParameterToString(opt)},
                                """
                            );
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    var len = EasyCSharp.GeneratorTools.Extension.InSourceNewLine.Length + 1 /* comma */;
                    if (reduceArgs.Length > len) // otherwise basically there are 0 arguments
                        reduceArgs.Remove(reduceArgs.Length - len, len);

                    string reduceMethodCall = red switch
                    {
                        ReduceMethod method => $"""
                        {method.Name}(
                            {reduceArgs.ToString().IndentWOF(1)}
                        )
                        """,
                        ReduceConstructor constructor => $"""
                        new {new FullType(constructor.Type_)}(
                            {reduceArgs.ToString().IndentWOF(1)}
                        )
                        """,
                        _ => throw new InvalidCastException()
                    };
                    creation =
                        nttype is null ?
                        $"""
                        {reduceMethodCall};
                        return CreateValue(
                            ({nonTerminalFT}){value},
                            reference: x
                        );
                        """ :
                        $"""
                        return CreateValue<{new FullType(nttype)}>(
                            ({nonTerminalFT}){value},
                            {reduceMethodCall.IndentWOF(1)},
                            reference: x
                        );
                        """;
                }
                else if (red is ReduceParserFunc reduceParserFunc)
                {
                    if (nttype is null)
                    {
                        creation = $"""
                            return CreateValue(
                                ({nonTerminalFT}){value},
                                reference: x
                            );
                            """;

                        break;
                    }
                    string? list = null;
                    string? val = null;
                    switch (reduceParserFunc.ParserFunc)
                    {
                        case ParserFuncs.Identity:
                        case ParserFuncs.SingleList:
                            foreach (var (idx, ele) in eles.WithIndex())
                            {
                                if (ele.AsArg is null) continue;
                                if (ele.AsArg is ParserFuncsArgument pfa)
                                {
                                    if (pfa.ArgName is not ParserFuncArgs.Value)
                                    {

                                        continue;
                                    }
                                    var type = (ele.Raw.IsTerminal ? TerminalTypes : NonTerminalTypes)[ele.Raw.RawEnum];
                                    if (type is not null)
                                    {
                                        if (val is null)
                                        {
                                            val = $"GetValue<{new FullType(type)}>(x[{idx}])";
                                        }
                                        else
                                        {

                                        }
                                    }
                                    else
                                    {

                                        val = $"/* the given token does not have a type */ default";
                                    }
                                }
                                else if (ele.AsArg is StringArgument s)
                                {

                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }
                            foreach (var opt in constParams)
                            {
                                if (opt.ArgumentName is ParserFuncsArgument pfa)
                                {
                                    if (pfa.ArgName is not ParserFuncArgs.Value)
                                    {

                                        continue;
                                    }
                                    if (val is null)
                                        val = ParserBaseHelper.ConstantParameterToString(opt);
                                    else
                                    {

                                    }
                                }
                                else if (opt.ArgumentName is StringArgument s)
                                {

                                }
                            }
                            if (val is null)
                            {

                                creation = "return default;";
                                break;
                            }
                            if (reduceParserFunc.ParserFunc is ParserFuncs.Identity)
                                creation = $"""
                                    return CreateValue<{new FullType(nttype)}>(
                                        ({nonTerminalFT}){value},
                                        ({val}),
                                        reference: x
                                    );
                                    """;
                            else if (reduceParserFunc.ParserFunc is ParserFuncs.SingleList)
                                creation = $"""
                                    return CreateValue<{new FullType(nttype)}>(
                                        ({nonTerminalFT}){value},
                                        [({val})],
                                        reference: x
                                    );
                                    """;
                            else
                                throw new NotImplementedException();
                            break;
                        case ParserFuncs.EmptyList:
                            foreach (var (idx, ele) in eles.WithIndex())
                            {
                                if (ele.AsArg is null) continue;
                                if (ele.AsArg is ParserFuncsArgument pfa)
                                {

                                }
                                else if (ele.AsArg is StringArgument s)
                                {

                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }
                            foreach (var opt in constParams)
                            {
                                if (opt.ArgumentName is ParserFuncsArgument pfa)
                                {
                                    if (pfa.ArgName is not ParserFuncArgs.Value)
                                    {

                                        continue;
                                    }
                                }
                                else if (opt.ArgumentName is StringArgument s)
                                {

                                }
                            }
                            creation = $"""
                                return CreateValue<{new FullType(nttype)}>(
                                    ({nonTerminalFT}){value},
                                    [],
                                    reference: x
                                );
                                """;
                            break;
                        case ParserFuncs.AppendList:
                            foreach (var (idx, ele) in eles.WithIndex())
                            {
                                if (ele.AsArg is null) continue;
                                if (ele.AsArg is ParserFuncsArgument pfa)
                                {
                                    if (pfa.ArgName is not (ParserFuncArgs.Value or ParserFuncArgs.List))
                                    {

                                        continue;
                                    }
                                    var type = (ele.Raw.IsTerminal ? TerminalTypes : NonTerminalTypes)[ele.Raw.RawEnum];
                                    if (type is not null)
                                    {
                                        if (pfa.ArgName is ParserFuncArgs.Value)
                                        {
                                            if (val is null)
                                            {
                                                val = $"GetValue<{new FullType(type)}>(x[{idx}])";
                                            }
                                            else
                                            {

                                            }
                                        }
                                        else if (pfa.ArgName is ParserFuncArgs.List)
                                        {
                                            if (list is null)
                                            {
                                                list = $"GetValue<{new FullType(type)}>(x[{idx}])";
                                            }
                                            else
                                            {

                                            }
                                        }
                                    }
                                    else
                                    {

                                        val = $"/* the given token does not have a type */ default";
                                    }
                                }
                                else if (ele.AsArg is StringArgument s)
                                {

                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }
                            foreach (var opt in constParams)
                            {
                                if (opt.ArgumentName is ParserFuncsArgument pfa)
                                {
                                    if (pfa.ArgName is not (ParserFuncArgs.Value or ParserFuncArgs.List))
                                    {

                                        continue;
                                    }
                                    if (pfa.ArgName is ParserFuncArgs.Value)
                                    {
                                        if (val is null)
                                            val = ParserBaseHelper.ConstantParameterToString(opt);
                                        else
                                        {

                                        }
                                    }
                                    else if (pfa.ArgName is ParserFuncArgs.List)
                                    {
                                        if (list is null)
                                            list = ParserBaseHelper.ConstantParameterToString(opt);
                                        else
                                        {

                                        }
                                    }
                                    else
                                    {
                                        throw new NotImplementedException();
                                    }
                                }
                                else if (opt.ArgumentName is StringArgument s)
                                {

                                }
                            }
                            if (val is null)
                            {

                                creation = "return default;";
                                break;
                            }
                            if (list is null)
                            {

                                creation = "return default;";
                                break;
                            }
                            const string listName = "listlocalvaRiABlEmAKeThEnamesocrAzYThaTNOconFlicT";
                            creation = $"""
                                var {listName} = {list};
                                {listName}.Add({val});
                                return CreateValue<{new FullType(nttype)}>(
                                    ({nonTerminalFT}){value},
                                    {listName},
                                    reference: x
                                );
                                """;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
                sb.AppendLine($$"""
                    CreateRule(
                        // NonTerminal.{{nt.Name}}
                        ({{nonTerminalFT}}){{value}},
                        [
                            {{string.Join("\n",
                                from ele in eles
                                select (ele.Raw.IsTerminal && ele.Raw.RawEnum == ErrorTerminal.Singleton)
                                    ? $"""
                                    // Error Terminal
                                    global::Get.Parser.ErrorTerminal.Singleton,
                                    """
                                    : $"""
                                    // {(ele.Raw.IsTerminal ? "Terminal" : "NonTerminal")}.???
                                    Syntax(({(ele.Raw.IsTerminal ? terminalFT : nonTerminalFT)}){ele.Raw.RawEnum}),
                                    """
                            ).IndentWOF(2)}}
                        ],
                        x => {
                            {{(
                                creation
                            ).IndentWOF(2)}}
                        },
                        Precedence: {{(ruleprec is null ? "null" : $"({terminalFT}){ruleprec}")}}
                    ),
                    """);
            }
        }
        StringBuilder precedenceSB = new();
        foreach (var precedence in precedenceList)
        {
            precedenceSB.AppendLine(
                $"""
                ([
                    {string.Join(
                        ",\n",
                        from term in precedence.RawEnumTerminals
                        select $"Syntax(({terminalFT}){term})").IndentWOF(1)}
                ], {FullType.Of<Associativity>()}.{precedence.Associativity}),
                """
            );
        }

        yield return $$"""
            protected override {{FullType.Of<ILRParserDFA>()}} GenerateDFA()
            {
                return new {{FullType.Of<LRParserDFAGen>()}}({{FullType.Of<EqualityComparer<INonTerminal>>()}}.Default, global::System.Collections.Generic.EqualityComparer<{{FullType.Of<ITerminal>(true)}}>.Default).CreateDFA(
                    [
                        {{sb.ToString().IndentWOF(3)}}
                    ],
                    Syntax(({{nonTerminalFT}}){{args.AttributeDatas[0].Wrapper.startNode}}),
                    [
                        // precedence
                        {{precedenceSB.ToString().IndentWOF(3)}}
                    ]
                );
            }
            """;
    }
    protected override ParserAttributeWarpper? TransformAttribute(AttributeData attributeData, Compilation compilation)
    {
        return AttributeDataToParserAttribute(attributeData, compilation);
    }
}
// just for sake of being able to use AddAttributeConverter
enum TempType : byte { }
