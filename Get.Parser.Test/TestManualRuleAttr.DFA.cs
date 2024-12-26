using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Get.Parser.Test;

static partial class TestManualRuleAttr
{
    static ILRParserDFA GetDFA()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal>.Default);
        ICFGRule[] rules = [
            new CFGRule(NonTerminal.Rule, [c(NonTerminal.ElementList), c(NonTerminal.OptionList), c(NonTerminal.ReduceAction)], x => {
                return new Rule(
                    (ElementList)x[0],
                    (OptionList)x[1],
                    (ReduceAction)x[2]
                );
            }),
            new CFGRule(NonTerminal.Element, [c(NonTerminal.Raw)],
            x => new Element((Raw)x[0], null)),
            new CFGRule(NonTerminal.Element, [c(NonTerminal.Raw), c(Terminal.As), c(Terminal.String)],
            x => new Element((Raw)x[0], (string)((TerminalValue)x[2]).RawObject)),
            new CFGRule(NonTerminal.Raw, [c(Terminal.Terminal)],
            x => new Raw(((TerminalValue)x[0]).RawObject, IsTerminal: true)),
            new CFGRule(NonTerminal.Raw, [c(Terminal.NonTerminal)],
            x => new Raw(((TerminalValue)x[0]).RawObject, IsTerminal: false)),
            new CFGRule(NonTerminal.Option, [c(Terminal.WithParam), c(Terminal.String), c(NonTerminal.Constant)],
            x => new Option((string)((TerminalValue)x[1]).RawObject, ((Constant)x[2]).RawObject)),
            new CFGRule(NonTerminal.ReduceAction, [c(Terminal.String)],
            x => new ReduceMethod((string)((TerminalValue)x[0]).RawObject)),
            new CFGRule(NonTerminal.ReduceAction, [c(Terminal.Type)],
            x => new ReduceConstructor((Type)((TerminalValue)x[0]).RawObject)),
            new CFGRule(NonTerminal.ElementList, [], x => new ElementList([])),
            new CFGRule(NonTerminal.ElementList, [c(NonTerminal.ElementList), c(NonTerminal.Element)],
            x => {
                var list = (ElementList)x[0];
                list.Elements.Add((Element)x[1]);
                return list;
            }),
            new CFGRule(NonTerminal.OptionList, [], x => new OptionList([])),
            new CFGRule(NonTerminal.OptionList, [c(NonTerminal.OptionList), c(NonTerminal.Option)],
            x => {
                var list = (OptionList)x[0];
                list.Options.Add((Option)x[1]);
                return list;
            }),
            // any kinds of terminal can be a constant
            ..
            from term in Enum.GetValues<Terminal>()
               select new CFGRule(
                    NonTerminal.Constant, [c(term)],
                    x => new Constant(((TerminalValue)x[0]).RawObject)
                ),
        ];
        var dfa = gen.CreateDFA(rules, c(NonTerminal.Rule), []);
        return dfa;
    }
    public enum Keywords
    {
        AS,
        WITHPARAM
    }
    enum Terminal
    {
        As, WithParam, String, Terminal, NonTerminal, Type, Unknown
    }
    enum NonTerminal
    {
        Rule, Element, Raw, Option, ReduceAction,
        ElementList, OptionList, Constant
    }
    static NonTerminalWrapper c(NonTerminal type) => type;
    static TerminalWrapper c(Terminal type) => type;
    record Rule(ElementList Elements, OptionList Options, ReduceAction ReduceAction)
        : NonTerminalValue(NonTerminal.Rule), INonTerminalValue<Rule>
    {
        Rule ISyntaxElementValue<Rule>.Value => this;
    }
    record ElementList(List<Element> Elements) : NonTerminalList<Element>(Elements, NonTerminal.ElementList)
    {
        public override string ToString()
        {
            return $"Elements[{string.Join(", ", Elements)}]";
        }
    }
    record OptionList(List<Option> Options) : NonTerminalList<Option>(Options, NonTerminal.OptionList)
    {
        public override string ToString()
        {
            return $"Options[{string.Join(", ", Options)}]";
        }
    }
    record Element(Raw Raw, string? AsParameter) : NonTerminalValue(NonTerminal.Element)
    {
        public override string ToString()
        {
            if (AsParameter is null)
                return Raw.ToString();
            return $"{Raw} AS {AsParameter}";
        }
    }
    record Raw(object RawEnum, bool IsTerminal) : NonTerminalValue(NonTerminal.Raw)
    {
        public override string ToString()
        {
            return $"{(IsTerminal ? "Terminal" : "NonTerminal")}.{RawEnum}";
        }
    }
    record Option(string ParameterName, object ConstantParameterValue) : NonTerminalValue(NonTerminal.Option)
    {
        public override string ToString()
        {
            return $"{ParameterName}: {ConstantParameterValue}";
        }
    }
    abstract record ReduceAction() : NonTerminalValue(NonTerminal.ReduceAction);
    record ReduceMethod(string Name) : ReduceAction
    {
        public override string ToString()
        {
            return $"ReduceBy: method call {Name}(...)";
        }
    }
    record ReduceConstructor(Type Type_) : ReduceAction
    {
        public override string ToString()
        {
            return $"ReduceBy: constructor call {Type_.FullName}(...)";
        }
    }
    record Constant(object RawObject) : NonTerminalValue(NonTerminal.Constant);

    record NonTerminalList<T>(List<T> Values, NonTerminal Type) : NonTerminalValue(Type), IList<T>, IReadOnlyList<T>
    {
        public T this[int index] { get => ((IList<T>)Values)[index]; set => ((IList<T>)Values)[index] = value; }

        public int Count => ((ICollection<T>)Values).Count;

        public bool IsReadOnly => ((ICollection<T>)Values).IsReadOnly;

        public void Add(T item)
        {
            ((ICollection<T>)Values).Add(item);
        }

        public void Clear()
        {
            ((ICollection<T>)Values).Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)Values).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)Values).CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)Values).GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return ((IList<T>)Values).IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ((IList<T>)Values).Insert(index, item);
        }

        public bool Remove(T item)
        {
            return ((ICollection<T>)Values).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<T>)Values).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Values).GetEnumerator();
        }
    }
    record struct NonTerminalWrapper(NonTerminal Type) : INonTerminal
    {
        public static implicit operator NonTerminalWrapper(NonTerminal t) => new(t);
        public override string ToString()
        {
            return $"NT[{Type}]";
        }
    }
    record NonTerminalValue(NonTerminal Type) : INonTerminalValue
    {
        ISyntaxElement ISyntaxElementValue.WithoutValue => new NonTerminalWrapper(Type);
    }
    record TerminalValue(object RawObject, Terminal Type) : ITerminalValue
    {
        ISyntaxElement ISyntaxElementValue.WithoutValue => new TerminalWrapper(Type);
        ITerminal ITerminalValue.WithoutValue => new TerminalWrapper(Type);
        public override string ToString()
        {
            return $"{RawObject} ({Type})";
        }
    }
    record struct TerminalWrapper(Terminal Type) : ITerminal
    {
        public static implicit operator TerminalWrapper(Terminal t) => new(t);
        public override string ToString()
        {
            return $"T[{Type}]";
        }
    }
    record class CFGRule(NonTerminalWrapper Target, IReadOnlyList<ISyntaxElement> Expressions, Func<ISyntaxElementValue[], NonTerminalValue> Reduce) : ICFGRule
    {
        INonTerminal ICFGRule.Target => Target;
        public INonTerminalValue GetValue(ISyntaxElementValue[] value)
            => Reduce(value);
        public override string ToString()
        {
            return $"{Target.Type} -> {(Expressions.Count > 0 ? string.Join(' ', Expressions) : "<empty>")}";
        }
    }
}
