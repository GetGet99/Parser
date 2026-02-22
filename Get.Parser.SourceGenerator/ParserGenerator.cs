using Get.EasyCSharp.GeneratorTools;
using Get.EasyCSharp.GeneratorTools.SyntaxCreator.Members;
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
    static RuleAttrSyntaxParser RuleAttrSyntaxParser { get; } = new RuleAttrSyntaxParser();
    static PrecedenceAttrSyntaxParser PrecedenceAttrSyntaxParser { get; } = new PrecedenceAttrSyntaxParser();
    class NonLocalizableString(string s) : LocalizableString
    {
        public string DiagonosticString { get; } = s;
        protected override bool AreEqual(object? other) => other is NonLocalizableString l && l.DiagonosticString == DiagonosticString;
        protected override int GetHash() => DiagonosticString.GetHashCode();
        protected override string GetText(IFormatProvider? formatProvider) => DiagonosticString;
    }
    protected override string? OnPointVisit(OnPointVisitArguments args)
    {
        if (!(args.Symbol.BaseType?.ToString().StartsWith("Get.Parser.ParserBase") ?? false))
        {
            return null;
        }
        if (args.Symbol.BaseType.TypeArguments.Length != 3)
        {
            return null;
        }
        return OnPointVisit2(args).JoinDoubleNewLine();
    }
    protected IEnumerable<string> OnPointVisit2(OnPointVisitArguments args)
    {
        var genContext = args.GenContext;
        var diagnostics = args.Diagnostics;
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
                    //var syn = raw.ApplicationSyntaxReference;
                    //args.Diagnostics.Add(Diagnostic.Create(
                    //    ParserRuleSyntaxErrorBase,
                    //    syn is null ? nt.Locations[0] : Location.Create(syn.SyntaxTree, syn.Span),
                    //    "Unexpected",
                    //    e.UnexpectedElement
                    //));
                    continue;
                }
                catch (LRParserRuntimeUnexpectedEndingException e)
                {
                    //var syn = raw.ApplicationSyntaxReference;
                    //args.Diagnostics.Add(Diagnostic.Create(
                    //    ParserRuleSyntaxErrorBase,
                    //    syn is null ? nt.Locations[0] : Location.Create(syn.SyntaxTree, syn.Span),
                    //    "Expected",
                    //    $"{string.Join(", ", (object?[])e.ExpectedInputs)} after the last parameter"
                    //));
                    continue;
                }
                static string ConstantParameterToString(Option option)
                {
                    var (type, value) = option.ConstantParameterValue;
                    // TODO
                    if (value is null)
                        return "null";

                    if (type is { TypeKind: TypeKind.Enum })
                    {
                        return $"({new FullType(type)}){value}";
                    }
                    else if (value is string str)
                    {
                        // escape it first
                        return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(str, true);
                    }
                    return option.ConstantParameterValue.ConstantParameterValue switch
                    {
                        bool b => b ? "true" : "false",
                        null => "null",
                        var rest => rest.ToString()
                    };
                }
                var nttype = NonTerminalTypes[value];
                var (eles, constParams, red, ruleprec) = rule;
                string creation;
                void Error(DiagnosticDescriptor desc, params object[] errorArgs)
                {
                    var syn = raw.ApplicationSyntaxReference;
                    args.Diagnostics.Add(Diagnostic.Create(
                        desc,
                        location: syn is null ? nt.Locations[0] : Location.Create(syn.SyntaxTree, syn.Span),
                        errorArgs
                    ));
                }
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
                                //Error(
                                //    ParserRuleNoTypeParam,
                                //    $"({(ele.Raw.IsTerminal ? "Terminal" : "NonTerminal")}){ele.Raw.RawEnum}",
                                //    ele.AsArg
                                //);
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
                            //Error(
                            //    ParserRuleNoTypeParam,
                            //    pfa.ArgName.ToString(),
                            //    red switch
                            //    {
                            //        ReduceMethod rd => rd.Name,
                            //        ReduceConstructor rd => $"(constructor of {rd.Type_.Name})",
                            //        _ => throw new InvalidCastException()
                            //    }
                            //);
                        }
                    }
                    foreach (var opt in constParams)
                    {
                        if (opt.ArgumentName is ParserFuncsArgument pfa)
                        {
                            //Error(
                            //    ParserRuleNoTypeParam,
                            //    pfa.ArgName.ToString(),
                            //    red switch
                            //    {
                            //        ReduceMethod rd => rd.Name,
                            //        ReduceConstructor rd => $"(constructor of {rd.Type_.Name})",
                            //        _ => throw new InvalidCastException()
                            //    }
                            //);
                            continue;
                        }
                        else if (opt.ArgumentName is StringArgument s)
                        {
                            reduceArgs.AppendLine(
                                $"""
                                {s.ArgName}: {ConstantParameterToString(opt)},
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
                            
                        //Error(
                        //    ParserRuleNoTypeReturn,
                        //    reduceParserFunc.ParserFunc.ToString()
                        //);
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
                                        //Error(
                                        //    ParserFuncInvalidArgs,
                                        //    pfa.ArgName.ToString(),
                                        //    "VALUE"
                                        //);
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
                                            //Error(
                                            //    ParserFuncDuplicateArgs,
                                            //    pfa.ArgName
                                            //);
                                        }
                                    }
                                    else
                                    {
                                        //Error(
                                        //    ParserRuleNoTypeParam,
                                        //    $"({(ele.Raw.IsTerminal ? "Terminal" : "NonTerminal")}){ele.Raw.RawEnum}",
                                        //    pfa.ArgName.ToString()
                                        //);
                                        val = $"/* the given token does not have a type */ default";
                                    }
                                }
                                else if (ele.AsArg is StringArgument s)
                                {
                                    //Error(
                                    //    ParserFuncInvalidArgs,
                                    //    s.ArgName,
                                    //    reduceParserFunc.ParserFunc.ToString()
                                    //);
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
                                        //Error(
                                        //    ParserFuncInvalidArgs,
                                        //    pfa.ArgName.ToString(),
                                        //    "VALUE"
                                        //);
                                        continue;
                                    }
                                    if (val is null)
                                        val = ConstantParameterToString(opt);
                                    else
                                    {
                                        //Error(
                                        //    ParserFuncDuplicateArgs,
                                        //    pfa.ArgName
                                        //);
                                    }
                                }
                                else if (opt.ArgumentName is StringArgument s)
                                {
                                    //Error(
                                    //    ParserFuncInvalidArgs,
                                    //    s.ArgName,
                                    //    "VALUE"
                                    //);
                                }
                            }
                            if (val is null)
                            {
                                //Error(
                                //    ParserFuncMissingArgs,
                                //    "VALUE"
                                //);
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
                                    //Error(
                                    //    ParserFuncInvalidArgs,
                                    //    pfa.ArgName.ToString(),
                                    //    "no argument"
                                    //);
                                }
                                else if (ele.AsArg is StringArgument s)
                                {
                                    //Error(
                                    //    ParserFuncInvalidArgs,
                                    //    s.ArgName,
                                    //    "no argument"
                                    //);
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
                                        //Error(
                                        //    ParserFuncInvalidArgs,
                                        //    pfa.ArgName.ToString(),
                                        //    "no argument"
                                        //);
                                        continue;
                                    }
                                }
                                else if (opt.ArgumentName is StringArgument s)
                                {
                                    //Error(
                                    //    ParserFuncInvalidArgs,
                                    //    s.ArgName,
                                    //    "no argument"
                                    //);
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
                                        //Error(
                                        //    ParserFuncInvalidArgs,
                                        //    pfa.ArgName.ToString(),
                                        //    "VALUE or LIST"
                                        //);
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
                                                //Error(
                                                //    ParserFuncDuplicateArgs,
                                                //    pfa.ArgName
                                                //);
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
                                                //Error(
                                                //    ParserFuncDuplicateArgs,
                                                //    pfa.ArgName
                                                //);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //Error(
                                        //    ParserRuleNoTypeParam,
                                        //    $"({(ele.Raw.IsTerminal ? "Terminal" : "NonTerminal")}){ele.Raw.RawEnum}",
                                        //    pfa.ArgName.ToString()
                                        //);
                                        val = $"/* the given token does not have a type */ default";
                                    }
                                }
                                else if (ele.AsArg is StringArgument s)
                                {
                                    //Error(
                                    //    ParserFuncInvalidArgs,
                                    //    s.ArgName,
                                    //    reduceParserFunc.ParserFunc.ToString()
                                    //);
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
                                        //Error(
                                        //    ParserFuncInvalidArgs,
                                        //    pfa.ArgName.ToString(),
                                        //    "VALUE or LIST"
                                        //);
                                        continue;
                                    }
                                    if (pfa.ArgName is ParserFuncArgs.Value)
                                    {
                                        if (val is null)
                                            val = ConstantParameterToString(opt);
                                        else
                                        {
                                            //Error(
                                            //    ParserFuncDuplicateArgs,
                                            //    pfa.ArgName.ToString()
                                            //);
                                        }
                                    }
                                    else if (pfa.ArgName is ParserFuncArgs.List)
                                    {
                                        if (list is null)
                                            list = ConstantParameterToString(opt);
                                        else
                                        {
                                            //Error(
                                            //    ParserFuncDuplicateArgs,
                                            //    pfa.ArgName.ToString()
                                            //);
                                        }
                                    }
                                    else
                                    {
                                        throw new NotImplementedException();
                                    }
                                }
                                else if (opt.ArgumentName is StringArgument s)
                                {
                                    //Error(
                                    //    ParserFuncInvalidArgs,
                                    //    s.ArgName,
                                    //    "VALUE or LIST"
                                    //);
                                }
                            }
                            if (val is null)
                            {
                                //Error(
                                //    ParserFuncMissingArgs,
                                //    "VALUE"
                                //);
                                creation = "return default;";
                                break;
                            }
                            if (list is null)
                            {
                                //Error(
                                //    ParserFuncMissingArgs,
                                //    "LIST"
                                //);
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
                return new {{FullType.Of<LRParserDFAGen>()}}({{FullType.Of<EqualityComparer<INonTerminal>>()}}.Default, {{FullType.Of<EqualityComparer<ITerminal>>()}}.Default).CreateDFA(
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
