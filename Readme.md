# Get's Parser and Lexer

A general lexer and LR(1) parser implementation in C#. It has not been tested througly yet. This is intended to be used behind a source gen, that is not incomplete yet. For now, all the work must be done manually.

See Get.Parser.Test and Get.Lexer.Test for manual implementation. In the future, the pattern may change since source generator will take care of some of it.

## Warning

The implementation is currently incomplete. We do not guarantee anything. Things may be unstable. Please do not use this in production.

## Lexer

Lexer takes in an input stream of characters, and output `IEnumerable<IToken>`. After a sequence of character is found, it can call user defined function to provide custom value for that token, which the lexer will emit as `IToken<T>`. Lexer also has some APIs such as `GoTo(state)` and `Reverse(characters)` to allow some custom parsing.

See Get.Lexer.Test for example

## LR(1) parser

The LR(1) parser takes in the list of context free grammar definition, and a list of precedence. Grammar Definition Rules can contain user-defined functions that can generate the value associated with the given nonterminal. The list of precedence can be used to resolve some shift-reduce conflicts.

The parser currently will error on ambiguous grammar with shift-reduce conflict or reduce-reduce conflict rather than having predefined conflict resolvation.

See Get.Parser.Test for example