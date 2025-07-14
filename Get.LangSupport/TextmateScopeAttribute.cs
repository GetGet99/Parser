namespace Get.LangSupport;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public class TextmateScopeAttribute(string scope) : Attribute
{
    public string Scope { get; } = scope;
    public string Key { get; set; } = scope.Split('.').Last();
    public int Priority { get; set; } = 0; // default lowest priority
    /// <summary>
    /// If this is null, it is inherited from ALL of the [Regex] attributes. Otherwise,
    /// we will use these ones.
    /// </summary>
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

public class KeywordScopeAttribute : TextmateScopeAttribute
{
    public KeywordScopeAttribute(string name) : base($"keyword.{name}") { }
    public KeywordScopeAttribute(KeywordType type) : this(type.ToString().ToLower()) { }
}

public class ConstantScopeAttribute : TextmateScopeAttribute
{
    public ConstantScopeAttribute(string name) : base($"constant.{name}") { }
    public ConstantScopeAttribute(ConstantType type) : this(type.ToString().ToLower()) { }
}

public class StringScopeAttribute : TextmateScopeAttribute
{
    public StringScopeAttribute(string name) : base($"string.{name}") { }
    public StringScopeAttribute(StringType type) : this(type.ToString().ToLower()) { }
}

public class CommentScopeAttribute : TextmateScopeAttribute
{
    public CommentScopeAttribute() : base("comment") { }
}

public class VariableScopeAttribute : TextmateScopeAttribute
{
    public VariableScopeAttribute(string name = "other.readwrite") : base($"variable.{name}") { }
    public VariableScopeAttribute(VariableType type) : this(type.ToString().ToLower()) { }
}

public enum OperatorType
{
    Arithmetic,
    Assignment,
    Comparison,
    Logical,
    Increment,
    Ternary,
    Other
}

public class OperatorScopeAttribute : TextmateScopeAttribute
{
    public OperatorScopeAttribute(string opType = "arithmetic") : base($"keyword.operator.{opType}") { }
    public OperatorScopeAttribute(OperatorType opType) : this(opType.ToString().ToLower()) { }
}

public class EntityNameFunctionScopeAttribute : TextmateScopeAttribute
{
    public EntityNameFunctionScopeAttribute(string name) : base($"entity.name.function.{name}") { }
    public EntityNameFunctionScopeAttribute(EntityFunctionType type) : this(type.ToString().ToLower()) { }
}

public class EntityNameTypeScopeAttribute : TextmateScopeAttribute
{
    public EntityNameTypeScopeAttribute(string name) : base($"entity.name.type.{name}") { }
    public EntityNameTypeScopeAttribute(EntityTypeType type) : this(type.ToString().ToLower()) { }
}

public class StorageTypeScopeAttribute : TextmateScopeAttribute
{
    public StorageTypeScopeAttribute() : base("storage.type") { }
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

public class NumericScopeAttribute : TextmateScopeAttribute
{
    public NumericScopeAttribute(string name = "")
        : base(string.IsNullOrEmpty(name) ? "constant.numeric" : $"constant.numeric.{name}")
    {
    }

    public NumericScopeAttribute(NumericType type)
        : this(type == NumericType.Default ? "" : type.ToString().ToLower())
    {
    }
}
