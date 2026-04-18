namespace Get.RegexMachine.Test;

using static TestUtils;

[TestClass]
public class TestConcurrentDfaGeneration
{
    [TestMethod]
    public void ConcurrentDfaGeneration()
    {
        Parallel.For(0, 128, _ =>
        {
            RegexCompiler<string>.DFAState dfa = RegexCompiler<string>.GenerateDFA([
                new(@"[0-9]+", "Integer"),
                new(@"[a-zA-Z_][a-zA-Z_0-9]*", "Identifier"),
                new(@"\+", "Plus"),
                new(@"-", "Minus"),
                new(@"[\t ]+", "Whitespace"),
                new(@"\*", "Times")
            ], RegexConflictBehavior.Throw);

            var iter = Iter("1234 + abc");
            var assertionHelper = new AssertionHelper<string>(dfa, iter);
            assertionHelper.AssertNext("Integer", "1234");
            assertionHelper.AssertNext("Whitespace", " ");
            assertionHelper.AssertNext("Plus", "+");
            assertionHelper.AssertNext("Whitespace", " ");
            assertionHelper.AssertNext("Identifier", "abc");
            assertionHelper.AssertNoMore();
        });
    }
}
