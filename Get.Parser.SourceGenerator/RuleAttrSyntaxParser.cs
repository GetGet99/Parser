using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Get.Parser.SourceGenerator;

class RuleAttrSyntaxParser : ParserBase<RuleAttrSyntaxParser.Terminal, RuleAttrSyntaxParser.NonTerminal, Rule>
{
    public enum Terminal
    {
        As, WithParam, WithPrecedence, String, Terminal, NonTerminal, Type, Unknown,
        ParserFuncArg, ParserFunc
    }
    public enum NonTerminal
    {
        Rule, Element, Raw, ConstArg, ReduceAction,
        ElementList, ConstArgsList, Constant,
        FuncArg
    }
    protected override ILRParserDFA GenerateDFA()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal>.Default);
        ICFGRule[] rules = [
            CreateRule(NonTerminal.Rule, [
                Syntax(NonTerminal.ElementList),
                Syntax(NonTerminal.ConstArgsList),
                Syntax(NonTerminal.ReduceAction)
            ], x => CreateValue(NonTerminal.Rule, new Rule(
                GetValue<List<Element>>(x[0]),
                GetValue<List<Option>>(x[1]),
                GetValue<ReduceAction>(x[2])
            ))),
            CreateRule(NonTerminal.Rule, [
                Syntax(NonTerminal.ElementList),
                Syntax(NonTerminal.ConstArgsList),
                Syntax(NonTerminal.ReduceAction),
                Syntax(Terminal.WithPrecedence),
                Syntax(Terminal.Terminal),
            ], x => CreateValue(NonTerminal.Rule, new Rule(
                GetValue<List<Element>>(x[0]),
                GetValue<List<Option>>(x[1]),
                GetValue<ReduceAction>(x[2]),
                GetValue<object>(x[4])
            ))),
            CreateRule(NonTerminal.Element, [
                Syntax(NonTerminal.Raw)
            ], x => CreateValue(NonTerminal.Element, new Element(
                GetValue<Raw>(x[0]),
                null
            ))),
            CreateRule(NonTerminal.Element, [
                Syntax(NonTerminal.Raw),
                Syntax(Terminal.As),
                Syntax(NonTerminal.FuncArg)
            ], x => CreateValue(NonTerminal.Element, new Element(
                GetValue<Raw>(x[0]), GetValue<Argument>(x[2])
            ))),
            CreateRule(NonTerminal.Raw, [
                Syntax(Terminal.Terminal)
            ], x => CreateValue(NonTerminal.Raw, new Raw(
                GetValue<object>(x[0]), IsTerminal: true
            ))),
            CreateRule(NonTerminal.Raw, [
                Syntax(Terminal.NonTerminal)
            ], x => CreateValue(NonTerminal.Raw, new Raw(
                GetValue<object>(x[0]), IsTerminal: false
            ))),
            CreateRule(NonTerminal.ConstArg, [
                Syntax(Terminal.WithParam),
                Syntax(NonTerminal.FuncArg),
                Syntax(NonTerminal.Constant)
            ], x => CreateValue(NonTerminal.ConstArg, new Option(
                GetValue<Argument>(x[1]), GetValue<object?>(x[2])
            ))),
            CreateRule(NonTerminal.FuncArg, [
                Syntax(Terminal.String)
            ], x => CreateValue<Argument>(NonTerminal.FuncArg, new StringArgument(GetValue<string>(x[0])))),
            CreateRule(NonTerminal.FuncArg, [
                Syntax(Terminal.ParserFuncArg)
            ], x => CreateValue<Argument>(NonTerminal.FuncArg, new ParserFuncsArgument(GetValue<ParserFuncArgs>(x[0])))),
            CreateRule(NonTerminal.ReduceAction, [
                Syntax(Terminal.String)
            ], x => CreateValue<ReduceAction>(NonTerminal.ReduceAction, new ReduceMethod(
                GetValue<string>(x[0])
            ))),
            CreateRule(NonTerminal.ReduceAction, [
                Syntax(Terminal.Type)
            ], x => CreateValue<ReduceAction>(NonTerminal.ReduceAction, new ReduceConstructor(
                GetValue<ITypeSymbol>(x[0])
            ))),
            CreateRule(NonTerminal.ReduceAction, [
                Syntax(Terminal.ParserFunc)
            ], x => CreateValue<ReduceAction>(NonTerminal.ReduceAction, new ReduceParserFunc(
                GetValue<ParserFuncs>(x[0])
            ))),
            CreateRule(NonTerminal.ElementList, [

            ], CreateEmptyListHandler<Element>(NonTerminal.ElementList)),
            CreateRule(NonTerminal.ElementList, [
                Syntax(NonTerminal.ElementList),
                Syntax(NonTerminal.Element)
            ], CreateAppendListHandler<Element>(NonTerminal.ElementList, listIdx: 0, eleIdx: 1)),
            CreateRule(NonTerminal.ConstArgsList, [

            ], CreateEmptyListHandler<Option>(NonTerminal.ConstArgsList)),
            CreateRule(NonTerminal.ConstArgsList, [
                Syntax(NonTerminal.ConstArgsList),
                Syntax(NonTerminal.ConstArg)
            ], CreateAppendListHandler<Option>(NonTerminal.ConstArgsList, listIdx: 0, eleIdx: 1)),
            // any kinds of terminal can be a constant
            ..
            from term in Enum.GetValues(typeof(Terminal)).Cast<Terminal>()
                select CreateRule(
                    NonTerminal.Constant, [
                        Syntax(term)
                    ], x => CreateValue(NonTerminal.Constant, GetValue<object?>(x[0]))
                ),
        ];
        var dfa = gen.CreateDFA(rules, Syntax(NonTerminal.Rule), []);
        return dfa;
    }
    public Rule Parse(ImmutableArray<TypedConstant> parameters, ITypeSymbol terminalType, ITypeSymbol nonTerminalType, ITypeSymbol keywordType)
    {
        IEnumerable<ITerminalValue> Iterate()
        {
            foreach (var parameter in parameters)
            {
                switch (parameter.Kind)
                {
                    case TypedConstantKind.Type:
                        yield return CreateValue(Terminal.Type, (ITypeSymbol)(parameter.Value ?? throw new NullReferenceException()));
                        continue;
                    case TypedConstantKind.Primitive:
                        switch (parameter.Value)
                        {
                            case string str:
                                yield return CreateValue(Terminal.String, str);
                                continue;
                            case var unknown:
                                yield return CreateValue(Terminal.Unknown, unknown);
                                continue;
                        }
                    case TypedConstantKind.Error:
                        yield return CreateValue<object?>(Terminal.Unknown, null);
                        continue;
                    case TypedConstantKind.Enum:
                        if (parameter.Type!.Equals(terminalType, SymbolEqualityComparer.Default))
                            yield return CreateValue(Terminal.Terminal, parameter.Value ?? throw new NullReferenceException());
                        else if (parameter.Type!.Equals(nonTerminalType, SymbolEqualityComparer.Default))
                            yield return CreateValue(Terminal.NonTerminal, parameter.Value ?? throw new NullReferenceException());
                        else if (parameter.Type!.Equals(keywordType, SymbolEqualityComparer.Default))
                            switch ((byte)(parameter.Value ?? throw new NullReferenceException())) {
                                case (byte)ParserSourceGeneratorKeywords.As:
                                    yield return CreateValue(Terminal.As);
                                    continue;
                                case (byte)ParserSourceGeneratorKeywords.WithParam:
                                    yield return CreateValue(Terminal.WithParam);
                                    continue;
                                case (byte)ParserSourceGeneratorKeywords.WithPrecedence:
                                    yield return CreateValue(Terminal.WithPrecedence);
                                    continue;
                                case (byte)ParserSourceGeneratorKeywords.Identity:
                                    yield return CreateValue(Terminal.ParserFunc, ParserFuncs.Identity);
                                    continue;
                                case (byte)ParserSourceGeneratorKeywords.EmptyList:
                                    yield return CreateValue(Terminal.ParserFunc, ParserFuncs.EmptyList);
                                    continue;
                                case (byte)ParserSourceGeneratorKeywords.SingleList:
                                    yield return CreateValue(Terminal.ParserFunc, ParserFuncs.SingleList);
                                    continue;
                                case (byte)ParserSourceGeneratorKeywords.AppendList:
                                    yield return CreateValue(Terminal.ParserFunc, ParserFuncs.AppendList);
                                    continue;
                                case (byte)ParserSourceGeneratorKeywords.List:
                                    yield return CreateValue(Terminal.ParserFuncArg, ParserFuncArgs.List);
                                    continue;
                                case (byte)ParserSourceGeneratorKeywords.Value:
                                    yield return CreateValue(Terminal.ParserFuncArg, ParserFuncArgs.Value);
                                    continue;
                                default:
                                    yield return CreateValue<object?>(Terminal.Unknown, parameter.Value);
                                    continue;
                            }
                        else
                            yield return CreateValue(Terminal.Unknown, parameter.Value);
                        continue;
                    case TypedConstantKind.Array:
                        yield return CreateValue<object?>(Terminal.Unknown, parameter.Values);
                        continue;
                }
            }
        }
        return Parse(Iterate());
    }
    static Func<ISyntaxElementValue[], INonTerminalValue> CreateEmptyListHandler<T>(NonTerminal nonTerminal)
    {
        return x => CreateValue<List<T>>(nonTerminal, []);
    }
    static Func<ISyntaxElementValue[], INonTerminalValue> CreateAppendListHandler<T>(NonTerminal nonTerminal, int listIdx, int eleIdx)
    {
        return x => {
            var list = GetValue<List<T>>(x[listIdx]);
            list.Add(GetValue<T>(x[eleIdx]));
            return CreateValue(nonTerminal, list);
        };
    }
}

public record Rule(List<Element> Elements, List<Option> Options, ReduceAction ReduceAction, object? PrecedenceTerminal = null);
public record Element(Raw Raw, Argument? AsArg)
{
    public override string ToString()
    {
        if (AsArg is null)
            return Raw.ToString();
        return $"{Raw} AS {AsArg}";
    }
}
public abstract record Argument;
public record StringArgument(string ArgName) : Argument;
public record ParserFuncsArgument(ParserFuncArgs ArgName) : Argument;
public record Raw(object RawEnum, bool IsTerminal)
{
    public override string ToString()
    {
        return $"{(IsTerminal ? "Terminal" : "NonTerminal")}.{RawEnum}";
    }
}
public record Option(Argument ArgumentName, object? ConstantParameterValue)
{
    public override string ToString()
    {
        return $"{ArgumentName}: {ConstantParameterValue}";
    }
}
public abstract record ReduceAction;
public record ReduceMethod(string Name) : ReduceAction
{
    public override string ToString()
    {
        return $"ReduceBy: method call {Name}(...)";
    }
}
public record ReduceConstructor(ITypeSymbol Type_) : ReduceAction
{
    public override string ToString()
    {
        return $"ReduceBy: constructor call {Type_}(...)";
    }
}
public record ReduceParserFunc(ParserFuncs ParserFunc) : ReduceAction
{
    public override string ToString()
    {
        return $"ReduceBy: parser func {ParserFunc}(...)";
    }
}
public enum ParserFuncs
{
    Identity,
    EmptyList,
    SingleList,
    AppendList
}
public enum ParserFuncArgs
{
    List,
    Value
}