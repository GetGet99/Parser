using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Get.Parser.SourceGenerator;

class PrecedenceAttrSyntaxParser : ParserBase<PrecedenceAttrSyntaxParser.Terminal, PrecedenceAttrSyntaxParser.NonTerminal, List<PrecedenceItem>>
{
    public enum Terminal
    {
        Terminal, Associativity, Unknown
    }
    public enum NonTerminal
    {
        PrecedenceList, TerminalList, Precedence
    }
    protected override ILRParserDFA GenerateDFA()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal>.Default);
        ICFGRule[] rules = [
            CreateRule(NonTerminal.Precedence, [
                Syntax(NonTerminal.TerminalList),
                Syntax(Terminal.Associativity)
            ], x => {
                return CreateValue(
                    NonTerminal.Precedence,
                    new PrecedenceItem(GetValue<List<object>>(x[0]), GetValue<Associativity>(x[1]))
                );
            }),
            CreateRule(NonTerminal.TerminalList, [
                Syntax(Terminal.Terminal)
            ], CreateSingleItemListHandler<object>(NonTerminal.TerminalList, eleIdx: 0)),
            CreateRule(NonTerminal.TerminalList, [
                Syntax(NonTerminal.TerminalList),
                Syntax(Terminal.Terminal)
            ], CreateAppendListHandler<object>(NonTerminal.TerminalList, listIdx: 0, eleIdx: 1)),
            CreateRule(NonTerminal.PrecedenceList, [
                Syntax(NonTerminal.Precedence)
            ], CreateSingleItemListHandler<PrecedenceItem>(NonTerminal.PrecedenceList, eleIdx: 0)),
            CreateRule(NonTerminal.PrecedenceList, [
                Syntax(NonTerminal.PrecedenceList),
                Syntax(NonTerminal.Precedence)
            ], CreateAppendListHandler<PrecedenceItem>(NonTerminal.PrecedenceList, listIdx: 0, eleIdx: 1)),
        ];
        var dfa = gen.CreateDFA(rules, Syntax(NonTerminal.PrecedenceList), []);
        return dfa;
    }
    public List<PrecedenceItem> Parse(ImmutableArray<TypedConstant> parameters, ITypeSymbol terminalType, ITypeSymbol associativityType)
    {
        IEnumerable<ITerminalValue> Iterate()
        {
            foreach (var parameter in parameters)
            {
                switch (parameter.Kind)
                {
                    case TypedConstantKind.Type:
                        yield return CreateValue(Terminal.Unknown, (ITypeSymbol)(parameter.Value ?? throw new NullReferenceException()));
                        continue;
                    case TypedConstantKind.Primitive:
                        switch (parameter.Value)
                        {
                            case string str:
                                yield return CreateValue(Terminal.Unknown, str);
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
                        else if (parameter.Type!.Equals(associativityType, SymbolEqualityComparer.Default))
                            yield return CreateValue(Terminal.Associativity, (Associativity)(parameter.Value ?? throw new NullReferenceException()));
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
    static Func<ISyntaxElementValue[], INonTerminalValue> CreateSingleItemListHandler<T>(NonTerminal nonTerminal, int eleIdx)
    {
        return x => {
            List<T> list = [];
            list.Add(GetValue<T>(x[eleIdx]));
            return CreateValue(nonTerminal, list);
        };
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

public record PrecedenceItem(List<object> RawEnumTerminals, Associativity Associativity);