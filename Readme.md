# Get's Parser and Lexer (ALPHA)

A general lexer and LR(1) parser implementation in C#.
It has not been tested througly yet.

See Get.Parser.Test and Get.Lexer.Test for some sample
of manual implementation. In the future, the pattern may
change since source generator will take care of some of
the manual work.

## Disclaimer

Since this project is still in ALPHA state, bugs may
(and is likely) occur. Please report the bugs via
[Developer Sanctuary](https://dsc.gg/devsanx) Discord
server `#gtudios` channel (prefered) or via the Issues tab.

## Lexer

Lexer takes in an input stream of characters, and output
`IEnumerable<IToken>`. After a sequence of character is
found, it can call user defined function to provide custom
value for that token, which the lexer will emit as `IToken<T>`.
Lexer also has some APIs such as `GoTo(state)` and
`Reverse(characters)` to allow some custom parsing.

See Get.Lexer.Test for example. The recommended approach
is to use the source generator (see `CustomLexerSourceGen.cs`),
although manual approach is also possible (see `CustomLexer.cs`).

Known Issue: Some complex regexes are not correctly parsed.
We are currently implementing a fix for that.

## LR(1) parser

The LR(1) parser takes in the list of context free grammar
definition, and a list of precedence. Grammar Definition
Rules can contain user-defined functions that can generate
the value associated with the given nonterminal. The list
of precedence can be used to resolve some shift-reduce conflicts.

The parser currently will error on ambiguous grammar with
shift-reduce conflict or reduce-reduce conflict rather
than having predefined conflict resolvation.

See Get.Parser.Test for example. The recommended approach
is to use the source generator (see `TestSourceGenMath.cs`),
although manual approach is also possible (see `TestRegex.DFA.cs`
or `TestManualRuleAttr.DFA.cs` for example).

Note that the source generator for parser is still not mature.
It may not report all errors yet.