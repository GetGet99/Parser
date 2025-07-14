#pragma warning disable CS0436 // Type conflicts with imported type
using Get.Lexer;
using Get.RegexMachine;
namespace Get.LangSupport.Test;

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
    private partial int BuildHex()
    {
        throw new NotImplementedException();
    }
    private partial string BuildString()
    {
        return MatchedText;
    }
    public enum State { Initial }
    public enum Terminals
    {
        [Type<int>]
        [NumericScope(NumericType.Decimal, Regexes = [@"[0-9]+"])]
        [Regex<int>(@"[0-9]+", "BuildInt")]
        [NumericScope(NumericType.Hex, Regexes = ["0x[0-9a-fA-F]+"], Priority = 1)]
        [Regex<int>(@"0x[0-9a-fA-F]+", "BuildHex")]
        Integer,
        [Type<string>]
        [Regex<string>(@"[a-zA-Z_][a-zA-Z_0-9]*", "BuildString")]
        [VariableScope]
        Identifier,
        [Regex(@"\+")]
        [OperatorScope]
        Plus,
        [Regex(@"\-")]
        [OperatorScope]
        Minus,
        [Regex(@" ")]
        Whitespace,
        [OperatorScope]
        [Regex(@"\*")]
        Times
    }
}

