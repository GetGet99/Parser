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
class RegexAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string Regex) : Attribute
{
    public int Order { get; set; } = 0;
    public int State { get; set; } = 0;
    public bool ShouldOutputToken { get; set; } = true;
}
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
class StringAttribute(string Exact) : Attribute;
class RegexAttribute<T>([StringSyntax(StringSyntaxAttribute.Regex)] string Regex, string ImplementationMethodName) : RegexAttribute(Regex);
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
class TypeAttribute<T> : Attribute;