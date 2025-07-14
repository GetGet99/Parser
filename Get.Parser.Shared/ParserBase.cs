using Get.PLShared;

namespace Get.Parser;

public abstract class ParserBase<Terminal, NonTerminal, TOut>
    where Terminal : struct, Enum where NonTerminal : struct, Enum
{
    protected const ParserSourceGeneratorKeywords AS = ParserSourceGeneratorKeywords.As;
    protected const ParserSourceGeneratorKeywords WITHPARAM = ParserSourceGeneratorKeywords.WithParam;
    protected const ParserSourceGeneratorKeywords FUNCCALL = ParserSourceGeneratorKeywords.FuncCall;
    protected const ParserSourceGeneratorKeywords WITHPRECDENCE = ParserSourceGeneratorKeywords.WithPrecedence;
    /// <summary>
    /// <code>
    /// func EMPTYLIST:
    ///     return []
    /// </code>
    /// </summary>
    protected const ParserSourceGeneratorKeywords EMPTYLIST = ParserSourceGeneratorKeywords.EmptyList;
    /// <summary>
    /// <code>
    /// func SINGLELIST(VALUE):
    ///     return [VALUE]
    /// </code>
    /// </summary>
    protected const ParserSourceGeneratorKeywords SINGLELIST = ParserSourceGeneratorKeywords.SingleList;
    /// <summary>
    /// <code>
    /// func APPENDLIST(LIST, VALUE):
    ///     LIST.Add(VALUE)
    ///     return LIST
    /// </code>
    /// </summary>
    protected const ParserSourceGeneratorKeywords APPENDLIST = ParserSourceGeneratorKeywords.AppendList;
    /// <summary>
    /// <code>
    /// func IDENTITY(VALUE):
    ///     return VALUE
    /// </code>
    /// </summary>
    protected const ParserSourceGeneratorKeywords IDENTITY = ParserSourceGeneratorKeywords.Identity;
    /// <summary>
    /// Parameter to APPENDLIST
    /// </summary>
    protected const ParserSourceGeneratorKeywords LIST = ParserSourceGeneratorKeywords.List;
    /// <summary>
    /// Parameter to IDENTITY, SINGLELIST, APPENDLIST
    /// </summary>
    protected const ParserSourceGeneratorKeywords VALUE = ParserSourceGeneratorKeywords.Value;
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
    record class NonTerminalValue(NonTerminal NonTerminal) : INonTerminalValue
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        ISyntaxElement ISyntaxElementValue.WithoutValue => new NonTerminalWrapper(NonTerminal);
        public override string ToString() => $"{NonTerminal}";
    }
    record class NonTerminalValue<T>(NonTerminal NonTerminal, T Value) : INonTerminalValue<T>
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        ISyntaxElement ISyntaxElementValue.WithoutValue => new NonTerminalWrapper(NonTerminal);
        public override string ToString() => $"{NonTerminal}[{Value}]";
    }
    record class TerminalValue<T>(Terminal Terminal, T Value) : ITerminalValue<T>
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        ISyntaxElement ISyntaxElementValue.WithoutValue => new TerminalWrapper(Terminal);
        ITerminal ITerminalValue.WithoutValue => new TerminalWrapper(Terminal);
        public override string ToString() => $"{Terminal}[{Value}]";
    }
    record class TerminalValue(Terminal Terminal) : ITerminalValue
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        ISyntaxElement ISyntaxElementValue.WithoutValue => new TerminalWrapper(Terminal);
        ITerminal ITerminalValue.WithoutValue => new TerminalWrapper(Terminal);
        public override string ToString() => $"{Terminal}";
    }

}