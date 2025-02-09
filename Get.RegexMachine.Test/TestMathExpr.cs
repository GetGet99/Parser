namespace Get.RegexMachine.Test;

using static TestUtils;
[TestClass]
public class TestMathExpr
{
    [TestMethod]
    public void Test()
    {
        const string Int = "Integer", Id = "Identifier", Plus = "Plus", Minus = "Minus", Times = "Times", Whitespace = "Whitespace";
        RegexCompiler<string>.DFAState dfa = RegexCompiler<string>.GenerateDFA([
            new(@"[0-9]+", Int),
            new(@"[a-zA-Z_][a-zA-Z_0-9]*", Id),
            new(@"\+", Plus),
            new(@"-", Minus),
            new(@"[\t ]+", Whitespace),
            new(@"\*", Times)
        ], RegexConflictBehavior.Throw);
        var iter = Iter("1234 + 123 * 2 - someVariable");
        var assertionHelper = new AssertionHelper<string>(dfa, iter);
        assertionHelper.AssertNext(Int, "1234");
        assertionHelper.AssertNext(Whitespace, " ");
        assertionHelper.AssertNext(Plus, "+");
        assertionHelper.AssertNext(Whitespace, " ");
        assertionHelper.AssertNext(Int, "123");
        assertionHelper.AssertNext(Whitespace, " ");
        assertionHelper.AssertNext(Times, "*");
        assertionHelper.AssertNext(Whitespace, " ");
        assertionHelper.AssertNext(Int, "2");
        assertionHelper.AssertNext(Whitespace, " ");
        assertionHelper.AssertNext(Minus, "-");
        assertionHelper.AssertNext(Whitespace, " ");
        assertionHelper.AssertNext(Id, "someVariable");
        assertionHelper.AssertNoMore();
        iter.Reset();
    }
}
