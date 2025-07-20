namespace Get.Parser.Test;
using Get.Parser;
using static ShiftReduceConflict;
using static ShiftReduceConflict.Terminal;
using static ShiftReduceConflict.NonTerminal;

[Parser(Start)]
[Precedence(Plus, Associativity.Left)]
partial class ShiftReduceConflict : ParserBase<Terminal, NonTerminal, int>
{
    public enum NonTerminal
    {
        [Type<int>]
        [Rule(Expr, AS, VALUE, IDENTITY)]
        Start,

        [Type<int>]
        [Rule(Expr, AS, "x", Plus, Expr, AS, "y", nameof(Add))]
        [Rule(Number, AS, VALUE, IDENTITY)]
        Expr,
    }

    public enum Terminal
    {
        Plus,
        [Type<int>] Number
    }

    static int Add(int x, int y) => x + y;
}
