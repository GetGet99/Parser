namespace Get.Parser;

public abstract class ParserBase<Terminal, NonTerminal, TOut>
    where Terminal : struct, Enum where NonTerminal : struct, Enum
{
    protected const Keywords AS = Keywords.As;
    protected const Keywords WITHPARAM = Keywords.WithParam;
    protected const Keywords FUNCCALL = Keywords.FuncCall;
    protected const Keywords WITHPRECDENCE = Keywords.WithPrecedence;
    public ParserBase()
    {
        ParserDFA = GenerateDFA();
    }
    protected abstract ILRParserDFA GenerateDFA();
    readonly ILRParserDFA ParserDFA;
    public TOut Parse(IEnumerable<ITerminalValue?> inputTerminals)
    {
        return LRParserRunner<TOut>.Parse(ParserDFA, inputTerminals);
    }
    protected virtual bool IsCustomErrorHandlingEnabled => false;
    protected static INonTerminalValue CreateValue(NonTerminal nt)
        => new NonTerminalValue(nt);
    protected static INonTerminalValue<T> CreateValue<T>(NonTerminal nt, T value)
        => new NonTerminalValue<T>(nt, value);
    protected static ITerminalValue CreateValue(Terminal t)
        => new TerminalValue(t);
    protected static ITerminalValue<T> CreateValue<T>(Terminal t, T value)
        => new TerminalValue<T>(t, value);
    protected static INonTerminal Syntax(NonTerminal nt)
        => new NonTerminalWrapper(nt);
    protected static ITerminal Syntax(Terminal nt)
        => new TerminalWrapper(nt);
    protected static T GetValue<T>(ISyntaxElementValue value)
    {
        if (value is not ISyntaxElementValue<T> v)
        {
            throw new InvalidCastException();
        } else
        {
            return v.Value;
        }
    }
    protected static ICFGRuleWithPrecedence CreateRule(NonTerminal Target, IReadOnlyList<ISyntaxElement> Expressions, Func<ISyntaxElementValue[], INonTerminalValue> Implementation, Terminal? Precedence = null)
        => new CFGRule(Target, Expressions, Implementation, Precedence);
    protected enum Keywords : byte
    {
        As,
        WithParam,
        WithPrecedence,
        FuncCall
    }
    readonly record struct CFGRule(NonTerminal Target, IReadOnlyList<ISyntaxElement> Expressions, Func<ISyntaxElementValue[], INonTerminalValue> Implementation, Terminal? Precedence) : ICFGRuleWithPrecedence
    {
        ITerminal? ICFGRuleWithPrecedence.PrecedenceTerminal => Precedence.HasValue ? Syntax(Precedence.Value) : null;

        INonTerminal ICFGRule.Target => Syntax(Target);

        INonTerminalValue ICFGRule.GetValue(ISyntaxElementValue[] value) => Implementation(value);
    }

    readonly record struct TerminalWrapper(Terminal Terminal) : ITerminal
    {
        public override string ToString() => Terminal.ToString();
    }
    readonly record struct NonTerminalWrapper(NonTerminal NonTerminal) : INonTerminal
    {
        public override string ToString() => NonTerminal.ToString();
    }
    readonly record struct NonTerminalValue(NonTerminal NonTerminal) : INonTerminalValue
    {
        ISyntaxElement ISyntaxElementValue.WithoutValue => new NonTerminalWrapper(NonTerminal);
        public override string ToString() => $"{NonTerminal}";
    }
    readonly record struct NonTerminalValue<T>(NonTerminal NonTerminal, T Value) : INonTerminalValue<T>
    {
        ISyntaxElement ISyntaxElementValue.WithoutValue => new NonTerminalWrapper(NonTerminal);
        public override string ToString() => $"{NonTerminal}[{Value}]";
    }
    readonly record struct TerminalValue<T>(Terminal Terminal, T Value) : ITerminalValue<T>
    {
        ISyntaxElement ISyntaxElementValue.WithoutValue => new TerminalWrapper(Terminal);
        ITerminal ITerminalValue.WithoutValue => new TerminalWrapper(Terminal);
        public override string ToString() => $"{Terminal}[{Value}]";
    }
    readonly record struct TerminalValue(Terminal Terminal) : ITerminalValue
    {
        ISyntaxElement ISyntaxElementValue.WithoutValue => new TerminalWrapper(Terminal);
        ITerminal ITerminalValue.WithoutValue => new TerminalWrapper(Terminal);
        public override string ToString() => $"{Terminal}";
    }

}