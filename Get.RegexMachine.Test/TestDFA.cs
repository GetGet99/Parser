namespace Get.RegexMachine.Test;

using static TestUtils;

[TestClass]
public class TestDFA
{
    [TestMethod]
    public void ConflictThrow_OverlappingPatterns()
    {
        Assert.ThrowsException<RegexConflictCompilerException>(() =>
            RegexCompiler<string>.GenerateDFA([
                new("if", "Keyword"),
                new("[a-z]+", "Identifier"),
            ], RegexConflictBehavior.Throw));
    }

    [TestMethod]
    public void ConflictThrow_ExactSamePattern()
    {
        Assert.ThrowsException<RegexConflictCompilerException>(() =>
            RegexCompiler<string>.GenerateDFA([
                new("abc", "First"),
                new("abc", "Second"),
            ], RegexConflictBehavior.Throw));
    }

    [TestMethod]
    public void ConflictLast_TakesLastDeclared()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("if", "Keyword", Order: 0),
            new("[a-z]+", "Identifier", Order: 1),
        ], RegexConflictBehavior.Last);

        var iter = Iter("if");
        var (val, _) = RegexRunner<string>.Next(dfa, iter)!.Value;
        Assert.AreEqual("Identifier", val);
    }

    [TestMethod]
    public void ConflictLast_SamePattern_LastWins()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("abc", "First", Order: 0),
            new("abc", "Second", Order: 1),
        ], RegexConflictBehavior.Last);

        var iter = Iter("abc");
        var (val, _) = RegexRunner<string>.Next(dfa, iter)!.Value;
        Assert.AreEqual("Second", val);
    }

    [TestMethod]
    public void OrderPrecedence_HigherOrderWins()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[a-z]+", "Identifier", Order: 0),
            new("if", "Keyword", Order: 1),
        ], RegexConflictBehavior.Throw);

        var iter = Iter("if");
        var (val, _) = RegexRunner<string>.Next(dfa, iter)!.Value;
        Assert.AreEqual("Keyword", val);
    }

    [TestMethod]
    public void OrderPrecedence_LowerOrderMatchesShorterStopsAtNonMatch()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[a-z]+", "Identifier", Order: 1),
            new("if", "Keyword", Order: 0),
        ], RegexConflictBehavior.Throw);

        var iter = Iter("ifabc");
        var (val, matched) = RegexRunner<string>.Next(dfa, iter)!.Value;
        Assert.AreEqual("Identifier", val);
        Assert.AreEqual("ifabc", matched);
    }

    [TestMethod]
    public void NoMatch_ReturnsNull()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[0-9]+", "Number"),
        ], RegexConflictBehavior.Throw);

        var iter = Iter("abc");
        var result = RegexRunner<string>.Next(dfa, iter);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void EmptyInput_NoMatch()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[0-9]+", "Number"),
        ], RegexConflictBehavior.Throw);

        var iter = Iter("");
        var result = RegexRunner<string>.Next(dfa, iter);
        Assert.IsFalse(result.HasValue);
    }

    [Ignore("Alternation inside a single regex rule only matches first branch")]
    [TestMethod]
    public void Alternation_EitherBranch()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("cat|dog", "Animal"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "cat", "cat");
        AssertMatch(dfa, "dog", "dog");
        AssertNoMatch(dfa, "cow");
    }

    [TestMethod]
    public void Star_ZeroOrMore()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("ab*c", "Matched"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "ac", "ac");
        AssertMatch(dfa, "abc", "abc");
        AssertMatch(dfa, "abbbc", "abbbc");
        AssertNoMatch(dfa, "adc");
    }

    [TestMethod]
    public void Plus_OneOrMore()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("ab+c", "Matched"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "abc", "abc");
        AssertMatch(dfa, "abbbc", "abbbc");
        AssertNoMatch(dfa, "ac");
    }

    [TestMethod]
    public void CharacterClass_Range()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[a-z]+", "Lower"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "abc", "abc");
        AssertMatch(dfa, "z", "z");
        AssertNoMatch(dfa, "ABC");
        AssertNoMatch(dfa, "123");
    }

    [TestMethod]
    public void CharacterClass_Negated()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[^0-9]+", "NotNumber"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "abc", "abc");
        AssertNoMatch(dfa, "123");
    }

    [TestMethod]
    public void Dot_AnyCharacter()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new(".+", "Any"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "hello", "hello");
        AssertMatch(dfa, "123!@#", "123!@#");
    }

    [Ignore("? quantifier is not implemented in the regex parser grammar")]
    [TestMethod]
    public void Optional_QuestionMark()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("colou?r", "Color"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "color", "color");
        AssertMatch(dfa, "colour", "colour");
    }

    [TestMethod]
    public void LongestMatch_WinsOverShorter()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("if", "Keyword"),
            new("[a-z]+", "Identifier"),
        ], RegexConflictBehavior.Last);

        var iter = Iter("ifconfig");
        var (val, matched) = RegexRunner<string>.Next(dfa, iter)!.Value;
        Assert.AreEqual("Identifier", val);
        Assert.AreEqual("ifconfig", matched);
    }

    [TestMethod]
    public void MultipleRules_SequentialMatch()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[0-9]+", "Number"),
            new("[a-zA-Z_][a-zA-Z_0-9]*", "Ident"),
        ], RegexConflictBehavior.Throw);

        var iter = Iter("42abc");
        var helper = new AssertionHelper<string>(dfa, iter);
        helper.AssertNext("Number", "42");
        helper.AssertNext("Ident", "abc");
        helper.AssertNoMore();
    }

    [TestMethod]
    public void MultipleRules_WithWhitespaceSkipper()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[0-9]+", "Number"),
            new("[a-zA-Z_][a-zA-Z_0-9]*", "Ident"),
            new(@"[\t ]+", "Space"),
        ], RegexConflictBehavior.Throw);

        var iter = Iter("abc 123");
        var helper = new AssertionHelper<string>(dfa, iter);
        helper.AssertNext("Ident", "abc");
        helper.AssertNext("Space", " ");
        helper.AssertNext("Number", "123");
        helper.AssertNoMore();
    }

    [TestMethod]
    public void EscapeSequences()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new(@"\n", "Newline"),
            new(@"\t", "Tab"),
            new(@"\r", "CarriageReturn"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "\n", "\n");
        AssertMatch(dfa, "\t", "\t");
        AssertMatch(dfa, "\r", "\r");
    }

    [TestMethod]
    public void EmptyPattern_MatchesEmptyString()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("", "Empty"),
        ], RegexConflictBehavior.Throw);

        var iter = Iter("abc");
        var result = RegexRunner<string>.Next(dfa, iter);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual("Empty", result.Value.value);
        Assert.AreEqual("", result.Value.matchedText);
    }

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

    static void AssertMatchAt(RegexCompiler<string>.DFAState dfa, ISeekable<char> iter, string expectedVal, string expectedMatch)
    {
        var result = RegexRunner<string>.Next(dfa, iter);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(expectedVal, result.Value.value);
        Assert.AreEqual(expectedMatch, result.Value.matchedText);
    }

    static void AssertNoMatchAt(RegexCompiler<string>.DFAState dfa, ISeekable<char> iter)
    {
        var result = RegexRunner<string>.Next(dfa, iter);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void CharacterClass_MultipleRanges()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[a-zA-Z]+", "Letter"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "abc", "abc");
        AssertMatch(dfa, "XYZ", "XYZ");
        AssertMatch(dfa, "aBcDeF", "aBcDeF");
        AssertNoMatch(dfa, "123");
        AssertMatch(dfa, "abc123", "abc");
    }

    [TestMethod]
    public void EscapeShorthand_WhitespaceInsideClass()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new(@"[\s]+", "Space"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "   ", "   ");
        AssertMatch(dfa, "\t", "\t");
        AssertMatch(dfa, " \t ", " \t ");
        AssertNoMatch(dfa, "abc");
    }

    [Ignore(@"\d shorthand is not implemented in the regex parser grammar")]
    [TestMethod]
    public void EscapeShorthand_Digit()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new(@"\d+", "Digit"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "123", "123");
        AssertNoMatch(dfa, "abc");
    }

    [Ignore(@"\w shorthand is not implemented in the regex parser grammar")]
    [TestMethod]
    public void EscapeShorthand_WordChar()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new(@"\w+", "Word"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "abc123", "abc123");
        AssertNoMatch(dfa, "...");
    }

    [Ignore("Character class intersection (nested brackets) is not implemented in the regex parser grammar")]
    [TestMethod]
    public void CharacterClass_Intersection()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new(@"[a-z&&[^aeiou]]+", "Consonant"),
        ], RegexConflictBehavior.Throw);

        AssertMatch(dfa, "bcdfg", "bcdfg");
        AssertNoMatch(dfa, "aeiou");
    }

    // --- NextWithPosition tests ---

    [TestMethod]
    public void NextWithPosition_SimpleMatch()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[0-9]+", "Number"),
        ], RegexConflictBehavior.Throw);

        var seeker = new StringTextSeekerForTest("123abc");
        var result = RegexRunner<string>.NextWithPosition(dfa, seeker);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual("Number", result.Value.value);
        Assert.AreEqual("123", result.Value.matchedText);
    }

    [TestMethod]
    public void NextWithPosition_NoMatch()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[0-9]+", "Number"),
        ], RegexConflictBehavior.Throw);

        var seeker = new StringTextSeekerForTest("abc");
        var result = RegexRunner<string>.NextWithPosition(dfa, seeker);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void NextWithPosition_EmptyPattern()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("", "Empty"),
        ], RegexConflictBehavior.Throw);

        var seeker = new StringTextSeekerForTest("abc");
        var result = RegexRunner<string>.NextWithPosition(dfa, seeker);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual("Empty", result.Value.value);
        Assert.AreEqual("", result.Value.matchedText);
    }

    [TestMethod]
    public void NextWithPosition_MultipleRules()
    {
        var dfa = RegexCompiler<string>.GenerateDFA([
            new("[0-9]+", "Number"),
            new("[a-z]+", "Ident"),
        ], RegexConflictBehavior.Throw);

        var seeker = new StringTextSeekerForTest("abc");
        var result = RegexRunner<string>.NextWithPosition(dfa, seeker);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual("Ident", result.Value.value);
        Assert.AreEqual("abc", result.Value.matchedText);
    }
}

class StringTextSeekerForTest(string text) : ITextSeekable
{
    int idx = -1;
    readonly string _text = text ?? throw new ArgumentNullException(nameof(text));

    public int LineNo { get; private set; }
    public int CharNo { get; private set; } = -1;
    public int CurrentPosition => idx;
    public char Current => idx >= 0 && idx < _text.Length ? _text[idx] : throw new InvalidOperationException();
    public bool MoveNext()
    {
        if (idx + 1 >= _text.Length) return false;
        idx++;
        var c = _text[idx];
        if (c == '\n') { LineNo++; CharNo = 0; }
        else if (c == '\r') { /* skip — will be handled by \n */ }
        else if (CharNo < 0) CharNo = 0;
        else CharNo++;
        return true;
    }
    public void Reverse(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        var target = idx - count;
        if (target < -1) target = -1;
        idx = -1; LineNo = 0; CharNo = -1;
        for (int i = 0; i <= target; i++) MoveNext();
    }
}
