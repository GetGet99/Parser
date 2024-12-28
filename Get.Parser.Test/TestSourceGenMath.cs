using Get.Lexer;
namespace Get.Parser.Test;
using static TestSourceGenMath.Terminal;
using static TestSourceGenMath.NonTerminal;
using System.Diagnostics;
using Get.PLShared;
using Get.RegexMachine;
using System.Text;

[Parser(StartNode)]
[Precedence(
    Times, Divide, Associativity.Left,
    Plus, Minus, Associativity.Left
)]
partial class TestSourceGenMath : ParserBase<TestSourceGenMath.Terminal, TestSourceGenMath.NonTerminal, decimal>
{
    const Keywords AS = Keywords.As;
    const Keywords WITHPARAM = Keywords.WithParam;
    public enum Terminal
    {
        Plus,
        Minus,
        Times,
        Divide,
        [Type<decimal>]
        Number,
        OpenBracket,
        CloseBracket,
        AbsoluteValueBar
    }
    public enum NonTerminal
    {
        [Type<decimal>]
        [Rule(Expr, AS, "val", nameof(Identity))]
        StartNode,
        [Type<decimal>]
        [Rule(Expr, AS, "x", Plus, Expr, AS, "y", nameof(AddImpl))]
        [Rule(Expr, AS, "x", Minus, Expr, AS, "y", nameof(SubtractImpl))]
        [Rule(Expr, AS, "x", Times, Expr, AS, "y", nameof(MultiplyImpl))]
        [Rule(Expr, AS, "x", Divide, Expr, AS, "y", nameof(DivideImpl))]
        [Rule(Minus, Expr, AS, "y", WITHPARAM, "x", 0, nameof(SubtractImpl))]
        [Rule(OpenBracket, Expr, AS, "val", CloseBracket, nameof(Identity))]
        [Rule(Plus, Expr, AS, "val", nameof(Identity))]
        [Rule(AbsoluteValueBar, Expr, AS, "val", AbsoluteValueBar, nameof(AbsImpl))]
        [Rule(Number, AS, "val", nameof(Identity))]
        Expr,
    }
    static decimal AddImpl(decimal x, decimal y) => x + y;
    static decimal SubtractImpl(decimal x, decimal y) => x - y;
    static decimal MultiplyImpl(decimal x, decimal y) => x * y;
    static decimal DivideImpl(decimal x, decimal y) => x / y;
    static decimal AbsImpl(decimal val) => Math.Abs(val);
    static T Identity<T>(T val) => val;

    // TESTCODE
    public static void Test()
    {
        TestSourceGenMath parser = new();
        foreach (var testCase1 in TestCases.Split(NewLine))
        {
            if (testCase1.StartsWith("#")) continue;
            var testCase = testCase1.Trim();
            var a = testCase.Split(" = ");
            var expr = a[0];
            var ans = decimal.Parse(a[1]);
            var input = GetTerminals(expr);
            var output = parser.Parse(input);
            if (output != ans)
            {
                Debugger.Break();
            }
        }
    }
    static IEnumerable<ITerminalValue> GetTerminals(string expr)
    {
        var s = MathLexer.StreamOf(expr);
        var lexer = new MathLexer.MathTextLexer(s);
        foreach (var token in lexer.GetTokens())
        {
            if (token.TokenType is MathLexer.Terminals.Integer && token is IToken<MathLexer.Terminals, decimal> d)
            {
                yield return CreateValue(Number, d.Data);
            }
            if (token.TokenType is MathLexer.Terminals.Plus) yield return CreateValue(Plus);
            if (token.TokenType is MathLexer.Terminals.Minus) yield return CreateValue(Minus);
            if (token.TokenType is MathLexer.Terminals.Times) yield return CreateValue(Times);
            if (token.TokenType is MathLexer.Terminals.Divide) yield return CreateValue(Divide);
            if (token.TokenType is MathLexer.Terminals.OpenBracket) yield return CreateValue(OpenBracket);
            if (token.TokenType is MathLexer.Terminals.CloseBracket) yield return CreateValue(CloseBracket);
            if (token.TokenType is MathLexer.Terminals.AbsoluteValueBar) yield return CreateValue(AbsoluteValueBar);
        }
    }
    static class MathLexer
    {
        public static StreamSeeker StreamOf(string text) => new(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        
        public class MathTextLexer(ITextSeekable text) : LexerBase<State, Terminals>(text, State.Initial)
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
        public enum State { Initial }
        public enum Terminals
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
}
