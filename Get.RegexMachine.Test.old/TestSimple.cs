using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Get.RegexMachine.Test;
using static TestUtils;
public class TestSimple
{
    [Fact]
    public void SimpleCat()
    {
        var dfa = SimpleDFA(@"text");
        AssertMatch(dfa, "text", "text");
        AssertMatch(dfa, "textbutlonger", "text");
        AssertMatch(dfa, "sometext", "text");
        AssertNoMatch(dfa, "something");
        AssertNoMatch(dfa, "Text");
    }
    static RegexCompiler<string>.DFAState SimpleDFA([StringSyntax(StringSyntaxAttribute.Regex)] string Regex)
    {
        return RegexCompiler<string>.GenerateDFA([new(Regex, "matched")], RegexConflictBehavior.Throw);
    }
    static void AssertMatch(RegexCompiler<string>.DFAState dfa, string toMatch, string matchedText)
    {
        var output = RegexRunner<string>.Next(dfa, Iter(toMatch));
        var (status, matched) = Assert.NotNull(output);
        Assert.Equal("matched", status);
        Assert.Equal(matchedText, matched);
    }
    static void AssertNoMatch(RegexCompiler<string>.DFAState dfa, string toMatch)
    {
        var output = RegexRunner<string>.Next(dfa, Iter(toMatch));
        Assert.Null(output);
    }
}
