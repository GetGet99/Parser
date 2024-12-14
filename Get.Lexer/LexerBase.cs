using Get.RegexMachine;
using Get.PLShared;
namespace Get.Lexer;
public abstract class LexerBase<TState, TTokenEnum> where TTokenEnum : Enum where TState : Enum
{
    record class Token<TData>(Position Start, Position End, TTokenEnum TokenType, TData Data) : Token(Start, End, TokenType), IToken<TTokenEnum, TData>
    {
        public override string ToString()
            => $"{Start}-{End}: {TokenType} {Data}";
    }
    record class Token(Position Start, Position End, TTokenEnum TokenType) : IToken<TTokenEnum>
    {
        public override string ToString()
            => $"{Start}-{End}: {TokenType}";
    }
    /// <remarks>Intended for Source Generator Use</remarks>
    protected Func<IToken<TTokenEnum>?> Empty()
    {
        return () => null;
    }
    protected Func<IToken<TTokenEnum>?> MakeFunc(TTokenEnum TokenType)
    {
        return () => new Token(MatchedStartPosition, MatchedEndPosition, TokenType);
    }
    /// <remarks>Intended for Source Generator Use</remarks>
    protected Func<IToken<TTokenEnum>?> MakeFunc<TData>(TTokenEnum TokenType, Func<TData> func)
    {
        return () => new Token<TData>(MatchedStartPosition, MatchedEndPosition, TokenType, func());
    }
    protected LexerBase(ITextSeekable text, TState initialState)
    {
        TextSeeker = text;
        DFAs = DFASourceGenOutput();
        DFA = DFAs[initialState];
    }
    readonly ITextSeekable TextSeeker;

    protected string MatchedText { get; private set; } = "";
    protected int MatchedStartRawPosition { get; private set; }
    protected int MatchedEndRawPosition { get; private set; }
    protected Position MatchedStartPosition { get; private set; }
    protected Position MatchedEndPosition { get; private set; }
    protected void Reverse(int characters)
    {
        TextSeeker.Reverse(characters);
    }
    protected void GoTo(TState state)
    {
        DFA = DFAs[state];
    }


    RegexCompiler<Func<IToken<TTokenEnum>?>>.DFAState DFA;
    Dictionary<TState, RegexCompiler<Func<IToken<TTokenEnum>?>>.DFAState> DFAs;
    public IEnumerable<IToken<TTokenEnum>> GetTokens()
    {
        while (true)
        {
            MatchedStartRawPosition = TextSeeker.CurrentPosition;
            var output = RegexRunner<Func<IToken<TTokenEnum>?>>.NextWithPosition(DFA, TextSeeker);
            if (output is not null)
            {
                MatchedStartPosition = output.Value.Start;
                MatchedEndPosition = output.Value.End;
                MatchedText = output.Value.matchedText;
                var a = output.Value.value();
                if (a != null) yield return a;
                continue;
            }
            yield break; // TODO: Handle EOF
        }
    }
    /// <summary>
    /// DFA from the source generator
    /// </summary>
    /// <returns>The compiled DFA used to generate tokens</returns>
    public abstract Dictionary<TState, RegexCompiler<Func<IToken<TTokenEnum>?>>.DFAState> DFASourceGenOutput();
}