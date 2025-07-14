using System.Diagnostics.CodeAnalysis;

namespace Get.Lexer;
/// <summary>
/// Make the given class to become a lexer, with tokens and rules defined in the enum.
/// </summary>
/// <typeparam name="TLexerTokens">The enum that defines all the token types</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LexerAttribute<TLexerTokens> : LexerAttributeBase where TLexerTokens : Enum;
/// <summary>
/// Make the given class to become a lexer, with tokens and rules defined in the enum.
/// </summary>
/// <typeparam name="TLexerTokens">The enum that defines all the token types</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LexerTokensAttribute<TLexerTokens> : LexerAttributeBase where TLexerTokens : class;
public abstract class LexerAttributeBase : Attribute;
/// <summary>
/// In the enum field, provides a regex to match
/// </summary>
/// <param name="regex">The regular expression to match</param>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class RegexAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string Regex, string? RawImplementationMethodName = null) : Attribute
{
    public int Order { get; set; } = 0;
    public int State { get; set; } = 0;
    /// <summary>
    /// Whether the regex should output a token.<br/><br/>
    /// For <see cref="RegexAttribute"/> manual implementation (ie. there is an implementation method),<br/>
    /// if <see cref="ShouldReturnToken"/> is true, the method should return the token, otherwise, the method should return void.<br/>
    /// Note that this does not prevent the method from using
    /// <see cref="LexerBase.YieldToken(PLShared.IToken{TTokenEnum})"/> or
    /// <see cref="LexerBase.YieldToken(PLShared.IToken)"/>.<br/><br/>
    /// For <see cref="RegexAttribute"/> auto implementation, if <see cref="ShouldReturnToken"/> is true,
    /// a token of the attached enum value without value is returned, otherwise, no token is returned.
    /// 
    /// <br/><br/>
    /// For <see cref="RegexAttribute{T}"/>, <see cref="ShouldReturnToken"/> must be true, and the implementation method will be called.
    /// </summary>
    public bool ShouldReturnToken { get; set; } = true;
    public string InputRegex { get; } = Regex;
}
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class StringAttribute(string Exact) : Attribute;
public class RegexAttribute<T>([StringSyntax(StringSyntaxAttribute.Regex)] string Regex, string ImplementationMethodName) : RegexAttribute(Regex, "AUTO-GENERATED");
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class TypeAttribute<T> : Attribute;
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
public class CompileTimeConflictCheckAttribute : Attribute;