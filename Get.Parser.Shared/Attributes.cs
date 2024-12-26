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
class RuleAttribute(params object[] ReduceRuleDefinition) : Attribute;
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
class TypeAttribute<T> : Attribute;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
class ParserAttribute<T> : Attribute;