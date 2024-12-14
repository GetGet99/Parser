using Get.PLShared;
using Get.RegexMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Get.Lexer.Test;

class CustomLexer(ITextSeekable text) : LexerBase<State, Terminals>(text, State.Initial)
{
    public override Dictionary<State, RegexCompiler<Func<IToken<Terminals>?>>.DFAState> DFASourceGenOutput()
    {
        Dictionary<State, RegexCompiler<Func<IToken<Terminals>?>>.DFAState> dict = [];
        dict[State.Initial] = RegexCompiler<Func<IToken<Terminals>?>>.GenerateDFA([
            new(@"[0-9]+", MakeFunc(Terminals.Integer, () => int.Parse(MatchedText))),
            new(@"[a-zA-Z_][a-zA-Z_0-9]*", MakeFunc(Terminals.Identifier, () => MatchedText)),
            new(@"\+", MakeFunc(Terminals.Plus)),
            new(@"-", MakeFunc(Terminals.Minus)),
            new(@"[\t \r\n]+", Empty()),
            new(@"\*", MakeFunc(Terminals.Times))
        ], RegexConflictBehavior.Throw);
        return dict;
    }
}
enum State { Initial }
enum Terminals
{
    Integer,
    Identifier,
    Plus,
    Minus,
    Whitespace,
    Times
}
