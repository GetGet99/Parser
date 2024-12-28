using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Get.Parser.SourceGenerator;

class RuleAttrSyntaxParser : ParserBase<RuleAttrSyntaxParser.Terminal, RuleAttrSyntaxParser.NonTerminal, Rule>
{
    public enum Terminal
    {
        As, WithParam, String, Terminal, NonTerminal, Type, Unknown
    }
    public enum NonTerminal
    {
        Rule, Element, Raw, Option, ReduceAction,
        ElementList, OptionList, Constant
    }
    protected override ILRParserDFA GenerateDFA()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal>.Default);
        ICFGRule[] rules = [
            CreateRule(NonTerminal.Rule, [
                Syntax(NonTerminal.ElementList),
                Syntax(NonTerminal.OptionList),
                Syntax(NonTerminal.ReduceAction)
            ], x => CreateValue(NonTerminal.Rule, new Rule(
                GetValue<List<Element>>(x[0]),
                GetValue<List<Option>>(x[1]),
                GetValue<ReduceAction>(x[2])
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
                Syntax(Terminal.String)
            ], x => CreateValue(NonTerminal.Element, new Element(
                GetValue<Raw>(x[0]), GetValue<string>(x[2])
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
            CreateRule(NonTerminal.Option, [
                Syntax(Terminal.WithParam),
                Syntax(Terminal.String),
                Syntax(NonTerminal.Constant)
            ], x => CreateValue(NonTerminal.Option, new Option(
                GetValue<string>(x[1]), GetValue<object?>(x[2])
            ))),
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
            CreateRule(NonTerminal.ElementList, [

            ], CreateEmptyListHandler<Element>(NonTerminal.ElementList)),
            CreateRule(NonTerminal.ElementList, [
                Syntax(NonTerminal.ElementList),
                Syntax(NonTerminal.Element)
            ], CreateAppendListHandler<Element>(NonTerminal.ElementList, listIdx: 0, eleIdx: 1)),
            CreateRule(NonTerminal.OptionList, [

            ], CreateEmptyListHandler<Option>(NonTerminal.OptionList)),
            CreateRule(NonTerminal.OptionList, [
                Syntax(NonTerminal.OptionList),
                Syntax(NonTerminal.Option)
            ], CreateAppendListHandler<Option>(NonTerminal.OptionList, listIdx: 0, eleIdx: 1)),
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
                                case (byte)Keywords.As:
                                    yield return CreateValue(Terminal.As);
                                    continue;
                                case (byte)Keywords.WithParam:
                                    yield return CreateValue(Terminal.WithParam);
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

public record Rule(List<Element> Elements, List<Option> Options, ReduceAction ReduceAction);
public record Element(Raw Raw, string? AsParameter)
{
    public override string ToString()
    {
        if (AsParameter is null)
            return Raw.ToString();
        return $"{Raw} AS {AsParameter}";
    }
}
public record Raw(object RawEnum, bool IsTerminal)
{
    public override string ToString()
    {
        return $"{(IsTerminal ? "Terminal" : "NonTerminal")}.{RawEnum}";
    }
}
public record Option(string ParameterName, object? ConstantParameterValue)
{
    public override string ToString()
    {
        return $"{ParameterName}: {ConstantParameterValue}";
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