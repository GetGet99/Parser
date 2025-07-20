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
}
public enum KeywordType
{
    Control,
    Modifier,
    Declaration,
    Import,
    Other
}

public enum ConstantType
{
    Numeric,
    Boolean,
    Character,
    Other
}

public enum StringType
{
    Quoted,
    Interpolated,
    Other
}
public enum StringQuotedType
{
    Single,
    Double,
    Triple,
    Interpolated,
    Unquoted,
    Other
}


public enum VariableType
{
    ReadOnly,
    ReadWrite,
    Parameter,
    Other
}

public enum EntityFunctionType
{
    Method,
    Constructor,
    Destructor,
    Other
}

public enum EntityTypeType
{
    Class,
    Interface,
    Enum,
    Struct,
    Other
}
public enum ConstantLanguageType
{
    Boolean,
    Null,
    Undefined,
    This,
    Super,
    Self,
    Infinity,
    NaN,
    Default,
    Arguments,
    Global,
    Window,
    File,     // For __FILE__
    Line,     // For __LINE__
}

public class TextmateKeywordScopeAttribute : TextmateScopeAttribute
{
    public TextmateKeywordScopeAttribute(string name) : base($"keyword.{name}") { AddBoundary = true; }
    public TextmateKeywordScopeAttribute(KeywordType type) : this(type.ToString().ToLower()) { }
}

public class TextmateConstantScopeAttribute : TextmateScopeAttribute
{
    public TextmateConstantScopeAttribute(string name) : base($"constant.{name}") { AddBoundary = true; }
    public TextmateConstantScopeAttribute(ConstantType type) : this(type.ToString().ToLower()) { }
}

public class TextmateConstantLanguageScopeAttribute : TextmateScopeAttribute
{
    public TextmateConstantLanguageScopeAttribute(string name) : base($"constant.language.{name}") { }
    public TextmateConstantLanguageScopeAttribute(ConstantLanguageType type) : this(type.ToString().ToLower()) { }
}

public class TextmateStringScopeAttribute : TextmateScopeAttribute
{
    public TextmateStringScopeAttribute(string name) : base($"string.{name}") { }
    public TextmateStringScopeAttribute(StringType type) : this(type.ToString().ToLower()) { }
}

public class TextmateStringQuotedScopeAttribute : TextmateScopeAttribute
{
    public TextmateStringQuotedScopeAttribute(string name)
        : base($"string.quoted.{name}") { }

    public TextmateStringQuotedScopeAttribute(StringQuotedType type)
        : this(type.ToString().ToLower()) { }
}


public class TextmateCommentScopeAttribute : TextmateScopeAttribute
{
    public TextmateCommentScopeAttribute() : base("comment") { }
}

public class TextmateOtherVariableScopeAttribute : TextmateScopeAttribute
{
    public TextmateOtherVariableScopeAttribute(string name = "readwrite") : base($"variable.other.{name}") { }
    public TextmateOtherVariableScopeAttribute(VariableType type) : this(type.ToString().ToLower()) { }
}

public enum OperatorType
{
    Arithmetic,
    Assignment,
    Comparison,
    Logical,
    Increment,
    Decrement,
    Ternary,
    TypeOf,
    Other
}

public class TextmateKeywordOperatorScopeAttribute : TextmateScopeAttribute
{
    public TextmateKeywordOperatorScopeAttribute(string opType = "arithmetic") : base($"keyword.operator.{opType}") { }
    public TextmateKeywordOperatorScopeAttribute(OperatorType opType) : this(opType.ToString().ToLower()) { }
}

public class TextmateEntityNameFunctionScopeAttribute : TextmateScopeAttribute
{
    public TextmateEntityNameFunctionScopeAttribute(string name) : base($"entity.name.function.{name}") { }
    public TextmateEntityNameFunctionScopeAttribute(EntityFunctionType type) : this(type.ToString().ToLower()) { }
}

public class TextmateEntityNameTypeScopeAttribute : TextmateScopeAttribute
{
    public TextmateEntityNameTypeScopeAttribute(string name) : base($"entity.name.type.{name}") { }
    public TextmateEntityNameTypeScopeAttribute(EntityTypeType type) : this(type.ToString().ToLower()) { }
}

public class TextmateStorageTypeScopeAttribute : TextmateScopeAttribute
{
    public TextmateStorageTypeScopeAttribute() : base("storage.type") { }
}
public enum PunctuationType
{
    Accessor,         // `.`
    Definition,       // `=` in variable declarations or function defs
    Terminator,       // `;`
    Bracket,          // `{`, `}`, `(`, `)`, `[`, `]`
    String,           // `"`, `'`, or escapes inside strings
    Embedded,         // Punctuation used to embed other langs (e.g., `{{`, `}}`)
    Section,          // For section headers (rare, e.g., Markdown)
    Whitespace,       // Not commonly scoped but sometimes used
    Other             // Catch-all fallback
}
public enum PunctuationSeparatorType
{
    Comma,        // `,`
    Colon,        // `:`
    Semicolon,    // `;`
    Pipe,         // `|`
    Dot,          // `.` (alternative to Accessor)
    Other
}


public class TextmatePunctuationScopeAttribute : TextmateScopeAttribute
{
    public TextmatePunctuationScopeAttribute(string name) : base($"punctuation.{name}") { }
    public TextmatePunctuationScopeAttribute(PunctuationType type) : this(type.ToString().ToLower()) { }
}
public class TextmatePunctuationSeparatorScopeAttribute : TextmateScopeAttribute
{
    public TextmatePunctuationSeparatorScopeAttribute(string name)
        : base($"punctuation.separator.{name}") { }

    public TextmatePunctuationSeparatorScopeAttribute(PunctuationSeparatorType type)
        : this(type.ToString().ToLower()) { }
}

public enum NumericType
{
    Default,    // plain numeric (decimal)
    Decimal,
    Hex,
    Octal,
    Binary,
    Float,
    Other
}

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
