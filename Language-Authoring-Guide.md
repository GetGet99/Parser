# Authoring a language with Get.Lexer and Get.Parser

This guide explains how to use **Get.Lexer** (tokenization) and **Get.Parser** (LR(1) parsing) together to define a small language or DSL in C#. It builds on the overview in [Readme.md](Readme.md) and points to examples **next to this file** (under the Parser source tree).

> **Paths** below are relative to the **root of the Parser source tree** — the folder that directly contains `Get.Lexer`, `Get.Parser`, `Get.Lexer.Test`, and so on. That may be the root of a standalone clone, **or** a submodule or nested subfolder inside a larger git repository. For example, if the parent repo is `QuickMarkup` and `Parser` is a submodule, paths like `Get.Lexer.Test/...` are relative to `QuickMarkup/Parser/` (not the parent repo root).

> **Beta.** The framework is in beta; expect rough edges. Report issues through your project’s issue tracker.

## What you are building

A typical pipeline looks like this:

1. **Source text** → `ITextSeekable` (character stream with position).
2. **Lexer** → `IEnumerable<IToken<TTerminal>>` (classified lexemes, sometimes with payloads like `int` or `string`).
3. **Adapter** (often a small loop) → `IEnumerable<ITerminalValue>` using `ParserBase.CreateValue(...)` so token types and spans match what the parser expects.
4. **Parser** → a single value of type `TOut` (your AST root, an interpreter result, etc.).

The **recommended** path is **attributes + source generators**: you get IntelliSense and **Roslyn analyzers** that flag lexer conflicts and LR shift/reduce or reduce/reduce problems while you edit. A **manual** lexer (building DFAs in code) still works with the parser but **does not** get lexer analyzers.

---

## Part 1 — Lexer (Get.Lexer)

### Core pieces

| Piece | Role |
|--------|------|
| `LexerBase<TState, TTokenEnum>` | Runtime driver: regex DFAs per state, `GetTokens()`, `Make` / `YieldToken`, `GoTo`, `Reverse`. |
| `[Lexer<TTokenEnum>]` on a `partial` class | Tells the source generator to emit DFA code from your token enum. |
| Token **enum** | One field per token kind; `[Regex(...)]` (and variants) attach patterns. |
| `TState` enum | Lexer modes (e.g. “inside string”, “default”). Each state has its own ordered set of rules. |

Minimal lexer example:

```csharp
[Lexer<Terminals>]
partial class CustomLexerSourceGen(ITextSeekable text) : LexerBase<State, Terminals>(text, State.Initial)
{
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
        [Regex(@"\+")] Plus,
        // ...
    }
}
```

Implement `BuildInt`, `BuildString`, etc. as `partial` methods returning the typed payload; the generator wires them to `Regex<T>`.

### Regex attributes

- **`[Regex("pattern")]`** — match text; optional handler name for side effects or custom token return.
- **`[Regex<T>("pattern", "MethodName")]`** — the method produces the token’s **value** (e.g. parsed `int`). Use with **`[Type<T>]`** on the same enum field so the parser/lexer tooling knows the payload type.
- **`State = (int)YourState.SomeMode`** — rule applies only in that lexer state.
- **`Order`** — disambiguation when multiple rules could match (higher priority wins where applicable).
- **`ShouldReturnToken = false`** — consume input but emit no token (whitespace, comments, or transitions).

### Lexer state machines

For nested or contextual syntax (tags, strings, nested comments), define multiple `State` values and switch with **`GoTo(state)`** from handler methods, optionally with stacks if you need to restore a previous mode. The small test lexers here use a single `State` (see [Get.Lexer.Test/CustomLexerSourceGen.cs](Get.Lexer.Test/CustomLexerSourceGen.cs)); add more states as your grammar needs. API surface: `Get.Lexer/LexerBase.cs`.

Other useful APIs on `LexerBase` (see `Get.Lexer/LexerBase.cs`):

- **`Reverse(n)`** — put characters back (e.g. lookahead).
- **`YieldToken(...)`** — enqueue extra tokens before the current match’s token.
- **`MatchedText` / `MatchedStartPosition` / `MatchedEndPosition`** — for building values.

### Conflict checking

Put **`[CompileTimeConflictCheck]`** on the token enum to ask the analyzer to detect ambiguous/overlapping rules in attribute-based lexers.

---

## Part 2 — Parser (Get.Parser)

### Core pieces

| Piece | Role |
|--------|------|
| `ParserBase<Terminal, NonTerminal, TOut>` | Generated LR parser entry: `Parse(IEnumerable<ITerminalValue?> ...)`. |
| `[Parser(startNonTerminal)]` on a `partial` class | Start symbol is an enum field of `NonTerminal`. |
| **Terminal enum** | Token kinds the grammar sees (must align with what you emit as `ITerminalValue`). |
| **NonTerminal enum** | Grammar nonterminals; each symbol that produces a value uses **`[Type<T>]`** and one or more **`[Rule(...)]`**. |

### Rule shape

Rules are declared on **non-terminal enum fields** via `[Rule(...)]`. Conceptually:

```text
Rule = sequence of symbols + optional WITHPARAM + reduce action
```

- **Symbols** — `Terminal` or `NonTerminal` enum values.
- **`AS` + `"paramName"`** — bind a child’s semantic value to a parameter of the reduce method or constructor.
- **`WITHPARAM` + `"name"` + constant** — pass a fixed value (e.g. `false`, `typeof(MyAstNode)`).
- **Reduce action** — `nameof(StaticMethod)` or `typeof(AstType)` for constructor reduction.

Example from the math test ([Get.Parser.Test/TestSourceGenMath.cs](Get.Parser.Test/TestSourceGenMath.cs)): expression grammar with left-associative `+`/`-` and higher precedence `*`/`/`:

```csharp
[Parser(StartNode)]
[Precedence(
    Times, Divide, Associativity.Left,
    Plus, Minus, Associativity.Left
)]
partial class TestSourceGenMath : ParserBase<Terminal, NonTerminal, decimal>
{
    public enum Terminal
    {
        Plus, Minus, Times, Divide,
        [Type<decimal>] Number,
        OpenBracket, CloseBracket, AbsoluteValueBar
    }

    public enum NonTerminal
    {
        [Type<decimal>]
        [Rule(Expr, AS, "val", nameof(Identity))]
        StartNode,

        [Type<decimal>]
        [Rule(Expr, AS, "x", Plus, Expr, AS, "y", nameof(AddImpl))]
        [Rule(Expr, AS, "x", Times, Expr, AS, "y", nameof(MultiplyImpl))]
        // ...
        [Rule(Number, AS, "val", nameof(Identity))]
        Expr,
    }
}
```

`Precedence` lists **lower precedence first**, then higher (each group is `terminals..., Associativity.Left|Right|NonAssociative`). This resolves shift/reduce conflicts for expression grammars. A minimal illustration is in [Get.Parser.Test/ShiftReduceConflict.cs](Get.Parser.Test/ShiftReduceConflict.cs): without precedence, `a + b` style grammars conflict; adding `[Precedence(Plus, Associativity.Left)]` fixes it.

### Built-in list and helper “keywords”

On `ParserBase`, these map to special reduce behavior (see doc comments in `Get.Parser.Shared/ParserBase.cs`):

| Keyword | Meaning |
|---------|--------|
| `EMPTYLIST` | Reduce to an empty list. |
| `SINGLELIST` | Wrap one value in a list. |
| `APPENDLIST` | Append to a list (with `LIST` / `VALUE` roles). |
| `IDENTITY` | Pass one value through. |
| `ERROR` | Error-recovery placeholder (advanced). |

Use these to build list-shaped AST nodes without handwritten glue for every cons step.

### Typing terminals

- **With Get.Lexer**: set **`[Parser(start, UseGetLexerTypeInformation = true)]`** so terminal types come from the lexer’s `[Type<T>]` / `Regex<T>` metadata.
- **Without** (external lexer): leave the default `false` and put **`[Type<T>]`** on terminal enum fields in the parser.

### Parsing API

`ParserBase` exposes:

```csharp
public TOut Parse(
    IEnumerable<ITerminalValue?> inputTerminals,
    bool debug = false,
    List<ErrorTerminalValue>? handledErrors = null,
    bool skipErrorHandling = true)
```

Use **`CreateValue(Terminal, ...)`** helpers to build `ITerminalValue` / typed terminal values from your lexer output. The **`GetTerminals`** helper in [Get.Parser.Test/TestSourceGenMath.cs](Get.Parser.Test/TestSourceGenMath.cs) walks lexer tokens and maps them to `CreateValue` for the parser (including typed `Number` from the manual `MathTextLexer`).

---

## Part 3 — End-to-end: designing a small language

### 1. Sketch the semantics and AST

Decide what values each construct produces (expressions, statements, modules). Use plain C# records/classes for AST nodes.

### 2. Design the token alphabet

List keywords, literals, operators, and punctuation. Each becomes a **terminal** in the parser and (usually) a **lexer** enum member.

Keep lexer **longer operators** ordered before shorter prefixes where the regex engine needs disambiguation (e.g. `=>` before `=`).

### 3. Implement the lexer

- Start with one `State` if possible; add states only when context-free regexes are insufficient.
- Mark literals with `Regex<T>` + `Type<T>` when the AST needs typed values.
- Skip whitespace and comments with `ShouldReturnToken = false`.

### 4. Implement the parser

- Choose a **start** non-terminal.
- For each grammar production, add a `[Rule(...)]` on the corresponding non-terminal field.
- Introduce precedence and associativity for binary operators and dangling-else style issues **before** debugging large grammars.
- Use `EMPTYLIST` / `APPENDLIST` / `SINGLELIST` for sequences.

### 5. Connect lexer to parser

Map each emitted `IToken` to `CreateValue(terminal, ...)` so ordering and types match the parser’s `Terminal` enum. If token types differ between lexer and parser, this layer is the single place to translate.

### 6. Iterate with analyzers

Fix lexer conflicts (overlapping patterns, wrong order) and parser conflicts (precedence, factoring). The IDE should surface them in attribute-based mode.

---

## Reference examples in this repository

| Example | Location | What it shows |
|---------|----------|----------------|
| Minimal generated lexer | [Get.Lexer.Test/CustomLexerSourceGen.cs](Get.Lexer.Test/CustomLexerSourceGen.cs) | `[Lexer<>]`, `Regex<T>`, `CompileTimeConflictCheck` |
| Parser + precedence + manual lexer | [Get.Parser.Test/TestSourceGenMath.cs](Get.Parser.Test/TestSourceGenMath.cs) | Expression grammar, `[Precedence]`, `CreateValue` bridge, nested `MathTextLexer` |
| Shift/reduce fix | [Get.Parser.Test/ShiftReduceConflict.cs](Get.Parser.Test/ShiftReduceConflict.cs) | Minimal precedence usage |
| Manual lexer (no attributes) | [Get.Lexer.Test/CustomLexer.cs](Get.Lexer.Test/CustomLexer.cs) | Programmatic DFA rules (no lexer analyzer diagnostics) |
| Larger manual parser tests | [Get.Parser.Test/TestRegex.DFA.cs](Get.Parser.Test/TestRegex.DFA.cs), [Get.Parser.Test/MathTests.cs](Get.Parser.Test/MathTests.cs) | Additional parser/lexer experiments in-tree |

For **`[Parser(..., UseGetLexerTypeInformation = true)]`** (terminal types inferred from the lexer’s `[Type<T>]` metadata), see the XML docs on `ParserAttribute` in `Get.Parser.Shared/Attributes.cs`; there is no separate sample in this repository beyond that API.

---

## Further reading

- [Readme.md](Readme.md) — features, analyzer behavior, beta disclaimer.
- XML docs on **`RuleAttribute`**, **`ParserAttribute`**, **`PrecedenceAttribute`** in `Get.Parser.Shared/Attributes.cs`.
- **`LexerTokensAttribute` / `RegexAttribute`** in `Get.Lexer.Shared/LexerTokensAttribute.cs`.
- **`ParserBase`** and **`LexerBase`** for runtime behavior and helper keywords.
