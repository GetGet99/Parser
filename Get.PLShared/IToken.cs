namespace Get.PLShared;
/// <summary>
/// Represents a token produced by the lexer, with a typed payload.
/// </summary>
/// <typeparam name="TToken">The terminal token enum type.</typeparam>
/// <typeparam name="TData">The type of the data payload attached to this token.</typeparam>
public interface IToken<TToken, TData> : IToken<TToken> where TToken : Enum
{
    /// <summary>The typed data payload attached to this token.</summary>
    TData Data { get; }
}
/// <summary>
/// Represents a token produced by the lexer, identified by its terminal type and source positions.
/// </summary>
/// <typeparam name="TToken">The terminal token enum type.</typeparam>
public interface IToken<TToken> where TToken : Enum
{
    /// <summary>The zero-based start position of this token in the source text.</summary>
    Position Start { get; }
    /// <summary>The zero-based end position of this token in the source text.</summary>
    Position End { get; }
    /// <summary>The terminal type of this token.</summary>
    TToken TokenType { get; }
}
