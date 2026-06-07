namespace Get.RegexMachine.Test;

using static TestUtils;

[TestClass]
public class TestUnicode
{
    // ── CharRange unit tests ──────────────────────────────────────────

    [TestMethod]
    public void CharRange_Contains()
    {
        var r = new CharRange('a', 'z');
        Assert.IsTrue(r.Contains('m'));
        Assert.IsTrue(r.Contains('a'));
        Assert.IsTrue(r.Contains('z'));
        Assert.IsFalse(r.Contains('A'));
        Assert.IsFalse(r.Contains('0'));
    }

    [TestMethod]
    public void CharRange_Overlaps()
    {
        var a = new CharRange('a', 'z');
        var b = new CharRange('x', 'z');
        var c = new CharRange('A', 'C');
        Assert.IsTrue(a.Overlaps(b));
        Assert.IsFalse(a.Overlaps(c));
        Assert.IsFalse(b.Overlaps(c));
    }

    [TestMethod]
    public void CharRange_CanMerge_Overlapping()
    {
        var a = new CharRange('a', 'm');
        var b = new CharRange('k', 'z');
        Assert.IsTrue(a.CanMerge(b));
        Assert.IsTrue(b.CanMerge(a));
    }

    [TestMethod]
    public void CharRange_CanMerge_Adjacent()
    {
        var a = new CharRange('a', 'm');
        var b = new CharRange('n', 'z');
        Assert.IsTrue(a.CanMerge(b));
    }

    [TestMethod]
    public void CharRange_CanMerge_NotAdjacent()
    {
        var a = new CharRange('a', 'm');
        var b = new CharRange('p', 'z');
        Assert.IsFalse(a.CanMerge(b));
    }

    [TestMethod]
    public void CharRange_Merge()
    {
        var a = new CharRange('a', 'm');
        var b = new CharRange('n', 'z');
        var merged = CharRange.Merge(a, b);
        Assert.AreEqual('a', merged.From);
        Assert.AreEqual('z', merged.To);
    }

    [TestMethod]
    public void CharRange_Merge_Overlapping()
    {
        var a = new CharRange('a', 'z');
        var b = new CharRange('j', 'x');
        var merged = CharRange.Merge(a, b);
        Assert.AreEqual('a', merged.From);
        Assert.AreEqual('z', merged.To);
    }

    [TestMethod]
    public void CharRange_FullRange()
    {
        var r = new CharRange(char.MinValue, char.MaxValue);
        Assert.IsTrue(r.Contains(char.MinValue));
        Assert.IsTrue(r.Contains(char.MaxValue));
        Assert.IsTrue(r.Contains('a'));
        Assert.IsTrue(r.Contains('\u00E9'));
        Assert.IsTrue(r.Contains('\u4E2D'));
    }

    // ── Literal non-ASCII character matching ───────────────────────

    [TestMethod]
    public void Unicode_LiteralCharsInClass()
    {
        var dfa = SimpleDFA(@"[äöü]+");

        AssertMatch(dfa, "ä", "ä");
        AssertMatch(dfa, "ö", "ö");
        AssertMatch(dfa, "ü", "ü");
        AssertMatch(dfa, "äöü", "äöü");
        AssertNoMatch(dfa, "a");
        AssertNoMatch(dfa, "aeu");
    }

    [TestMethod]
    public void Unicode_MixedAsciiAndUnicodeClass()
    {
        var dfa = SimpleDFA(@"[a-zäöü]+");

        AssertMatch(dfa, "abc", "abc");
        AssertMatch(dfa, "ä", "ä");
        AssertMatch(dfa, "testä", "testä");
        AssertMatch(dfa, "müll", "müll");
        AssertNoMatch(dfa, "ABC");
    }

    [TestMethod]
    public void Unicode_LiteralNonAsciiOutsideClass()
    {
        var dfa = SimpleDFA("é");

        AssertMatch(dfa, "é", "é");
        AssertNoMatch(dfa, "e");
    }

    [TestMethod]
    public void Unicode_MultipleNonAsciiChars()
    {
        var dfa = SimpleDFA("äöü");

        AssertMatch(dfa, "äöü", "äöü");
        AssertNoMatch(dfa, "äö");
    }

    // ── Range with non-ASCII characters ────────────────────────────

    [TestMethod]
    public void Unicode_RangeLatin1()
    {
        // \u00E0 = à, \u00FF = ÿ (C# expands \uXXXX in regular strings)
        var dfa = SimpleDFA("[\u00E0-\u00FF]+");

        AssertMatch(dfa, "à", "à");
        AssertMatch(dfa, "é", "é");
        AssertMatch(dfa, "ñ", "ñ");
        AssertMatch(dfa, "ü", "ü");
        AssertMatch(dfa, "àéïõü", "àéïõü");
        AssertNoMatch(dfa, "a");
        AssertNoMatch(dfa, "z");
    }

    [TestMethod]
    public void Unicode_RangeCJK()
    {
        // \u4E00 = 一, \u9FFF (CJK Unified Ideographs)
        var dfa = SimpleDFA("[\u4E00-\u9FFF]+");

        AssertMatch(dfa, "\u4E00", "\u4E00");
        AssertMatch(dfa, "\u4E2D", "\u4E2D");
        AssertMatch(dfa, "\u4E2D\u56FD", "\u4E2D\u56FD");
        AssertNoMatch(dfa, "abc");
    }

    // ── Inverse character class ────────────────────────────────────

    [TestMethod]
    public void Unicode_InverseClass_MatchesNonAscii()
    {
        // [^a-z]+ should match non-ASCII (was ASCII-only before)
        var dfa = SimpleDFA(@"[^a-z]+");

        AssertMatch(dfa, "123", "123");
        AssertMatch(dfa, "ABC", "ABC");
        AssertMatch(dfa, "é", "é");
        AssertMatch(dfa, "äöü", "äöü");
        AssertMatch(dfa, "\u4E2D", "\u4E2D");
        AssertNoMatch(dfa, "abc");
    }

    [TestMethod]
    public void Unicode_InverseDigitClass_MatchesUnicode()
    {
        var dfa = SimpleDFA(@"[^0-9]+");

        AssertMatch(dfa, "abc", "abc");
        AssertMatch(dfa, "ééé", "ééé");
        AssertMatch(dfa, "\u65E5\u672C\u8A9E", "\u65E5\u672C\u8A9E");
        AssertNoMatch(dfa, "123");
    }

    [TestMethod]
    public void Unicode_InverseClass_WithUnicodeExclusions()
    {
        // [^a-zäöü]+ excludes both ASCII and non-ASCII chars
        var dfa = SimpleDFA(@"[^a-zäöü]+");

        AssertMatch(dfa, "ABC", "ABC");
        AssertMatch(dfa, "é", "é");
        AssertMatch(dfa, "123", "123");
        AssertNoMatch(dfa, "abc");
        AssertNoMatch(dfa, "äöü");
    }

    // ── Dot ────────────────────────────────────────────────────────

    [TestMethod]
    public void Unicode_Dot_MatchesNonAscii()
    {
        var dfa = SimpleDFA(@".+");

        AssertMatch(dfa, "hello", "hello");
        AssertMatch(dfa, "héllö", "héllö");
        AssertMatch(dfa, "\u65E5\u672C\u8A9E", "\u65E5\u672C\u8A9E");
        AssertMatch(dfa, "\u00E9\u4E2D\u00FF", "\u00E9\u4E2D\u00FF");
    }

    [TestMethod]
    public void Unicode_Dot_ExcludesLineTerminators()
    {
        var dfa = SimpleDFA(@".+");

        AssertMatch(dfa, "a\nb", "a");
    }

    // ── \s and \S inside character class ──────────────────────────

    [TestMethod]
    public void Unicode_ShorthandS_InsideClass()
    {
        var dfa = SimpleDFA(@"[\s]+");

        AssertMatch(dfa, "   ", "   ");
        AssertMatch(dfa, "\t", "\t");
        AssertMatch(dfa, "\n", "\n");
        AssertMatch(dfa, "\r", "\r");
        AssertNoMatch(dfa, "abc");
    }

    [TestMethod]
    public void Unicode_ShorthandCapitalS_InsideClass()
    {
        var dfa = SimpleDFA(@"[\S]+");

        AssertMatch(dfa, "abc", "abc");
        AssertMatch(dfa, "é", "é");
        AssertMatch(dfa, "\u65E5\u672C\u8A9E", "\u65E5\u672C\u8A9E");
        AssertNoMatch(dfa, "   ");
        AssertNoMatch(dfa, "\t");
    }

    // ── Mixed / multi-rule ─────────────────────────────────────────

    [TestMethod]
    public void Unicode_MultipleRules_Sequential()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new(@"[0-9]+", "Number"),
            new(@"[a-zäöü]+", "Word"),
            new(@"[\s]+", "Space"),
        ], RegexConflictBehavior.Throw);

        var iter = Iter("abc 123 müll");
        var helper = new AssertionHelper<string>(dfa, iter);
        helper.AssertNext("Word", "abc");
        helper.AssertNext("Space", " ");
        helper.AssertNext("Number", "123");
        helper.AssertNext("Space", " ");
        helper.AssertNext("Word", "müll");
        helper.AssertNoMore();
    }

    [TestMethod]
    public void Unicode_ConflictWithUnicodeRules()
    {
        // With Last behavior + explicit Order, higher Order wins
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("café", "LiteralCafe", Order: 0),
            new(@"[a-zé]+", "Word", Order: 1),
        ], RegexConflictBehavior.Last);

        var iter = Iter("café");
        var (val, matched) = RegexRunner<string>.Next(dfa, iter)!.Value;
        Assert.AreEqual("Word", val);
        Assert.AreEqual("café", matched);
    }

    // ── Edge cases ─────────────────────────────────────────────────

    [TestMethod]
    public void Unicode_ClassWithMaxValueChar()
    {
        // \uFFFF is char.MaxValue
        var dfa = SimpleDFA("[\uFFFF]+");

        AssertMatch(dfa, "\uFFFF", "\uFFFF");
        AssertNoMatch(dfa, "a");
    }

    [TestMethod]
    public void Unicode_DotMatchesMaxValue()
    {
        var dfa = SimpleDFA(".");

        AssertMatch(dfa, "\uFFFF", "\uFFFF");
    }

    [TestMethod]
    public void Unicode_InverseClass_ExcludesMaxValue()
    {
        // [^\uFFFF] should match everything except char.MaxValue
        var dfa = SimpleDFA("[^\uFFFF]+");

        AssertMatch(dfa, "abc", "abc");
        AssertMatch(dfa, "é", "é");
        AssertNoMatch(dfa, "\uFFFF");
    }

    [TestMethod]
    public void Unicode_EmptyInverseClass()
    {
        // [^a] should match full range except 'a'
        var dfa = SimpleDFA(@"[^a]+");

        AssertMatch(dfa, "b", "b");
        AssertMatch(dfa, "é", "é");
        AssertMatch(dfa, "\u4E2D", "\u4E2D");
        AssertNoMatch(dfa, "a");
    }

    // ── Helpers ───────────────────────────────────────────────────

    static void AssertMatch(RegexCompiler<string>.DFAState dfa, string input, string expectedMatch)
    {
        var iter = Iter(input);
        var result = RegexRunner<string>.Next(dfa, iter);
        Assert.IsTrue(result.HasValue, $"Expected match for '{input}'");
        Assert.AreEqual(expectedMatch, result.Value.matchedText);
    }

    static void AssertNoMatch(RegexCompiler<string>.DFAState dfa, string input)
    {
        var iter = Iter(input);
        var result = RegexRunner<string>.Next(dfa, iter);
        Assert.IsFalse(result.HasValue, $"Expected no match for '{input}'");
    }

    static RegexCompiler<string>.DFAState SimpleDFA(string regex)
    {
        return RegexCompiler<string>.GenerateDFA([new(regex, "matched")], RegexConflictBehavior.Throw);
    }
}
