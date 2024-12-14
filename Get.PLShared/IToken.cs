namespace Get.PLShared;
public interface IToken<TToken, TData> : IToken<TToken> where TToken : Enum
{
    TData Data { get; }
}
public interface IToken<TToken> where TToken : Enum
{
    Position Start { get; }
    Position End { get; }
    TToken TokenType { get; }
}
