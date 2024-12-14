
using Get.Lexer;
using Get.PLShared;
using Get.RegexMachine;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Get.Parser.Test;

static class MathTests
{
    static Terminal
        plus = "+",
        minus = "-",
        times = "*",
        divide = "/",
        lb = "(", rb = ")",
        absBar = "|",
        number = "NUMBER";

    static string NewLine =
        """
        a
        b
        """[1..^1];
    const string TestCases =
        """
        1 + 1 = 2  
        2 - 1 = 1  
        3 * 4 = 12  
        8 / 2 = 4  
        -3 = -3  
        +4 = 4  
        -(-5) = 5
        -3 + 1 = -2
        2 + 3 * 4 = 14  
        2 * 3 + 4 = 10  
        2 + 3 * 4 - 5 = 9  
        (2 + 3) * 4 = 20  
        2 * (3 + 4) = 14  
        (2 + 3) * (4 - 1) = 15  
        ((2 + 3) * 4) / 2 = 10  
        ((1 + 2) * (3 - 1)) / (4 / 2) = 3  
        ((2 * 3) + (4 / 2)) = 8  
        ((2 + 3) * ((4 - 2) + 1)) = 15  
        0 * 5 = 0
        ((2)) = 2  
        1 + (2 * (3 + (4 / 2))) = 11  
        1 + 2 - 3 + 4 * 5 / 2 = 10  
        3 * (4 + (5 - 2 * (6 / 3))) = 15
        22 / (2 + 3 * (7 - 4)) = 2  
        1 + (-2) * (3 + 4) - (-5) = -8
        """;
    static ILRParserDFA MathLRDFAGen()
    {
        Func<ISyntaxElementValue[], decimal> Binary(Func<decimal, decimal, decimal> f)
        {
            return x =>
            {
                return f(((ISyntaxElementValue<decimal>)x[0]).Value, ((ISyntaxElementValue<decimal>)x[2]).Value);
            };
        }
        Func<ISyntaxElementValue[], decimal> Unary(Func<decimal, decimal> f)
        {
            return x =>
            {
                return f(((ISyntaxElementValue<decimal>)x[1]).Value);
            };
        }
        Func<ISyntaxElementValue[], decimal> Get(int idx)
        {
            return x =>
            {
                return ((ISyntaxElementValue<decimal>)x[idx]).Value;
            };
        }
        var dfagen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, new TermComparer());
        var dfa = dfagen.CreateDFA([
            new Rule(NonTerminal.Expr, [number], Get(0)),
            new Rule(NonTerminal.Expr, [NonTerminal.Expr, plus, NonTerminal.Expr], Binary((x, y) => x + y)),
            new Rule(NonTerminal.Expr, [NonTerminal.Expr, minus, NonTerminal.Expr], Binary((x, y) => x - y)),
            new Rule(NonTerminal.Expr, [NonTerminal.Expr, times, NonTerminal.Expr], Binary((x, y) => x * y)),
            new Rule(NonTerminal.Expr, [NonTerminal.Expr, divide, NonTerminal.Expr], Binary((x, y) => x / y)),
            new Rule(NonTerminal.Expr, [lb, NonTerminal.Expr, rb], Get(1)),
            new Rule(NonTerminal.Expr, [absBar, NonTerminal.Expr, absBar], x => Math.Abs(Get(1)(x))),
            new Rule(NonTerminal.Expr, [minus, NonTerminal.Expr], Unary(x => -x)),
            new Rule(NonTerminal.Expr, [plus, NonTerminal.Expr], Unary(x => +x)), // no-op
        ], NonTerminal.Expr, [
            ([times, divide], Associativity.Left),
            ([plus, minus], Associativity.Left)
        ]);
        return dfa;
    }
    public static void TestMath()
    {
        var dfa = MathLRDFAGen();
        foreach (var testCase1 in TestCases.Split(NewLine))
        {
            if (testCase1.StartsWith("#")) continue;
            var testCase = testCase1.Trim();
            var a = testCase.Split(" = ");
            var expr = a[0];
            var ans = decimal.Parse(a[1]);
            var input = MathLexer.GetTerminals(expr);
            var output = LRParserRunner<decimal>.Parse(dfa, input);
            if (output != ans)
            {
                Debugger.Break();
            }
        }
    }
    [DoesNotReturn]
    public static void Interpreter()
    {
        var dfa = MathLRDFAGen();
        Console.WriteLine("Mathematics Interpreter");
        while (true)
        {
            Console.Write("> ");
            var expr = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(expr)) continue;
            try
            {
                var input = MathLexer.GetTerminals(expr);
                var output = LRParserRunner<decimal>.Parse(dfa, input);
                Console.WriteLine(output.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex.GetType().FullName}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
    class NonTerminal : INonTerminal
    {
        public static NonTerminal Expr { get; } = new("Expression");
        string _comment;
        private NonTerminal(string comment) { _comment = comment; }
        public override string ToString() => _comment;
    }
    class Rule(INonTerminal target, ISyntaxElement[] rule, Func<ISyntaxElementValue[], decimal> f) : ICFGRule
    {
        public INonTerminal Target => target;
        public IReadOnlyList<ISyntaxElement> Expressions => rule;

        public INonTerminalValue GetValue(ISyntaxElementValue[] value) => new NTV(target, f(value));
        class NTV(INonTerminal type, decimal val) : INonTerminalValue<decimal>
        {
            public decimal Value => val;
            public ISyntaxElement WithoutValue => type;
        }
        public override string ToString()
        {
            return $"{target} -> {string.Join(" ", from x in rule select x.ToString())}";
        }
    }
    class Terminal : ITerminal, ITerminalWithCustomPrecedence, ITerminalValue
    {
        public string Value { get; }

        public ITerminal PrecedenceTerminal { get; }

        public ITerminal WithoutValue => this;
        ISyntaxElement ISyntaxElementValue.WithoutValue => this;

        private Terminal(string term, Terminal? precedence = null)
        {
            Value = term;
            PrecedenceTerminal = precedence ?? this;
        }
        readonly static Dictionary<string, Terminal> maps = [];
        public static Terminal Get(string term)
        {
            if (maps.TryGetValue(term, out var result)) return result;
            result = new Terminal(term);
            maps.Add(term, result);
            return result;
        }
        public static Terminal Get(string term, Terminal precedence)
        {
            return new(term, precedence);
        }
        public static implicit operator Terminal(string term) => Get(term);
        public override string ToString() => Value;
    }
    class IntTerminal(decimal val) : ITerminal, ITerminalValue<decimal>
    {
        public decimal Value { get; } = val;

        public ITerminal WithoutValue => number;
        ISyntaxElement ISyntaxElementValue.WithoutValue => WithoutValue;
    }
    class TermComparer : IEqualityComparer<ITerminal>
    {
        public bool Equals(ITerminal? x, ITerminal? y)
        {
            if (x is ITerminalValue x2) x = x2.WithoutValue;
            if (y is ITerminalValue y2) y = y2.WithoutValue;
            if (x is Terminal x1 && y is Terminal y1)
                return x1.Value == y1.Value;
            //if (x is IntTerminal x2 && y is IntTerminal y2)
            //    return x2.Value == y2.Value;
            return false;
        }

        public int GetHashCode([DisallowNull] ITerminal obj)
        {
            if (obj is ITerminalValue obj2) obj = obj2.WithoutValue;
            if (obj is Terminal x1) return x1.Value.GetHashCode();
            return obj.GetHashCode();
        }
    }

    static class MathLexer
    {
        static StreamSeeker StreamOf(string text) => new(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        public static IEnumerable<ITerminalValue> GetTerminals(string expr)
        {
            var s = StreamOf(expr);
            var lexer = new MathTextLexer(s);
            foreach (var token in lexer.GetTokens())
            {
                if (token.TokenType is Terminals.Integer && token is IToken<Terminals, decimal> d)
                {
                    yield return new IntTerminal(d.Data);
                }
                if (token.TokenType is Terminals.Plus) yield return plus;
                if (token.TokenType is Terminals.Minus) yield return minus;
                if (token.TokenType is Terminals.Times) yield return times;
                if (token.TokenType is Terminals.Divide) yield return divide;
                if (token.TokenType is Terminals.OpenBracket) yield return lb;
                if (token.TokenType is Terminals.CloseBracket) yield return rb;
                if (token.TokenType is Terminals.AbsoluteValueBar) yield return absBar;
            }
        }
        class MathTextLexer(ITextSeekable text) : LexerBase<State, Terminals>(text, State.Initial)
        {
            public override Dictionary<State, RegexCompiler<Func<IToken<Terminals>?>>.DFAState> DFASourceGenOutput()
            {
                Dictionary<State, RegexCompiler<Func<IToken<Terminals>?>>.DFAState> dict = [];
                dict[State.Initial] = RegexCompiler<Func<IToken<Terminals>?>>.GenerateDFA([
                    new(@"[0-9]+", MakeFunc(Terminals.Integer, () => decimal.Parse(MatchedText))),
                    new(@"\+", MakeFunc(Terminals.Plus)),
                    new(@"-", MakeFunc(Terminals.Minus)),
                    new(@"\*", MakeFunc(Terminals.Times)),
                    new(@"/", MakeFunc(Terminals.Divide)),
                    new(@"\(", MakeFunc(Terminals.OpenBracket)),
                    new(@"\)", MakeFunc(Terminals.CloseBracket)),
                    new(@"\|", MakeFunc(Terminals.AbsoluteValueBar)),
                    new(@"[\t ]+", Empty()),
                ], RegexConflictBehavior.Throw);
                return dict;
            }
        }
        enum State { Initial }
        enum Terminals
        {
            Integer,
            Plus,
            Minus,
            Divide,
            OpenBracket,
            CloseBracket,
            AbsoluteValueBar,
            Times
        }

    }


}