using System.Diagnostics.CodeAnalysis;

namespace Get.RegexMachine.Test;
using static TestUtils;
[TestClass]
public class TestSimple
{
    [TestMethod]
    public void SimpleCat()
    {
        var dfa = SimpleDFA(@"text");
        AssertMatch(dfa, "text", "text");
        AssertMatch(dfa, "textbutlonger", "text");
        AssertNoMatch(dfa, "sometext"); // it tries to match at the beginning
        AssertNoMatch(dfa, "something");
        AssertNoMatch(dfa, "Text");
    }
    [TestMethod]
    public void StarPrecedence()
    {
        var dfa = SimpleDFA(@"ab*");
        AssertMatch(dfa, "a", "a");
        AssertMatch(dfa, "ab", "ab");
        AssertMatch(dfa, "abbbbb", "abbbbb");
        AssertMatch(dfa, "abbbbbcdef", "abbbbb");
        AssertMatch(dfa, "abababa", "ab");
    }
    static RegexCompiler<string>.DFAState SimpleDFA([StringSyntax(StringSyntaxAttribute.Regex)] string Regex)
    {
        return RegexCompiler<string>.GenerateDFA([new(Regex, "matched")], RegexConflictBehavior.Throw);
    }
    static void AssertMatch(RegexCompiler<string>.DFAState dfa, string toMatch, string matchedText)
    {
        var output = RegexRunner<string>.Next(dfa, Iter(toMatch));
        Assert.IsTrue(output.HasValue);
        var (status, matched) = output.Value;
        Assert.AreEqual("matched", status);
        Assert.AreEqual(matchedText, matched);
    }
    static void AssertNoMatch(RegexCompiler<string>.DFAState dfa, string toMatch)
    {
        var output = RegexRunner<string>.Next(dfa, Iter(toMatch));
        Assert.IsFalse(output.HasValue);
    }
}
