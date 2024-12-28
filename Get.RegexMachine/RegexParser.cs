using Get.Parser;
using System.Diagnostics.CodeAnalysis;
namespace Get.RegexMachine;
using static RegexParser.Terminal;
using static RegexParser.NonTerminal;

[Parser(FinalRegex)]
[Precedence(
    Alternation, Associativity.Left,
    Concatenation, Associativity.Left
)]
partial class RegexParser : ParserBase<RegexParser.Terminal, RegexParser.NonTerminal, RegexNFAs>
{
    public static RegexParser Instance { get; } = new();
    public Func<INFAState> CreateEmptyNFAState = null!;
    RegexNFAs EmptyString()
    {
        var state = CreateEmptyNFAState();
        // just a simple epsilon state
        return new RegexNFAs(state, state);
    }
    RegexNFAs Alt(RegexNFAs a, RegexNFAs b)
    {
        var newStartState = CreateEmptyNFAState();
        newStartState.AddEpsilonTransition(a.StartState);
        newStartState.AddEpsilonTransition(b.StartState);
        var newEndState = CreateEmptyNFAState();
        a.StartState.AddEpsilonTransition(newEndState);
        b.StartState.AddEpsilonTransition(newEndState);
        return new(newStartState, newEndState);
    }
    RegexNFAs Alt(RegexNFAs a) => Alt(a, EmptyString());
    RegexNFAs Cat(RegexNFAs a, RegexNFAs b)
    {
        a.EndState.AddEpsilonTransition(b.StartState);
        return new RegexNFAs(a.StartState, b.EndState);
    }
    RegexNFAs StarHandler(RegexNFAs a)
    {
        INFAState newStartState = CreateEmptyNFAState();
        newStartState.AddEpsilonTransition(a.StartState);
        a.StartState.AddEpsilonTransition(newStartState);
        a.StartState = newStartState;
        a.EndState = CreateEmptyNFAState();
        newStartState.AddEpsilonTransition(a.EndState);
        return a;
    }
    RegexNFAs PlusHandler(RegexNFAs a)
    {

        INFAState newStartState = CreateEmptyNFAState();
        INFAState newEndState = CreateEmptyNFAState();
        newStartState.AddEpsilonTransition(a.StartState);
        a.EndState.AddEpsilonTransition(newEndState);
        newEndState.AddEpsilonTransition(newStartState);
        a.StartState = newStartState;
        a.EndState = newEndState;
        return a;
    }
    RegexNFAs ClassHandler(HashSet<char> chars, bool inverse)
    {

        INFAState newStartState = CreateEmptyNFAState();
        INFAState newEndState = CreateEmptyNFAState();
        if (!inverse)
        {
            foreach (var c in chars)
                newStartState.AddTransition(c, newEndState);
        } else
        {
            // VERY SLOW!
            for (char c = char.MinValue; c <= char.MaxValue; c++)
            {
                if (!chars.Contains(c))
                    newStartState.AddTransition(c, newEndState);
            }
        }
        return new(newStartState, newEndState);
    }
    RegexNFAs CharHandler(char c)
    {
        INFAState newStartState = CreateEmptyNFAState();
        INFAState newEndState = CreateEmptyNFAState();
        newStartState.AddTransition(c, newEndState);
        return new(newStartState, newEndState);
    }
    static T Identity<T>(T val) => val;
    static IEnumerable<char> SingleCharSet(char a)
    {
        yield return a;
    }
    static IEnumerable<char> RangeCharSet(char a, char b)
    {
        for (char c = a; c <= b; c++)
            yield return c;
    }
    static HashSet<char> EmptyCharSet() => [];
    static HashSet<char> AddAll(HashSet<char> target, IEnumerable<char> src)
    {
        target.UnionWith(src);
        return target;
    }
    static char SpecialEscapeNCSE(char val) => val switch
    {
        'r' => '\r',
        'n' => '\n',
        't' => '\t',
        _ => throw new InvalidDataException("the given token should not be NormalCharOrSpecialEscape")
    };
    public enum Terminal
    {
        [Type<char>]
        OpenBracket,
        [Type<char>]
        CloseBracket,
        [Type<char>]
        OpenSquareBracket,
        [Type<char>]
        CloseSquareBracket,
        [Type<char>]
        Alternation,
        [Type<char>]
        Star,
        [Type<char>]
        Plus,
        [Type<char>]
        Caret,
        [Type<char>]
        Backslash,
        [Type<char>]
        SingleQuote,
        [Type<char>]
        DoubleQuote,
        [Type<char>]
        Dot,
        [Type<char>]
        Dash,
        [Type<char>]
        NormalCharOrSpecialEscape,
        [Type<char>]
        Other,
        // not a real terminal, used for precedence
        Concatenation
    }
    public enum NonTerminal
    {
        [Type<RegexNFAs>]
        [Rule(Expr, AS, "val", nameof(Identity))]
        [Rule(nameof(EmptyString))]
        [Rule(Alternation, nameof(EmptyString))] // Regex @"|" is just like empty string
        [Rule(Alternation, Expr, AS, "a", nameof(Alt))]
        FinalRegex,
        [Type<RegexNFAs>]
        [Rule(Primary, AS, "val", nameof(Identity))]
        [Rule(Expr, AS, "a", Alternation, Expr, AS, "b", nameof(Alt))] // a|b
        [Rule(Expr, AS, "a", Alternation, nameof(Alt))] // expr|
        [Rule(Expr, AS, "a", Expr, AS, "b", nameof(Cat), WITHPRECDENCE, Concatenation)] // ab
        [Rule(Primary, AS, "a", Star, nameof(StarHandler))] // expr*
        [Rule(Primary, AS, "a", Plus, nameof(PlusHandler))] // expr+
        Expr,
        [Type<RegexNFAs>]
        [Rule(OpenBracket, CloseBracket, nameof(EmptyString))] // ()
        [Rule(OpenBracket, Expr, AS, "val", CloseBracket, nameof(Identity))] // (expr)
        [Rule(OpenBracket, Alternation, CloseBracket, nameof(EmptyString))] // (|) is basically ()
        [Rule(OpenBracket, Alternation, Expr, AS, "a", CloseBracket, nameof(Alt))] // (|expr)
        [Rule(NonClassCharacter, AS, "c", nameof(CharHandler))]
        [Rule(OpenSquareBracket, Classes, AS, "chars", CloseSquareBracket, WITHPARAM, "inverse", false, nameof(ClassHandler))] // [classes]
        [Rule(OpenSquareBracket, Caret, Classes, AS, "chars", CloseSquareBracket, WITHPARAM, "inverse", true, nameof(ClassHandler))] // [^classes]
        Primary,
        /// <summary>
        /// Character outside the [] notation
        /// </summary>
        [Type<char>]
        [Rule(Character, AS, "val", nameof(Identity))]
        [Rule(Caret, AS, "val", nameof(Identity))]
        [Rule(Dash, AS, "val", nameof(Identity))]
        NonClassCharacter,
        [Type<char>]
        [Rule(Other, AS, "val", nameof(Identity))]
        [Rule(SingleQuote, AS, "val", nameof(Identity))]
        [Rule(DoubleQuote, AS, "val", nameof(Identity))]
        [Rule(NormalCharOrSpecialEscape, AS, "val", nameof(Identity))]
        [Rule(Backslash, NormalCharOrSpecialEscape, AS, "val", nameof(SpecialEscapeNCSE))]
        // other escape characters
        [Rule(Backslash, OpenBracket, AS, "val", nameof(Identity))]
        [Rule(Backslash, CloseBracket, AS, "val", nameof(Identity))]
        [Rule(Backslash, OpenSquareBracket, AS, "val", nameof(Identity))]
        [Rule(Backslash, CloseSquareBracket, AS, "val", nameof(Identity))]
        [Rule(Backslash, Alternation, AS, "val", nameof(Identity))]
        [Rule(Backslash, Star, AS, "val", nameof(Identity))]
        [Rule(Backslash, Plus, AS, "val", nameof(Identity))]
        [Rule(Backslash, Caret, AS, "val", nameof(Identity))]
        [Rule(Backslash, Backslash, AS, "val", nameof(Identity))]
        [Rule(Backslash, SingleQuote, AS, "val", nameof(Identity))]
        [Rule(Backslash, DoubleQuote, AS, "val", nameof(Identity))]
        [Rule(Backslash, Dot, AS, "val", nameof(Identity))]
        [Rule(Backslash, Dash, AS, "val", nameof(Identity))]
        Character,
        [Type<IEnumerable<char>>]
        [Rule(Character, AS, "a", nameof(SingleCharSet))]
        [Rule(Character, AS, "a", Dash, Character, AS, "b", nameof(RangeCharSet))]
        Class,
        [Type<HashSet<char>>]
        [Rule(nameof(EmptyCharSet))]
        [Rule(Classes, AS, "target", Class, AS, "src", nameof(AddAll))]
        Classes
    }
    public RegexNFAs Parse([StringSyntax(StringSyntaxAttribute.Regex)] string regex)
    {
        return Parse(Tokens(regex));
    }
    static IEnumerable<ITerminalValue<char>> Tokens([StringSyntax(StringSyntaxAttribute.Regex)] string str)
    {
        foreach (var c in str)
        {
            yield return CreateValue(c switch
            {
                '(' => OpenBracket,
                ')' => CloseBracket,
                '[' => OpenSquareBracket,
                ']' => CloseSquareBracket,
                '|' => Alternation,
                '*' => Star,
                '+' => Plus,
                '^' => Caret,
                '\\' => Backslash,
                '\'' => SingleQuote,
                '\"' => DoubleQuote,
                '.' => Dot,
                '-' => Dash,
                'n' or 'r' or 't' => NormalCharOrSpecialEscape,
                _ => Other
            }, c);
        }
    }
}
record struct RegexNFAs(INFAState StartState, INFAState EndState);
