#pragma warning disable CS9113 // Parameter is unread. Read by source generator
namespace Get.Parser;

/// <summary>Keywords used by the parser source generator to encode grammar rule semantics.</summary>
public enum ParserSourceGeneratorKeywords : byte
{
    /// <summary>Cast a value to a different type.</summary>
    As,
    /// <summary>Pass a parameter to a function call.</summary>
    WithParam,
    /// <summary>Assign precedence to a rule.</summary>
    WithPrecedence,
    /// <summary>Invoke a function to produce the value.</summary>
    FuncCall,
    /// <summary>Create an empty list.</summary>
    EmptyList,
    /// <summary>Create a singleton list from a value.</summary>
    SingleList,
    /// <summary>Append a value to a list.</summary>
    AppendList,
    /// <summary>Return the value as-is (identity).</summary>
    Identity,
    /// <summary>A list parameter.</summary>
    List,
    /// <summary>A value parameter.</summary>
    Value,
    /// <summary>The error terminal marker.</summary>
    Error
}
