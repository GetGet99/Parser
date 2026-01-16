using Get.RegexMachine;
using Get.PLShared;
namespace Get.Lexer;
public abstract class LexerBase<TState, TTokenEnum> where TTokenEnum : Enum where TState : Enum
{
    public record class Token<TData>(Position Start, Position End, TTokenEnum TokenType, TData Data) : Token(Start, End, TokenType), IToken<TTokenEnum, TData>
    {
        public override string ToString()
            => $"{Start}-{End}: {TokenType} {Data}";
    }
    public record class Token(Position Start, Position End, TTokenEnum TokenType) : IToken<TTokenEnum>
    {
        public override string ToString()
            => $"{Start}-{End}: {TokenType}";
    }
    protected bool HasReachedEOF
    {
        get
        {
            if (TextSeeker.MoveNext())
            {
                // reverse back, we don't want the next character
                TextSeeker.Reverse(1);
                // no, has not reached EOF
                return false;
            } else
            {
                // yes, EOF reached
                return true;
            }
        }
    }
    protected bool HasEnded { get; private set; } = false;
    /// <remarks>Intended for Source Generator Use</remarks>
    protected Func<IToken<TTokenEnum>?> Empty()
    {
        return () => null;
    }
    /// <remarks>Intended for Source Generator Use</remarks>
    protected Func<IToken<TTokenEnum>?> Empty(Action act)
    {
        return delegate
        {
            act();
            return null;
        };
    }
    /// <remarks>Intended for Source Generator Use</remarks>
    protected Func<IToken<TTokenEnum>?> MakeFunc(TTokenEnum TokenType)
    {
        return () => Make(TokenType);
    }
    /// <remarks>Intended for Source Generator Use</remarks>
    protected Func<IToken<TTokenEnum>?> MakeFunc<TData>(TTokenEnum TokenType, Func<TData> func)
    {
        return () => Make(TokenType, func());
    }
    /// <summary>
    /// Makes a new token based on <see cref="MatchedStartPosition"/>, <see cref="MatchedEndPosition"/>
    /// </summary>
    /// <param name="TokenType">The token type to be made</param>
    /// <returns>A new token with given token type</returns>
    protected IToken<TTokenEnum> Make(TTokenEnum TokenType)
    {
        return new Token(MatchedStartPosition, MatchedEndPosition, TokenType);
    }
    /// <summary>
    /// Makes a new token based on <see cref="MatchedStartPosition"/>, <see cref="MatchedEndPosition"/>
    /// </summary>
    /// <param name="TokenType">The token type to be made</param>
    /// <returns>A new token with given token type and data</returns>
    protected IToken<TTokenEnum> Make<TData>(TTokenEnum TokenType, TData data)
    {
        return new Token<TData>(MatchedStartPosition, MatchedEndPosition, TokenType, data);
    }
    protected LexerBase(ITextSeekable text, TState initialState)
    {
        TextSeeker = text;
        CurrentState = initialState;
        DFAs = DFASourceGenOutput();
        DFA = DFAs[initialState];
    }
    readonly ITextSeekable TextSeeker;

    /// <summary>
    /// Specifies that there are no more tokens
    /// </summary>
    protected void End() {
        HasEnded = true;
    }
    protected string MatchedText { get; private set; } = "";
    protected int MatchedStartRawPosition { get; private set; }
    protected int MatchedEndRawPosition { get; private set; }
    protected Position MatchedStartPosition { get; private set; }
    protected Position MatchedEndPosition { get; private set; }
    /// <summary>
    /// Reverse ITextSeekable by the given number of characters
    /// See <seealso cref="ISeekable{T}.Reverse"/> for more information
    /// </summary>
    /// <param name="characters">The number of characters to be reversed</param>
    protected void Reverse(int characters)
    {
        TextSeeker.Reverse(characters);
    }
    /// <summary>
    /// Set the new state to be used after the current function returns.
    /// </summary>
    /// <param name="state">The new state</param>
    protected void GoTo(TState state)
    {
        CurrentState = state;
        DFA = DFAs[state];
    }

    public TState CurrentState { get; private set; }

    /// <summary>
    /// Adds the token to the queue. Once your function returns,
    /// each token in the queue will be emitted first. Then, the
    /// token that your function returns will be emitted.
    /// </summary>
    /// <remarks>
    /// At the beginning of your function call, the queue is empty.
    /// After your function call completes and all tokens are emitted,
    /// the queue will be cleared.
    /// </remarks>
    /// <param name="token">The token to be sent out</param>
    protected void YieldToken(IToken<TTokenEnum> token)
        => InternalQueue.Enqueue(token);
    /// <param name="TokenType">The token type to be added</param>
    /// <remarks>
    /// The token generated will be based on <see cref="MatchedStartPosition"/>, <see cref="MatchedEndPosition"/>.
    /// If you need different starting and ending position, please consider using
    /// <see cref="YieldToken(IToken{TTokenEnum})"/> and manually constructing
    /// <see cref="Token"/> or <see cref="Token{TData}"/>
    /// <br/><br/>
    /// At the beginning of your function call, the queue is empty.
    /// After your function call completes and all tokens are emitted,
    /// the queue will be cleared.
    /// </remarks>
    /// <inheritdoc cref="YieldToken(IToken{TTokenEnum})"/>
    protected void YieldToken(TTokenEnum TokenType)
        => YieldToken(Make(TokenType));

    Queue<IToken<TTokenEnum>> InternalQueue { get; } = [];

    RegexCompiler<Func<IToken<TTokenEnum>?>>.DFAState DFA;
    readonly Dictionary<TState, RegexCompiler<Func<IToken<TTokenEnum>?>>.DFAState> DFAs;
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
                while (InternalQueue.Count > 0)
                    yield return InternalQueue.Dequeue();
                if (a != null) yield return a;
                if (!HasEnded) continue;
            }
            while (InternalQueue.Count > 0)
                yield return InternalQueue.Dequeue();
            yield break; // TODO: Handle EOF
        }
    }
    /// <summary>
    /// DFA from the source generator
    /// </summary>
    /// <returns>The compiled DFA used to generate tokens</returns>
    public abstract Dictionary<TState, RegexCompiler<Func<IToken<TTokenEnum>?>>.DFAState> DFASourceGenOutput();
}