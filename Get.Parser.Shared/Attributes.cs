#pragma warning disable CS9113 // Parameter is unread. Read by source generator
namespace Get.Parser;
/// <summary>
/// Specifies a reduction rule for a parser.<br/>
/// Parameters are provided in the following format:<br/>
/// <code>
/// Rule = Elements Options ReduceAction
/// Elements = listof(Raw | Raw AS param_name)
/// Raw = Terminal | NonTerminal
/// Options = listof(WITH_PARAM param_name constant)
/// ReduceAction = reduce_method_name | constructor_of_that_type
/// 
/// where Es = listof(E) means Es = [empty rule] | Es (E)
/// </code>
/// Types:<br/>
/// - AS, WITH_PARAM: Keywords enum<br/>
/// - param_name : string<br/>
/// - Terminal : user defined enum<br/>
/// - NonTerminal : another user defined enum, different from Terminal<br/>
/// - constant : Can be values like <c>42</c>, <c>"string"</c>, <c>true</c>, or <c>typeof(SomeType)</c>, or anything that is allowed in C# attribute<br/>
/// - reduce_method_name : string, a name of a method defined in the parser<br/>
/// - constructor_of_that_type : Type (use <c>typeof(Type)</c>)<br/>
/// Notes:<br/>
/// - <c>listof()</c> allows for an empty rule, enabling the omission of <c>Elements</c> or <c>Options</c> for minimal configurations.<br/>
/// - When a reduction occurs, the specified reduce method or constructor will be invoked with the defined parameters.
/// Examples:
/// <code>
/// // A rule with a parameterized reduction method from the output of NonTerminal1.
/// [Rule(NonTerminal.NonTerminal1, Keywords.AS, "param1", Terminal.Terminal1, "ReduceMethod")]
/// // A rule with a parameterized reduction method from the output of NonTerminal1 and a constant parameter.
/// [Rule(NonTerminal.NonTerminal1, Keywords.AS, "param1", Terminal.Terminal1, Keywords.WITH_PARAM, "param2", 42, "ReduceMethod")]
/// </code>
/// </summary>
/// <typeparam name="T">The output type of </typeparam>
/// <param name="ReduceRuleDefinition">
/// Parameters in the following format:<br/>
/// <code>
/// Rule = Elements Options ReduceAction
/// Elements = listof(Raw | Raw AS param_name)
/// Options = listof(WITH_PARAM param_name constant)
/// ReduceAction = reduce_method_name | constructor_of_that_type
/// </code>
/// See <see cref="RuleAttribute"/> for detailed examples and type definitions.
/// </param>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class RuleAttribute(params object?[] ReduceRuleDefinition) : Attribute;
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class TypeAttribute<T> : Attribute;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ParserAttribute(object startNode) : Attribute
{
    /// <summary>
    /// If set to true, parser generator will use typing information from
    /// Lexer's <see cref="Get.Lexer.TypeAttribute{T}"/> and <see cref="Get.Lexer.RegexAttribute{T}"/>
    /// to infer type for the terminals. This is useful if you already use Get.Lexer library.<br/>
    /// If set to false (default), parser generator will use typing information from
    /// Parser's <see cref="TypeAttribute{T}"/>. This is useful if you have used an external lexer and would like this
    /// parser to be compatible without Get.Lexer dependency.
    /// </summary>
    public bool UseGetLexerTypeInformation { get; set; } = false;
}
/// <summary>
/// Specifies a precedence for a parser.<br/>
/// Parameters are provided in the following format:<br/>
/// <code>
/// PrecedenceDecl = listof(Precedence)
/// Precedence = listof(Terminal) Associativity
/// 
/// where Es = listof(E) means Es = E | Es (E)
/// </code>
/// Types:<br/>
/// - Terminal : user defined enum<br/>
/// - Associativity : <see cref="Associativity"/>
/// </summary>
/// <param name="parameters"></param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PrecedenceAttribute(params object[] parameters) : Attribute
{

}