#pragma warning disable CS0436 // Type conflicts with imported type
using Get.RegexMachine;
namespace Get.Lexer.Test;

[Lexer<Terminals>]
partial class CustomLexerSourceGen(ITextSeekable text) : LexerBase<
    CustomLexerSourceGen.State,
    CustomLexerSourceGen.Terminals
>(text, State.Initial)
{
    private partial int BuildInt()
    {
        return int.Parse(MatchedText);
    }
    private partial int BuildInt1()
    {
        return int.Parse(MatchedText);
    }
    private partial string BuildString()
    {
        return MatchedText;
    }
    public enum State { Initial }
    [CompileTimeConflictCheck]
    public enum Terminals
    {
        [Type<int>]
        [Regex<int>(@"[0-9]+", "BuildInt")]
        Integer,
        [Type<string>]
        [Regex<string>(@"[a-zA-Z_][a-zA-Z_0-9]*", "BuildString")]
        Identifier,
        [Regex(@"\+")]
        Plus,
        [Regex(@"\-")]
        Minus,
        [Regex(@" ")]
        Whitespace,
        [Regex(@"\*")]
        Times
    }
}

