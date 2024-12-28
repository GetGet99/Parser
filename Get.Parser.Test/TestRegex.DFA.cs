
using System.Collections;

namespace Get.Parser.Test
{
    static partial class TestRegex
    {
        static ILRParserDFA GetDFA()
        {
            var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal>.Default);
            ICFGRule[] rules = [
                new CFGRule(NonTerminal.FinalRegex, [c(NonTerminal.Expr)],
                x => new FinalRegex((RegexExpr)x[0])),
                // empty string
                new CFGRule(NonTerminal.FinalRegex, [],
                    x => new FinalRegex(new CatExpr([]))), // let's reuse CatExpr and say we cat a list of "nothing"
                
                new CFGRule(NonTerminal.Expr, [c(NonTerminal.Primary)],
                    x => x[0].As<Primary>().Expression),

                // ()
                new CFGRule(NonTerminal.Primary, [c(Terminal.OpenBracket), c(Terminal.CloseBracket)],
                x => new Primary(new CatExpr([]))),
                // (expr)
                new CFGRule(NonTerminal.Primary, [c(Terminal.OpenBracket), c(NonTerminal.Expr), c(Terminal.CloseBracket)],
                x => new Primary(x[1].As<RegexExpr>())),
                // a|b
                new CFGRule(NonTerminal.Expr, [c(NonTerminal.Expr), c(Terminal.Alternation), c(NonTerminal.Expr)],
                x => AlternationExpr.CreateOrCombine((RegexExpr)x[0], (RegexExpr)x[2])),
                // a|<empty>
                new CFGRule(NonTerminal.Expr, [c(NonTerminal.Expr), c(Terminal.Alternation)],
                x => AlternationExpr.CreateOrCombine((RegexExpr)x[0], new CatExpr([]))),

                // handle left <empty>|
                // <empty>|<empty> for the entire rule
                new CFGRule(NonTerminal.FinalRegex, [c(Terminal.Alternation)],
                x => new FinalRegex(new AlternationExpr([new CatExpr([]), new CatExpr([])]))),
                // <empty>|something for the entire rule
                new CFGRule(NonTerminal.FinalRegex, [c(Terminal.Alternation), c(NonTerminal.Expr)],
                x => new FinalRegex(new AlternationExpr([new CatExpr([]), (RegexExpr)x[1]]))),
                // (<empty>|<empty>)
                new CFGRule(NonTerminal.Primary, [c(Terminal.OpenBracket), c(Terminal.Alternation), c(Terminal.CloseBracket)],
                x => new Primary(new AlternationExpr([new CatExpr([]), new CatExpr([])]))),
                // (<empty>|something)
                new CFGRule(NonTerminal.Primary, [c(Terminal.OpenBracket), c(Terminal.Alternation), c(NonTerminal.Expr), c(Terminal.CloseBracket)],
                x => new Primary(new AlternationExpr([new CatExpr([]), (RegexExpr)x[2]]))),

                // ab
                new CFGRule(NonTerminal.Expr, [c(NonTerminal.Expr), c(NonTerminal.Expr)],
                x => CatExpr.CreateOrCombine((RegexExpr)x[0], (RegexExpr)x[1]),
                PrecedenceTerminal: c(Terminal.Concatenation)),
                
                // expr*
                new CFGRule(NonTerminal.Expr, [c(NonTerminal.Primary), c(Terminal.Star)],
                x => new StarExpr(x[0].As<Primary>().Expression)),
                // expr+
                new CFGRule(NonTerminal.Expr, [c(NonTerminal.Primary), c(Terminal.Plus)],
                x => new PlusExpr(x[0].As<Primary>().Expression)),
                
                // char
                new CFGRule(NonTerminal.Primary, [c(NonTerminal.NonClassCharacter)],
                x => new Primary(new CharExpr(x[0].As<NonClassCharacter>().Char))),
                // char (allow non escape outside [])
                new CFGRule(NonTerminal.NonClassCharacter, [c(NonTerminal.Character)],
                x => new NonClassCharacter(((Character)x[0]).Char)),
                // terminals that do not require escaping outside []
                ..
                from term in (IEnumerable<Terminal>)[Terminal.Caret, Terminal.Dash]
                select
                    new CFGRule(NonTerminal.NonClassCharacter, [c(term)],
                    x => new NonClassCharacter(((ITerminalValue<char>)x[0]).Value)),

                new CFGRule(NonTerminal.Primary,
                [c(Terminal.OpenSquareBracket), c(NonTerminal.Classes), c(Terminal.CloseSquareBracket)],
                x => new Primary(new ClassExpr(x[1].As<ClassList>().Classes.SelectMany(x => x.Chars).Distinct().ToArray(), IsInverse: false))),
                new CFGRule(NonTerminal.Primary,
                [c(Terminal.OpenSquareBracket), c(Terminal.Caret), c(NonTerminal.Classes), c(Terminal.CloseSquareBracket)],
                x => new Primary(new ClassExpr(x[2].As<ClassList>().Classes.SelectMany(x => x.Chars).Distinct().ToArray(), IsInverse: true))),

                new CFGRule(NonTerminal.Classes, [/*empty*/],
                x => new ClassList([])),
                new CFGRule(NonTerminal.Classes, [c(NonTerminal.Classes), c(NonTerminal.Class)],
                x => {
                    var l = (ClassList)x[0];
                    l.Add((Class)x[1]);
                    return l;
                }),
                new CFGRule(NonTerminal.Class, [c(NonTerminal.Character)],
                x => new Class([x[0].As<Character>().Char])),
                new CFGRule(NonTerminal.Class, [c(NonTerminal.Character), c(Terminal.Dash), c(NonTerminal.Character)],
                x => new Class(CharRange(x[0].As<Character>().Char, x[2].As<Character>().Char))),

                new CFGRule(
                    NonTerminal.Character, [c(Terminal.Other)],
                    x => new Character(((TerminalValue)x[0]).RawChar)
                ),
                // terminals that do not require escaping
                ..
                from term in (IEnumerable<Terminal>)[Terminal.SingleQuote, Terminal.DoubleQuote, Terminal.NormalCharOrSpecialEscape]
                select
                    new CFGRule(NonTerminal.Character, [c(term)],
                    x => new Character(((ITerminalValue<char>)x[0]).Value)),
                new CFGRule(
                    NonTerminal.Character, [c(Terminal.Backslash), c(Terminal.NormalCharOrSpecialEscape)],
                    x => new Character(((ITerminalValue<char>)x[1]).Value switch // Correct x[1]
                    {
                        'r' => '\r',
                        'n' => '\n',
                        't' => '\t',
                        _ => throw new InvalidDataException("the given token should not be NormalCharOrSpecialEscape")
                    })
                ),
                // adding escape characters for all
                ..
                from term in Enum.GetValues<Terminal>()
                where term is not (Terminal.Other or Terminal.NormalCharOrSpecialEscape or Terminal.Concatenation)
                   select new CFGRule(
                        NonTerminal.Character, [c(Terminal.Backslash), c(term)],
                        x => new Character(x[1].As<TerminalValue>().RawChar)
                    ),
            ];
            var dfa = gen.CreateDFA(rules, c(NonTerminal.FinalRegex), precedenceList: [
                ([c(Terminal.Alternation)], Associativity.Left),
                ([c(Terminal.Concatenation)], Associativity.Left),
            ]);
            return dfa;
        }
        static char[] CharRange(char c1, char c2)
        {
            char[] toRet = new char[c2 - c1 + 1];
            for (char c = c1; c <= c2; c++)
                toRet[c - c1] = c;
            return toRet;
        }
        enum Terminal
        {
            OpenBracket, CloseBracket, OpenSquareBracket, CloseSquareBracket,
            Alternation, Star, Plus, Caret, Backslash,
            SingleQuote, DoubleQuote, Dot, Dash, NormalCharOrSpecialEscape, Other,
            // not a real terminal, used for precedence
            Concatenation
        }
        enum NonTerminal
        {
            FinalRegex,
            Expr,
            Primary,
            /// <summary>
            /// Character outside the [] notation
            /// </summary>
            NonClassCharacter,
            Character,
            Class,
            Classes
        }
        static NonTerminalWrapper c(NonTerminal type) => type;
        static TerminalWrapper c(Terminal type) => type;
        record FinalRegex(RegexExpr Expression) : NonTerminalValue(NonTerminal.FinalRegex), INonTerminalValue<FinalRegex>
        {
            FinalRegex ISyntaxElementValue<FinalRegex>.Value => this;
        }
        abstract record RegexExpr() : NonTerminalValue(NonTerminal.Expr);
        record Primary(RegexExpr Expression) : NonTerminalValue(NonTerminal.Primary)
        {
            public override string ToString()
            {
                return $"Primary{{{Expression}}}";
            }
        }
        record StarExpr(RegexExpr Expression) : RegexExpr
        {
            public override string ToString()
            {
                return $"Star({Expression})";
            }
        }
        record PlusExpr(RegexExpr Expression) : RegexExpr
        {
            public override string ToString()
            {
                return $"Plus({Expression})";
            }
        }
        record CatExpr(RegexExpr[] Expressions) : RegexExpr
        {
            public static CatExpr CreateOrCombine(RegexExpr expr1, RegexExpr expr2)
            {
                if (expr1 is CatExpr catExpr1)
                    return new CatExpr([.. catExpr1.Expressions, expr2]);
                if (expr2 is CatExpr catExpr2)
                    return new CatExpr([expr1, .. catExpr2.Expressions]);
                return new CatExpr([expr1, expr2]);
            }
            public override string ToString()
            {
                return $"Cat({string.Join(' ', (object?[])Expressions)})";
            }
        }
        record AlternationExpr(RegexExpr[] Expressions) : RegexExpr
        {
            public static AlternationExpr CreateOrCombine(RegexExpr expr1, RegexExpr expr2)
            {
                if (expr1 is AlternationExpr catExpr)
                    return new AlternationExpr([.. catExpr.Expressions, expr2]);
                return new AlternationExpr([expr1, expr2]);
            }
            public override string ToString()
            {
                return $"Alt({string.Join('|', (object?[])Expressions)})";
            }
        }
        record ClassExpr(char[] Chars, bool IsInverse) : RegexExpr
        {
            public override string ToString()
            {
                return $"Class[{(IsInverse ? "^" : "")}{string.Join(null, Chars)}]";
            }
        }
        record ClassList(List<Class> Classes) : NonTerminalValue(NonTerminal.Classes), IList<Class>
        {
            public Class this[int index] { get => ((IList<Class>)Classes)[index]; set => ((IList<Class>)Classes)[index] = value; }

            public int Count => ((ICollection<Class>)Classes).Count;

            public bool IsReadOnly => ((ICollection<Class>)Classes).IsReadOnly;

            public void Add(Class item)
            {
                ((ICollection<Class>)Classes).Add(item);
            }

            public void Clear()
            {
                ((ICollection<Class>)Classes).Clear();
            }

            public bool Contains(Class item)
            {
                return ((ICollection<Class>)Classes).Contains(item);
            }

            public void CopyTo(Class[] array, int arrayIndex)
            {
                ((ICollection<Class>)Classes).CopyTo(array, arrayIndex);
            }

            public IEnumerator<Class> GetEnumerator()
            {
                return ((IEnumerable<Class>)Classes).GetEnumerator();
            }

            public int IndexOf(Class item)
            {
                return ((IList<Class>)Classes).IndexOf(item);
            }

            public void Insert(int index, Class item)
            {
                ((IList<Class>)Classes).Insert(index, item);
            }

            public bool Remove(Class item)
            {
                return ((ICollection<Class>)Classes).Remove(item);
            }

            public void RemoveAt(int index)
            {
                ((IList<Class>)Classes).RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)Classes).GetEnumerator();
            }
        }
        record Class(char[] Chars) : NonTerminalValue(NonTerminal.Class);
        record CharExpr(char Char) : RegexExpr
        {
            public override string ToString()
            {
                return $"CharExpr({Char})";
            }
        }
        record Character(char Char) : NonTerminalValue(NonTerminal.Character);
        record NonClassCharacter(char Char) : NonTerminalValue(NonTerminal.NonClassCharacter);
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
        record struct TerminalWrapper(Terminal Type) : ITerminal
        {
            public static implicit operator TerminalWrapper(Terminal t) => new(t);
            public override string ToString()
            {
                return $"T[{Type}]";
            }
        }
        record class CFGRule(NonTerminalWrapper Target, IReadOnlyList<ISyntaxElement> Expressions, Func<ISyntaxElementValue[], NonTerminalValue> Reduce, ITerminal? PrecedenceTerminal = null) : ICFGRule, ICFGRuleWithPrecedence
        {

            INonTerminal ICFGRule.Target => Target;
            public INonTerminalValue GetValue(ISyntaxElementValue[] value)
                => Reduce(value);
            public override string ToString()
            {
                return $"{Target.Type} -> {(Expressions.Count > 0 ? string.Join(' ', Expressions) : "<empty>")}";
            }
        }
        static T As<T>(this ISyntaxElementValue ele)
        {
            return (T)ele;
        }
    }
}
