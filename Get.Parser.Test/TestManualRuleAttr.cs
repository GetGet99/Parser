using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Get.Parser.Test;
using static TestManualRuleAttr.Keywords;
[TestClass]
public partial class TestManualRuleAttr
{
    enum UserNonTerminal { Start, Expression }
    enum UserTerminal { Number, Plus, Minus, Times, Divide, OpenBracket, CloseBracket }
    record class ExprAST;
    [TestMethod]
    public void Test()
    {
        var dfa = GetDFA();
        TestBlock1(dfa);
        TestBlock2(dfa);
        TestBlock3(dfa);
        TestBlock4(dfa);
        TestBlock5(dfa);
        TestBlock6(dfa);
        TestBlock7(dfa);
        TestBlock8(dfa);
        TestBlock9(dfa);
        TestBlock10(dfa);
        TestBlock11(dfa);
        TestBlock12(dfa);
        TestBlock13(dfa);
        TestBlock14(dfa);
        TestBlock15(dfa);
        TestBlock16(dfa);
        TestBlock17(dfa);
        TestBlock18(dfa);
    }
    void TestBlock1(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, "MethodCall"));
        Assert.AreEqual(1, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal);
        Assert.AreEqual(UserTerminal.Number, rule.Elements[0].Raw.RawEnum);
        Assert.IsNull(rule.Elements[0].AsParameter);
        Assert.AreEqual(0, rule.Options.Count);
        Assert.IsInstanceOfType<ReduceMethod>(rule.ReduceAction);
        Assert.AreEqual("MethodCall", ((ReduceMethod)rule.ReduceAction).Name);
    }
    void TestBlock2(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, WITHPARAM, "Culture", "Default", "MethodCall"));
        Assert.AreEqual(1, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal);
        Assert.AreEqual(UserTerminal.Number, rule.Elements[0].Raw.RawEnum);
        Assert.IsNull(rule.Elements[0].AsParameter);
        Assert.AreEqual(1, rule.Options.Count);
        Assert.AreEqual("Culture", rule.Options[0].ParameterName);
        Assert.AreEqual("Default", rule.Options[0].ConstantParameterValue);
        Assert.IsInstanceOfType<ReduceMethod>(rule.ReduceAction);
        Assert.AreEqual("MethodCall", ((ReduceMethod)rule.ReduceAction).Name);
    }
    void TestBlock3(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, WITHPARAM, "Culture", "Default", typeof(ExprAST)));
        Assert.AreEqual(1, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal);
        Assert.AreEqual(UserTerminal.Number, rule.Elements[0].Raw.RawEnum);
        Assert.IsNull(rule.Elements[0].AsParameter);
        Assert.AreEqual(1, rule.Options.Count);
        Assert.AreEqual("Culture", rule.Options[0].ParameterName);
        Assert.AreEqual("Default", rule.Options[0].ConstantParameterValue);
        Assert.IsInstanceOfType<ReduceConstructor>(rule.ReduceAction);
        Assert.AreEqual(typeof(ExprAST), ((ReduceConstructor)rule.ReduceAction).Type_);
    }
    void TestBlock4(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, AS, "Value", WITHPARAM, "Culture", "Default", typeof(ExprAST)));
        Assert.AreEqual(1, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal);
        Assert.AreEqual(UserTerminal.Number, rule.Elements[0].Raw.RawEnum);
        Assert.AreEqual("Value", rule.Elements[0].AsParameter);
        Assert.AreEqual(1, rule.Options.Count);
        Assert.AreEqual("Culture", rule.Options[0].ParameterName);
        Assert.AreEqual("Default", rule.Options[0].ConstantParameterValue);
        Assert.IsInstanceOfType<ReduceConstructor>(rule.ReduceAction);
        Assert.AreEqual(typeof(ExprAST), ((ReduceConstructor)rule.ReduceAction).Type_);
    }
    void TestBlock5(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals("MethodCall"));
        Assert.AreEqual(0, rule.Elements.Count);
        Assert.AreEqual(0, rule.Options.Count);
        Assert.IsInstanceOfType<ReduceMethod>(rule.ReduceAction);
        Assert.AreEqual("MethodCall", ((ReduceMethod)rule.ReduceAction).Name);
    }
    void TestBlock6(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, UserTerminal.Plus, "MethodCall"));
        Assert.AreEqual(2, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal && (UserTerminal)rule.Elements[0].Raw.RawEnum == UserTerminal.Number);
        Assert.IsTrue(rule.Elements[1].Raw.IsTerminal && (UserTerminal)rule.Elements[1].Raw.RawEnum == UserTerminal.Plus);
        Assert.IsNull(rule.Elements[0].AsParameter);
        Assert.IsNull(rule.Elements[1].AsParameter);
        Assert.AreEqual(0, rule.Options.Count);
        Assert.IsInstanceOfType<ReduceMethod>(rule.ReduceAction);
        Assert.AreEqual("MethodCall", ((ReduceMethod)rule.ReduceAction).Name);
    }
    void TestBlock7(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, UserTerminal.Plus, AS, "Value", typeof(ExprAST)));
        Assert.AreEqual(2, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal && (UserTerminal)rule.Elements[0].Raw.RawEnum == UserTerminal.Number);
        Assert.IsTrue(rule.Elements[1].Raw.IsTerminal && (UserTerminal)rule.Elements[1].Raw.RawEnum == UserTerminal.Plus);
        Assert.IsNull(rule.Elements[0].AsParameter);
        Assert.AreEqual("Value", rule.Elements[1].AsParameter);
        Assert.AreEqual(0, rule.Options.Count);
        Assert.IsInstanceOfType<ReduceConstructor>(rule.ReduceAction);
        Assert.AreEqual(typeof(ExprAST), ((ReduceConstructor)rule.ReduceAction).Type_);
    }
    void TestBlock8(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, AS, "Num", UserTerminal.Plus, AS, "Op", WITHPARAM, "Culture", "Invariant", WITHPARAM, "SomeNote", "Addition", "MethodCall"));
        Assert.AreEqual(2, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal && (UserTerminal)rule.Elements[0].Raw.RawEnum == UserTerminal.Number);
        Assert.IsTrue(rule.Elements[1].Raw.IsTerminal && (UserTerminal)rule.Elements[1].Raw.RawEnum == UserTerminal.Plus);
        Assert.AreEqual("Num", rule.Elements[0].AsParameter);
        Assert.AreEqual("Op", rule.Elements[1].AsParameter);
        Assert.AreEqual(2, rule.Options.Count);
        Assert.AreEqual("Culture", rule.Options[0].ParameterName);
        Assert.AreEqual("Invariant", rule.Options[0].ConstantParameterValue);
        Assert.AreEqual("SomeNote", rule.Options[1].ParameterName);
        Assert.AreEqual("Addition", rule.Options[1].ConstantParameterValue);
        Assert.IsInstanceOfType<ReduceMethod>(rule.ReduceAction);
        Assert.AreEqual("MethodCall", ((ReduceMethod)rule.ReduceAction).Name);
    }
    void TestBlock9(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(WITHPARAM, "Culture", "Default", typeof(ExprAST)));
        Assert.AreEqual(0, rule.Elements.Count);
        Assert.AreEqual(1, rule.Options.Count);
        Assert.AreEqual("Culture", rule.Options[0].ParameterName);
        Assert.AreEqual("Default", rule.Options[0].ConstantParameterValue);
        Assert.IsInstanceOfType<ReduceConstructor>(rule.ReduceAction);
        Assert.AreEqual(typeof(ExprAST), ((ReduceConstructor)rule.ReduceAction).Type_);
    }
    void TestBlock10(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, AS, "Num", typeof(ExprAST)));
        Assert.AreEqual(1, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal && (UserTerminal)rule.Elements[0].Raw.RawEnum == UserTerminal.Number);
        Assert.AreEqual("Num", rule.Elements[0].AsParameter);
        Assert.AreEqual(0, rule.Options.Count);
        Assert.IsInstanceOfType<ReduceConstructor>(rule.ReduceAction);
        Assert.AreEqual(typeof(ExprAST), ((ReduceConstructor)rule.ReduceAction).Type_);
    }
    void TestBlock11(ILRParserDFA dfa)
    {
        Assert.ThrowsException<LRParserRuntimeUnexpectedInputException>(() =>
            LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, AS, "Num", typeof(ExprAST), "Should not be here")));
    }
    void TestBlock12(ILRParserDFA dfa)
    {
        Assert.ThrowsException<LRParserRuntimeUnexpectedEndingException>(() =>
            LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, AS, "Num")));
    }
    void TestBlock13(ILRParserDFA dfa)
    {
        Assert.ThrowsException<LRParserRuntimeUnexpectedInputException>(() =>
            LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, AS, AS)));
    }
    void TestBlock14(ILRParserDFA dfa)
    {
        Assert.ThrowsException<LRParserRuntimeUnexpectedInputException>(() =>
            LRParserRunner<Rule>.Parse(dfa, GetTerminals(WITHPARAM, "parameter", "argument", UserTerminal.Number, AS, "n1", "ReduceMethod")));
    }
    void TestBlock15(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, WITHPARAM, "Culture", "Default", "MethodCall"));
        Assert.AreEqual(1, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal);
        Assert.AreEqual(UserTerminal.Number, rule.Elements[0].Raw.RawEnum);
        Assert.IsNull(rule.Elements[0].AsParameter);
        Assert.AreEqual(1, rule.Options.Count);
        Assert.AreEqual("Culture", rule.Options[0].ParameterName);
        Assert.AreEqual("Default", rule.Options[0].ConstantParameterValue);
        Assert.IsInstanceOfType<ReduceMethod>(rule.ReduceAction);
        Assert.AreEqual("MethodCall", ((ReduceMethod)rule.ReduceAction).Name);
    }
    void TestBlock16(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, WITHPARAM, "Culture", "Default", typeof(ExprAST)));
        Assert.AreEqual(1, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal);
        Assert.AreEqual(UserTerminal.Number, rule.Elements[0].Raw.RawEnum);
        Assert.IsNull(rule.Elements[0].AsParameter);
        Assert.AreEqual(1, rule.Options.Count);
        Assert.AreEqual("Culture", rule.Options[0].ParameterName);
        Assert.AreEqual("Default", rule.Options[0].ConstantParameterValue);
        Assert.IsInstanceOfType<ReduceConstructor>(rule.ReduceAction);
        Assert.AreEqual(typeof(ExprAST), ((ReduceConstructor)rule.ReduceAction).Type_);
    }
    void TestBlock17(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals(UserTerminal.Number, AS, "Value", WITHPARAM, "Culture", "Default", typeof(ExprAST)));
        Assert.AreEqual(1, rule.Elements.Count);
        Assert.IsTrue(rule.Elements[0].Raw.IsTerminal);
        Assert.AreEqual(UserTerminal.Number, rule.Elements[0].Raw.RawEnum);
        Assert.AreEqual("Value", rule.Elements[0].AsParameter);
        Assert.AreEqual(1, rule.Options.Count);
        Assert.AreEqual("Culture", rule.Options[0].ParameterName);
        Assert.AreEqual("Default", rule.Options[0].ConstantParameterValue);
        Assert.IsInstanceOfType<ReduceConstructor>(rule.ReduceAction);
        Assert.AreEqual(typeof(ExprAST), ((ReduceConstructor)rule.ReduceAction).Type_);
    }
    void TestBlock18(ILRParserDFA dfa)
    {
        var rule = LRParserRunner<Rule>.Parse(dfa, GetTerminals("MethodCall"));
        Assert.AreEqual(0, rule.Elements.Count);
        Assert.AreEqual(0, rule.Options.Count);
        Assert.IsInstanceOfType<ReduceMethod>(rule.ReduceAction);
        Assert.AreEqual("MethodCall", ((ReduceMethod)rule.ReduceAction).Name);
    }
    IEnumerable<ITerminalValue> GetTerminals(params IEnumerable<object> objects)
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
