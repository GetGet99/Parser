using Get.Parser;
using Get.PLShared;

namespace Get.RegexMachine.Test;

[TestClass]
public class TestLRParserDFAGen
{
    [TestMethod]
    public void SimpleGrammar_CreateDFA_Succeeds()
    {
        var dfa = CreateUnambiguousExprGrammar();
        Assert.IsNotNull(dfa);
    }

    [TestMethod]
    public void SimpleGrammar_ParseAddition()
    {
        var dfa = CreateUnambiguousExprGrammar();
        var result = LRParserRunner<decimal>.Parse(dfa, Tokens("3+4"));
        Assert.AreEqual(7m, result);
    }

    [TestMethod]
    public void SimpleGrammar_ParseMultiplication()
    {
        var dfa = CreateUnambiguousExprGrammar();
        var result = LRParserRunner<decimal>.Parse(dfa, Tokens("3*4"));
        Assert.AreEqual(12m, result);
    }

    [TestMethod]
    public void SimpleGrammar_ParseParens()
    {
        var dfa = CreateUnambiguousExprGrammar();
        var result = LRParserRunner<decimal>.Parse(dfa, Tokens("(3+4)*5"));
        Assert.AreEqual(35m, result);
    }

    [TestMethod]
    public void ReduceReduceConflict_Throws()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal?>.Default);
        Assert.ThrowsException<LRReduceReduceConflictException>(() =>
            gen.CreateDFA([
                new GRule(NT.S, [NT.A], _ => "s"),
                new GRule(NT.S, [NT.B], _ => "s"),
                new GRule(NT.A, [T.a], _ => "a"),
                new GRule(NT.B, [T.a], _ => "b"),
            ], NT.S, [])
        );
    }

    [TestMethod]
    public void ShiftReduceConflict_WithoutPrecedence_Throws()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal?>.Default);
        Assert.ThrowsException<LRShiftReduceConflictException>(() =>
            gen.CreateDFA([
                new GRule(NT.S, [NT.E], _ => 0m),
                new GRule(NT.E, [NT.E, T.plus, NT.E], _ => 0m),
                new GRule(NT.E, [T.num], _ => 0m),
            ], NT.S, [])
        );
    }

    static ITerminal[] Terminals(params T[] ts) => ts.Select(t => (ITerminal)t).ToArray();

    [TestMethod]
    public void AmbiguousGrammar_WithPrecedence_CreateDFA_Succeeds()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal?>.Default);
        var dfa = gen.CreateDFA([
            new GRule(NT.E, [NT.E, T.plus, NT.E], Reduce, precedenceTerminal: T.plus),
            new GRule(NT.E, [NT.E, T.times, NT.E], Reduce, precedenceTerminal: T.times),
            new GRule(NT.E, [T.num], Reduce),
        ], NT.E, [
            (Terminals(T.times), Associativity.Left),
            (Terminals(T.plus), Associativity.Left),
        ]);
        Assert.IsNotNull(dfa);
    }

    [TestMethod]
    public void AmbiguousGrammar_WithPrecedence_ParseAddition()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal?>.Default);
        var dfa = gen.CreateDFA([
            new GRule(NT.E, [NT.E, T.plus, NT.E], Reduce, precedenceTerminal: T.plus),
            new GRule(NT.E, [NT.E, T.times, NT.E], Reduce, precedenceTerminal: T.times),
            new GRule(NT.E, [T.num], Reduce),
        ], NT.E, [
            (Terminals(T.times), Associativity.Left),
            (Terminals(T.plus), Associativity.Left),
        ]);
        var result = LRParserRunner<decimal>.Parse(dfa, Tokens("1+2+3"));
        Assert.AreEqual(6m, result);
    }

    [TestMethod]
    public void AmbiguousGrammar_WithPrecedence_MultiplicationFirst()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal?>.Default);
        var dfa = gen.CreateDFA([
            new GRule(NT.E, [NT.E, T.plus, NT.E], Reduce, precedenceTerminal: T.plus),
            new GRule(NT.E, [NT.E, T.times, NT.E], Reduce, precedenceTerminal: T.times),
            new GRule(NT.E, [T.num], Reduce),
        ], NT.E, [
            (Terminals(T.times), Associativity.Left),
            (Terminals(T.plus), Associativity.Left),
        ]);
        var result = LRParserRunner<decimal>.Parse(dfa, Tokens("1+2*3"));
        Assert.AreEqual(7m, result);
    }

    [TestMethod]
    public void AmbiguousGrammar_WithPrecedence_ParensOverride()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal?>.Default);
        var dfa = gen.CreateDFA([
            new GRule(NT.E, [NT.E, T.plus, NT.E], Reduce, precedenceTerminal: T.plus),
            new GRule(NT.E, [NT.E, T.times, NT.E], Reduce, precedenceTerminal: T.times),
            new GRule(NT.E, [T.lparen, NT.E, T.rparen], Reduce),
            new GRule(NT.E, [T.num], Reduce),
        ], NT.E, [
            (Terminals(T.times), Associativity.Left),
            (Terminals(T.plus), Associativity.Left),
        ]);
        var result = LRParserRunner<decimal>.Parse(dfa, Tokens("(1+2)*3"));
        Assert.AreEqual(9m, result);
    }

    [TestMethod]
    public void PrecedenceTerminal_OverridesLastTerminal()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal?>.Default);
        // E → E + E with PrecedenceTerminal = times (overrides the + terminal)
        var dfa = gen.CreateDFA([
            new GRule(NT.E, [NT.E, T.plus, NT.E], Reduce, precedenceTerminal: T.times),
            new GRule(NT.E, [T.num], Reduce),
        ], NT.E, [
            (Terminals(T.times), Associativity.Left),
            (Terminals(T.plus), Associativity.Left),
        ]);
        // With + having lower precedence than *, but E → E+E has times precedence
        // 1 + 2 should resolve as: incoming + has lower precedence (index 1),
        // rule has times precedence (index 0), so rule > term → REDUCE
        // This effectively means the rule "wins" over the incoming +
        var result = LRParserRunner<decimal>.Parse(dfa, Tokens("1+2+3"));
        Assert.AreEqual(6m, result);
    }

    // --- Unambiguous expression grammar: E → E + T | T ; T → T * F | F ; F → num | ( E ) ---

    static ILRParserDFA CreateUnambiguousExprGrammar()
    {
        var gen = new LRParserDFAGen(EqualityComparer<INonTerminal>.Default, EqualityComparer<ITerminal?>.Default);
        return gen.CreateDFA([
            new GRule(NT.S, [NT.E], Reduce),
            new GRule(NT.E, [NT.E, T.plus, NT.T], Reduce),
            new GRule(NT.E, [NT.T], Reduce),
            new GRule(NT.T, [NT.T, T.times, NT.F], Reduce),
            new GRule(NT.T, [NT.F], Reduce),
            new GRule(NT.F, [T.num], Reduce),
            new GRule(NT.F, [T.lparen, NT.E, T.rparen], Reduce),
        ], NT.S, []);
    }

    static IEnumerable<ITerminalValue> Tokens(string expr)
    {
        for (int i = 0; i < expr.Length; i++)
        {
            var c = expr[i];
            if (c == ' ') continue;
            yield return c switch
            {
                '+' => new TV(T.plus),
                '*' => new TV(T.times),
                '(' => new TV(T.lparen),
                ')' => new TV(T.rparen),
                >= '0' and <= '9' => Num(c - '0'),
                _ => throw new InvalidOperationException($"Unexpected char: {c}")
            };
        }
    }

    // --- Test data types ---

    record struct NT(string Name) : INonTerminal
    {
        public static readonly NT S = new("S"), E = new("E"), T = new("T"), F = new("F"), A = new("A"), B = new("B");
        public override string ToString() => Name;
    }

    record struct T(string Name) : ITerminal
    {
        public static readonly T
            a = new("a"), plus = new("+"), times = new("*"),
            lparen = new("("), rparen = new(")"), num = new("num");
        public override string ToString() => Name;
    }

    record TV(T Terminal) : ITerminalValue
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        public ITerminal WithoutValue => Terminal;
        ISyntaxElement ISyntaxElementValue.WithoutValue => Terminal;
        public string? Value => null;
    }

    record NumTerminal(decimal Value) : ITerminal, ITerminalValue<decimal>
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        public ITerminal WithoutValue => T.num;
        ISyntaxElement ISyntaxElementValue.WithoutValue => WithoutValue;
    }
    static NumTerminal Num(int v) => new(v);

    record GRule : ICFGRule, ICFGRuleWithPrecedence
    {
        readonly INonTerminal _target;
        readonly IReadOnlyList<ISyntaxElement> _exprs;
        readonly ITerminal? _precedenceTerminal;
        public GRule(INonTerminal target, ISyntaxElement[] exprs, Func<ISyntaxElementValue[], object> reduce, ITerminal? precedenceTerminal = null)
        {
            _target = target;
            _exprs = exprs;
            _reduce = reduce;
            _precedenceTerminal = precedenceTerminal;
        }
        readonly Func<ISyntaxElementValue[], object> _reduce;
        public INonTerminal Target => _target;
        public IReadOnlyList<ISyntaxElement> Expressions => _exprs;
        public ITerminal? PrecedenceTerminal => _precedenceTerminal;
        public INonTerminalValue GetValue(ISyntaxElementValue[] values)
        {
            var result = _reduce(values);
            // typed value so LRParserRunner can extract the final result
            if (result is decimal d)
                return new NValT<decimal>(_target, d);
            if (result is string s)
                return new NValT<string>(_target, s);
            return new NVal(_target, result);
        }
    }

    record NVal(INonTerminal Type, object Val) : INonTerminalValue
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        ISyntaxElement ISyntaxElementValue.WithoutValue => Type;
    }

    record NValT<T>(INonTerminal Type, T Val) : INonTerminalValue<T>
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        T ISyntaxElementValue<T>.Value => Val;
        ISyntaxElement ISyntaxElementValue.WithoutValue => Type;
    }

    static decimal Eval(ISyntaxElementValue x) => x switch
    {
        INonTerminalValue<decimal> d => d.Value,
        ITerminalValue<decimal> td => td.Value,
        _ => throw new InvalidOperationException($"Cannot eval {x.GetType()}")
    };
    static object Reduce(ISyntaxElementValue[] x)
    {
        if (x.Length == 1)
        {
            // F → num: return raw decimal
            // T → F, E → T: x[0] is NValT<decimal>, Eval extracts it
            return Eval(x[0]);
        }
        if (x.Length == 3)
        {
            if (x[1] is TV op && (op.Terminal.Equals(T.plus) || op.Terminal.Equals(T.times)))
            {
                decimal left = Eval(x[0]), right = Eval(x[2]);
                return op.Terminal.Equals(T.plus) ? left + right : left * right;
            }
            // ( E )
            return Eval(x[1]);
        }
        throw new InvalidOperationException($"Unexpected reduction: {x.Length} elements");
    }
}
