using System.Diagnostics.CodeAnalysis;

namespace Get.Lexer;
/// <summary>
/// Make the given class to become a lexer, with tokens and rules defined in the enum.
/// </summary>
/// <typeparam name="TLexerTokens">The enum that defines all the token types</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LexerAttribute<TLexerTokens> : LexerAttributeBase where TLexerTokens : Enum;
public abstract class LexerAttributeBase : Attribute;
/// <summary>
/// In the enum field, provides a regex to match
/// </summary>
/// <param name="regex">The regular expression to match</param>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
class RegexAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string regex) : Attribute
{
    public int Order { get; set; } = 0;
    public int State { get; set; } = 0;
}
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
class StringAttribute(string exact) : Attribute;
class RegexAttribute<T>([StringSyntax(StringSyntaxAttribute.Regex)] string regex, string implementationMethodName) : RegexAttribute(regex);
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
class TypeAttribute<T> : Attribute;
