using System.Diagnostics;
namespace Get.Parser.Test;
using static TestManualRuleAttr.Keywords;
static partial class TestManualRuleAttr
{
    enum UserNonTerminal { Start, Expression }
    enum UserTerminal { Number, Plus, Minus, Times, Divide, OpenBracket, CloseBracket }
    record class ExprAST;
    public static void Test()
    {
        var dfa = GetDFA();
        {
            // Rule = Number, ReduceWith: MethodCall
            var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                UserTerminal.Number, "MethodCall"
            ));
            Debug.Assert(rule.Elements.Count == 1);
            Debug.Assert(rule.Elements[0].Raw.IsTerminal);
            Debug.Assert(rule.Elements[0].Raw.RawEnum is UserTerminal terminal && terminal == UserTerminal.Number);
            Debug.Assert(rule.Elements[0].AsParameter is null);
            Debug.Assert(rule.Options.Count == 0);
            Debug.Assert(rule.ReduceAction is ReduceMethod act && act.Name == "MethodCall");
        }
        {
            // Rule = Number, ReduceWith: MethodCall(Culture:"Default")
            var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                UserTerminal.Number, WITHPARAM, "Culture", "Default", "MethodCall"
            ));
            Debug.Assert(rule.Elements.Count == 1);
            Debug.Assert(rule.Elements[0].Raw.IsTerminal);
            Debug.Assert(rule.Elements[0].Raw.RawEnum is UserTerminal terminal && terminal == UserTerminal.Number);
            Debug.Assert(rule.Elements[0].AsParameter is null);
            Debug.Assert(rule.Options.Count == 1);
            Debug.Assert(rule.Options[0].ParameterName == "Culture");
            Debug.Assert(rule.Options[0].ConstantParameterValue is string str && str == "Default");
            Debug.Assert(rule.ReduceAction is ReduceMethod act && act.Name == "MethodCall");
        }
        {
            // Rule = Number, ReduceWith: new Expr(Culture:"Default")
            var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                UserTerminal.Number, WITHPARAM, "Culture", "Default", typeof(ExprAST)
            ));
            Debug.Assert(rule.Elements.Count == 1);
            Debug.Assert(rule.Elements[0].Raw.IsTerminal);
            Debug.Assert(rule.Elements[0].Raw.RawEnum is UserTerminal terminal && terminal == UserTerminal.Number);
            Debug.Assert(rule.Elements[0].AsParameter is null);
            Debug.Assert(rule.Options.Count == 1);
            Debug.Assert(rule.Options[0].ParameterName == "Culture");
            Debug.Assert(rule.Options[0].ConstantParameterValue is string str && str == "Default");
            Debug.Assert(rule.ReduceAction is ReduceConstructor constructor && constructor.Type_ == typeof(ExprAST));
        }
        {
            // Rule = Number::Value, ReduceWith: new Expr(Culture:"Default", Value)
            var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                UserTerminal.Number, AS, "Value", WITHPARAM, "Culture", "Default", typeof(ExprAST)
            ));
            Debug.Assert(rule.Elements.Count == 1);
            Debug.Assert(rule.Elements[0].Raw.IsTerminal);
            Debug.Assert(rule.Elements[0].Raw.RawEnum is UserTerminal terminal && terminal == UserTerminal.Number);
            Debug.Assert(rule.Elements[0].AsParameter is "Value");
            Debug.Assert(rule.Options.Count == 1);
            Debug.Assert(rule.Options[0].ParameterName == "Culture");
            Debug.Assert(rule.Options[0].ConstantParameterValue is string str && str == "Default");
            Debug.Assert(rule.ReduceAction is ReduceConstructor constructor && constructor.Type_ == typeof(ExprAST));
        }
        {
            // Rule = (empty), ReduceWith: MethodCall
            var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                "MethodCall"
            ));
            Debug.Assert(rule.Elements.Count == 0);
            Debug.Assert(rule.Options.Count == 0);
            Debug.Assert(rule.ReduceAction is ReduceMethod act && act.Name == "MethodCall");
        }
        {
            // Rule = Number Plus, ReduceWith: MethodCall
            var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                UserTerminal.Number, UserTerminal.Plus, "MethodCall"
            ));
            Debug.Assert(rule.Elements.Count == 2);
            Debug.Assert(rule.Elements[0].Raw.IsTerminal && rule.Elements[0].Raw.RawEnum is UserTerminal terminal1 && terminal1 == UserTerminal.Number);
            Debug.Assert(rule.Elements[1].Raw.IsTerminal && rule.Elements[1].Raw.RawEnum is UserTerminal terminal2 && terminal2 == UserTerminal.Plus);
            Debug.Assert(rule.Elements[0].AsParameter is null);
            Debug.Assert(rule.Elements[1].AsParameter is null);
            Debug.Assert(rule.Options.Count == 0);
            Debug.Assert(rule.ReduceAction is ReduceMethod act && act.Name == "MethodCall");
        }
        {
            // Rule = Number Plus::Value, ReduceWith: new Expr(Value:[ref])
            var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                UserTerminal.Number, UserTerminal.Plus, AS, "Value", typeof(ExprAST)
            ));
            Debug.Assert(rule.Elements.Count == 2);
            Debug.Assert(rule.Elements[0].Raw.IsTerminal && rule.Elements[0].Raw.RawEnum is UserTerminal terminal1 && terminal1 == UserTerminal.Number);
            Debug.Assert(rule.Elements[1].Raw.IsTerminal && rule.Elements[1].Raw.RawEnum is UserTerminal terminal2 && terminal2 == UserTerminal.Plus);
            Debug.Assert(rule.Elements[0].AsParameter is null);
            Debug.Assert(rule.Elements[1].AsParameter is "Value");
            Debug.Assert(rule.Options.Count == 0);
            Debug.Assert(rule.ReduceAction is ReduceConstructor constructor && constructor.Type_ == typeof(ExprAST));
        }
        {
            // Rule = Number::Num Plus::Op, ReduceWith: MethodCall(Culture:"Invariant", Op:[ref], Num:[ref], SomeNote: "Addition")
            var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                UserTerminal.Number, AS, "Num", UserTerminal.Plus, AS, "Op", WITHPARAM, "Culture", "Invariant", WITHPARAM, "SomeNote", "Addition", "MethodCall"
            ));
            Debug.Assert(rule.Elements.Count == 2);
            Debug.Assert(rule.Elements[0].Raw.IsTerminal && rule.Elements[0].Raw.RawEnum is UserTerminal terminal1 && terminal1 == UserTerminal.Number);
            Debug.Assert(rule.Elements[1].Raw.IsTerminal && rule.Elements[1].Raw.RawEnum is UserTerminal terminal2 && terminal2 == UserTerminal.Plus);
            Debug.Assert(rule.Elements[0].AsParameter == "Num");
            Debug.Assert(rule.Elements[1].AsParameter == "Op");
            Debug.Assert(rule.Options.Count == 2);
            Debug.Assert(rule.Options[0].ParameterName == "Culture" && rule.Options[0].ConstantParameterValue is string str1 && str1 == "Invariant");
            Debug.Assert(rule.Options[1].ParameterName == "SomeNote" && rule.Options[1].ConstantParameterValue is string str2 && str2 == "Addition");
            Debug.Assert(rule.ReduceAction is ReduceMethod act && act.Name == "MethodCall");
        }
        {
            // Rule = (empty), ReduceWith: new Expr(Culture:"Default")
            var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                WITHPARAM, "Culture", "Default", typeof(ExprAST)
            ));
            Debug.Assert(rule.Elements.Count == 0);
            Debug.Assert(rule.Options.Count == 1);
            Debug.Assert(rule.Options[0].ParameterName == "Culture" && rule.Options[0].ConstantParameterValue is string str && str == "Default");
            Debug.Assert(rule.ReduceAction is ReduceConstructor constructor && constructor.Type_ == typeof(ExprAST));
        }
        {
            // Rule = Number::Num, ReduceWith: new Expr(Num: [ref])
            var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                UserTerminal.Number, AS, "Num", typeof(ExprAST)
            ));
            Debug.Assert(rule.Elements.Count == 1);
            Debug.Assert(rule.Elements[0].Raw.IsTerminal && rule.Elements[0].Raw.RawEnum is UserTerminal terminal && terminal == UserTerminal.Number);
            Debug.Assert(rule.Elements[0].AsParameter == "Num");
            Debug.Assert(rule.Options.Count == 0);
            Debug.Assert(rule.ReduceAction is ReduceConstructor constructor && constructor.Type_ == typeof(ExprAST));
        }
        {
            // Invalid syntax: additional item
            try
            {
                var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                    UserTerminal.Number, AS, "Num", typeof(ExprAST), "Should not be here"
                ));
                Debugger.Break(); // not supposed to not throw exception
            }
            catch (LRParserRuntimeUnexpectedInputException e)
            {
                Debug.Assert(e.UnexpectedElement.ToString()?.Contains("Should not be here") ?? false);
            }
        }
        {
            // Invalid syntax: no target
            try
            {
                var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                    UserTerminal.Number, AS, "Num"
                ));
                Debugger.Break(); // not supposed to not throw exception
            }
            catch (LRParserRuntimeUnexpectedEndingException) { }
        }
        {
            // Invalid syntax: malformed syntax
            try
            {
                var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                    UserTerminal.Number, AS, AS
                ));
                Debugger.Break(); // not supposed to not throw exception
            }
            catch (LRParserRuntimeUnexpectedInputException e)
            {
                Debug.Assert(e.UnexpectedElement.ToString()?.ToLowerInvariant().Contains("as", StringComparison.InvariantCultureIgnoreCase) ?? false);
            }
        }
        {
            // Invalid syntax: rule after options
            try
            {
                var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(
                    WITHPARAM, "parameter", "argument", UserTerminal.Number, AS, "n1", "ReduceMethod"
                ));
                Debugger.Break(); // not supposed to not throw exception
            }
            catch (LRParserRuntimeUnexpectedInputException e)
            {
                Debug.Assert(e.UnexpectedElement.ToString()?.ToLowerInvariant().Contains("number", StringComparison.InvariantCultureIgnoreCase) ?? false);
            }
        }
        static IEnumerable<ITerminalValue> GetTerminals(params IEnumerable<object> objects)
        {
            foreach (object obj in objects)
            {
                yield return new TerminalValue(obj, 
                    obj switch
                    {
                        Keywords k => k is Keywords.AS ? Terminal.As : Terminal.WithParam,
                        string => Terminal.String,
                        UserNonTerminal => Terminal.NonTerminal,
                        UserTerminal => Terminal.Terminal,
                        Type => Terminal.Type,
                        _ => Terminal.Unknown
                    }
                );
            }
        }
    }
}
