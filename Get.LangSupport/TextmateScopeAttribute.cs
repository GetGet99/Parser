using System.Diagnostics.CodeAnalysis;

namespace Get.LangSupport;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public class TextmateScopeAttribute(string scope) : Attribute
{
    public string Scope { get; } = scope;
    public string Key { get; set; } = scope.Split('.').Last();
    public int Priority { get; set; } = 0; // default lowest priority
    public bool AddBoundary { get; set; } = false;
    /// <summary>
    /// Repository key for grouping rules (e.g. "main", "comments", "strings").
    /// </summary>
    public string RepositoryKey { get; set; } = "main";
    /// <summary>
    /// When true (default), only one TextMate rule is emitted per unique regex on this field.
    /// </summary>
    public bool DeduplicateRegexes { get; set; } = true;
    /// <summary>
    /// If this is null, it is inherited from ALL of the [Regex] attributes. Otherwise,
    /// we will use these ones.
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string[]? Regexes { get; set; }
    /// <summary>
    /// Optional: Begin regex for multi-line constructs.
    /// </summary>
    /// <remarks>
    /// Ignored unless <see cref="Begin"/> and <see cref="End"/> is set.
    /// </remarks>
    public string? Begin { get; set; }

    /// <summary>
    /// Optional: End regex for multi-line constructs.
    /// </summary>
    /// <remarks>
    /// Ignored unless <see cref="Begin"/> and <see cref="End"/> is set.
    /// </remarks>
    public string? End { get; set; }

    /// <summary>
    /// Optional: Patterns inside begin-end, like content.
    /// </summary>
    /// <remarks>
    /// Ignored unless <see cref="Begin"/> and <see cref="End"/> is set.
    /// </remarks>
    public string[]? InsideIncludes { get; set; }

    /// <summary>
    /// VS Code embedded language id (emitted as <c>meta.embedded.{id}</c>).
    /// </summary>
    public string? EmbeddedLanguage { get; set; }

    /// <summary>
    /// TextMate grammar to include for embedded content (e.g. <c>source.cs</c>).
    /// When null, defaults to <c>source.{<see cref="EmbeddedLanguage"/>}</c>.
    /// </summary>
    public string? EmbeddedGrammarScope { get; set; }

    /// <summary>
    /// Scope applied to content inside begin/end when not using <see cref="EmbeddedLanguage"/>.
    /// </summary>
    public string? ContentScope { get; set; }

    /// <summary>
    /// Capture group index → TextMate scope name for begin rules.
    /// </summary>
    public Dictionary<string, string>? BeginCaptures { get; set; }

    /// <summary>
    /// Capture group index → TextMate scope name for end rules.
    /// </summary>
    public Dictionary<string, string>? EndCaptures { get; set; }

    /// <summary>
    /// Capture group index → TextMate scope name for match rules.
    /// </summary>
    public Dictionary<string, string>? MatchCaptures { get; set; }
}

/// <summary>Categories for <c>keyword.*</c> TextMate scopes.</summary>
public enum KeywordType
{
    /// <summary>Control flow keywords (if, else, while, for, etc.).</summary>
    Control,
    /// <summary>Modifier keywords (public, private, static, etc.).</summary>
    Modifier,
    /// <summary>Declaration keywords (class, struct, enum, etc.).</summary>
    Declaration,
    /// <summary>Import/include keywords (using, import, etc.).</summary>
    Import,
    /// <summary>Other keyword scopes.</summary>
    Other
}

/// <summary>Categories for <c>constant.*</c> TextMate scopes.</summary>
public enum ConstantType
{
    /// <summary>Numeric literals.</summary>
    Numeric,
    /// <summary>Boolean literals (true, false).</summary>
    Boolean,
    /// <summary>Character literals.</summary>
    Character,
    /// <summary>Other constant scopes.</summary>
    Other
}

/// <summary>Categories for <c>string.*</c> TextMate scopes.</summary>
public enum StringType
{
    /// <summary>Quoted string literals.</summary>
    Quoted,
    /// <summary>Interpolated strings.</summary>
    Interpolated,
    /// <summary>Other string scopes.</summary>
    Other
}
/// <summary>Categories for <c>string.quoted.*</c> TextMate scopes.</summary>
public enum StringQuotedType
{
    /// <summary>Single-quoted strings.</summary>
    Single,
    /// <summary>Double-quoted strings.</summary>
    Double,
    /// <summary>Triple-quoted strings.</summary>
    Triple,
    /// <summary>Interpolated quoted strings.</summary>
    Interpolated,
    /// <summary>Unquoted string-like tokens.</summary>
    Unquoted,
    /// <summary>Other quoted string scopes.</summary>
    Other
}


/// <summary>Categories for <c>variable.*</c> TextMate scopes.</summary>
public enum VariableType
{
    /// <summary>Read-only variables (const, val).</summary>
    ReadOnly,
    /// <summary>Read-write variables (var, let).</summary>
    ReadWrite,
    /// <summary>Function/method parameters.</summary>
    Parameter,
    /// <summary>Other variable scopes.</summary>
    Other
}

/// <summary>Categories for <c>entity.name.function.*</c> TextMate scopes.</summary>
public enum EntityFunctionType
{
    /// <summary>Regular method/function definitions.</summary>
    Method,
    /// <summary>Constructor declarations.</summary>
    Constructor,
    /// <summary>Destructor declarations.</summary>
    Destructor,
    /// <summary>Other function scopes.</summary>
    Other
}

/// <summary>Categories for <c>entity.name.type.*</c> TextMate scopes.</summary>
public enum EntityTypeType
{
    /// <summary>Class declarations.</summary>
    Class,
    /// <summary>Interface declarations.</summary>
    Interface,
    /// <summary>Enum declarations.</summary>
    Enum,
    /// <summary>Struct declarations.</summary>
    Struct,
    /// <summary>Other type scopes.</summary>
    Other
}
/// <summary>Categories for <c>constant.language.*</c> TextMate scopes (language built-in constants).</summary>
public enum ConstantLanguageType
{
    Boolean, Null, Undefined, This, Super, Self, Infinity, NaN, Default, Arguments, Global, Window,
    /// <summary>For <c>__FILE__</c>-like constants.</summary>
    File,
    /// <summary>For <c>__LINE__</c>-like constants.</summary>
    Line,
}

/// <summary>Applies a <c>keyword.&lt;name&gt;</c> TextMate scope. Maps from <see cref="KeywordType"/>.</summary>
public class TextmateKeywordScopeAttribute : TextmateScopeAttribute
{
    public TextmateKeywordScopeAttribute(string name) : base($"keyword.{name}") { AddBoundary = true; }
    public TextmateKeywordScopeAttribute(KeywordType type) : this(type.ToString().ToLower()) { }
}

/// <summary>Applies a <c>constant.&lt;name&gt;</c> TextMate scope. Maps from <see cref="ConstantType"/>.</summary>
public class TextmateConstantScopeAttribute : TextmateScopeAttribute
{
    public TextmateConstantScopeAttribute(string name) : base($"constant.{name}") { AddBoundary = true; }
    public TextmateConstantScopeAttribute(ConstantType type) : this(type.ToString().ToLower()) { }
}

/// <summary>Applies a <c>constant.language.&lt;name&gt;</c> scope. Maps from <see cref="ConstantLanguageType"/>.</summary>
public class TextmateConstantLanguageScopeAttribute : TextmateScopeAttribute
{
    public TextmateConstantLanguageScopeAttribute(string name) : base($"constant.language.{name}") { }
    public TextmateConstantLanguageScopeAttribute(ConstantLanguageType type) : this(type.ToString().ToLower()) { }
}

/// <summary>Applies a <c>string.&lt;name&gt;</c> TextMate scope. Maps from <see cref="StringType"/>.</summary>
public class TextmateStringScopeAttribute : TextmateScopeAttribute
{
    public TextmateStringScopeAttribute(string name) : base($"string.{name}") { }
    public TextmateStringScopeAttribute(StringType type) : this(type.ToString().ToLower()) { }
}

/// <summary>Applies a <c>string.quoted.&lt;name&gt;</c> scope. Maps from <see cref="StringQuotedType"/>.</summary>
public class TextmateStringQuotedScopeAttribute : TextmateScopeAttribute
{
    public TextmateStringQuotedScopeAttribute(string name)
        : base($"string.quoted.{name}") { }

    public TextmateStringQuotedScopeAttribute(StringQuotedType type)
        : this(type.ToString().ToLower()) { }
}


/// <summary>Applies a <c>comment</c> TextMate scope.</summary>
public class TextmateCommentScopeAttribute : TextmateScopeAttribute
{
    public TextmateCommentScopeAttribute() : base("comment") { }
}

/// <summary>Applies a <c>variable.other.&lt;name&gt;</c> scope. Maps from <see cref="VariableType"/>.</summary>
public class TextmateOtherVariableScopeAttribute : TextmateScopeAttribute
{
    public TextmateOtherVariableScopeAttribute(string name = "readwrite") : base($"variable.other.{name}") { }
    public TextmateOtherVariableScopeAttribute(VariableType type) : this(type.ToString().ToLower()) { }
}

/// <summary>Categories for <c>keyword.operator.*</c> TextMate scopes.</summary>
public enum OperatorType
{
    /// <summary>Arithmetic operators (+, -, *, /, %).</summary>
    Arithmetic,
    /// <summary>Assignment operators (=, +=, -=, etc.).</summary>
    Assignment,
    /// <summary>Comparison operators (==, !=, &lt;, &gt;, etc.).</summary>
    Comparison,
    /// <summary>Logical operators (&amp;&amp;, ||, !).</summary>
    Logical,
    /// <summary>Increment/decrement operators (++, --).</summary>
    Increment,
    /// <summary>Decrement operators (--).</summary>
    Decrement,
    /// <summary>Ternary operator (? :).</summary>
    Ternary,
    /// <summary>TypeOf/type-checking operators.</summary>
    TypeOf,
    /// <summary>Other operator scopes.</summary>
    Other
}

/// <summary>Applies a <c>keyword.operator.&lt;type&gt;</c> scope. Maps from <see cref="OperatorType"/>.</summary>
public class TextmateKeywordOperatorScopeAttribute : TextmateScopeAttribute
{
    public TextmateKeywordOperatorScopeAttribute(string opType = "arithmetic") : base($"keyword.operator.{opType}") { }
    public TextmateKeywordOperatorScopeAttribute(OperatorType opType) : this(opType.ToString().ToLower()) { }
}

/// <summary>Applies an <c>entity.name.function.&lt;name&gt;</c> scope. Maps from <see cref="EntityFunctionType"/>.</summary>
public class TextmateEntityNameFunctionScopeAttribute : TextmateScopeAttribute
{
    public TextmateEntityNameFunctionScopeAttribute(string name) : base($"entity.name.function.{name}") { }
    public TextmateEntityNameFunctionScopeAttribute(EntityFunctionType type) : this(type.ToString().ToLower()) { }
}

/// <summary>Applies an <c>entity.name.type.&lt;name&gt;</c> scope. Maps from <see cref="EntityTypeType"/>.</summary>
public class TextmateEntityNameTypeScopeAttribute : TextmateScopeAttribute
{
    public TextmateEntityNameTypeScopeAttribute(string name) : base($"entity.name.type.{name}") { }
    public TextmateEntityNameTypeScopeAttribute(EntityTypeType type) : this(type.ToString().ToLower()) { }
}

/// <summary>Applies a <c>storage.type</c> TextMate scope.</summary>
public class TextmateStorageTypeScopeAttribute : TextmateScopeAttribute
{
    public TextmateStorageTypeScopeAttribute() : base("storage.type") { }
}
/// <summary>Categories for <c>punctuation.*</c> TextMate scopes.</summary>
public enum PunctuationType
{
    /// <summary>Accessor/dot punctuation (<c>.</c>).</summary>
    Accessor,
    /// <summary>Definition punctuation (<c>=</c>).</summary>
    Definition,
    /// <summary>Terminator punctuation (<c>;</c>).</summary>
    Terminator,
    /// <summary>Bracket punctuation (<c>{ } ( ) [ ]</c>).</summary>
    Bracket,
    /// <summary>String-related punctuation (<c>" '</c>).</summary>
    String,
    /// <summary>Punctuation for embedding other languages (<c>{{ }}</c>).</summary>
    Embedded,
    /// <summary>Section punctuation (e.g., Markdown headers).</summary>
    Section,
    /// <summary>Whitespace punctuation (rarely scoped).</summary>
    Whitespace,
    /// <summary>Other punctuation scopes.</summary>
    Other
}
/// <summary>Categories for <c>punctuation.separator.*</c> TextMate scopes.</summary>
public enum PunctuationSeparatorType
{
    /// <summary>Comma separator (<c>,</c>).</summary>
    Comma,
    /// <summary>Colon separator (<c>:</c>).</summary>
    Colon,
    /// <summary>Semicolon separator (<c>;</c>).</summary>
    Semicolon,
    /// <summary>Pipe separator (<c>|</c>).</summary>
    Pipe,
    /// <summary>Dot separator (<c>.</c>).</summary>
    Dot,
    /// <summary>Other separator scopes.</summary>
    Other
}


/// <summary>Applies a <c>punctuation.&lt;name&gt;</c> scope. Maps from <see cref="PunctuationType"/>.</summary>
public class TextmatePunctuationScopeAttribute : TextmateScopeAttribute
{
    public TextmatePunctuationScopeAttribute(string name) : base($"punctuation.{name}") { }
    public TextmatePunctuationScopeAttribute(PunctuationType type) : this(type.ToString().ToLower()) { }
}
/// <summary>Applies a <c>punctuation.separator.&lt;name&gt;</c> scope. Maps from <see cref="PunctuationSeparatorType"/>.</summary>
public class TextmatePunctuationSeparatorScopeAttribute : TextmateScopeAttribute
{
    public TextmatePunctuationSeparatorScopeAttribute(string name)
        : base($"punctuation.separator.{name}") { }

    public TextmatePunctuationSeparatorScopeAttribute(PunctuationSeparatorType type)
        : this(type.ToString().ToLower()) { }
}

/// <summary>Categories for <c>constant.numeric.*</c> TextMate scopes.</summary>
public enum NumericType
{
    /// <summary>Plain decimal numeric literal.</summary>
    Default,
    /// <summary>Decimal numeric literal.</summary>
    Decimal,
    /// <summary>Hexadecimal numeric literal.</summary>
    Hex,
    /// <summary>Octal numeric literal.</summary>
    Octal,
    /// <summary>Binary numeric literal.</summary>
    Binary,
    /// <summary>Floating-point numeric literal.</summary>
    Float,
    /// <summary>Other numeric scopes.</summary>
    Other
}

/// <summary>Applies a <c>constant.numeric[.&lt;type&gt;]</c> scope. Maps from <see cref="NumericType"/>.</summary>
public class TextmateConstantNumericScopeAttribute : TextmateScopeAttribute
{
    public TextmateConstantNumericScopeAttribute(string name = "")
        : base(string.IsNullOrEmpty(name) ? "constant.numeric" : $"constant.numeric.{name}")
    {
    }

    public TextmateConstantNumericScopeAttribute(NumericType type)
        : this(type == NumericType.Default ? "" : type.ToString().ToLower())
    {
    }
}
