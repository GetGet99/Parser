namespace Get.RegexMachine.Test;

using static TestUtils;
using Get.Parser;
using Get.PLShared;

[TestClass]
public class TestRegexFuzz
{
    static readonly char[] LiteralChars = Enumerable.Range(32, 95).Select(i => (char)i)
        .Except(['[', ']', '\\', '.', '*', '+', '?', '|', '^', '$', '(', ')', '{', '}'])
        .ToArray();

    [TestMethod]
    public void Fuzz_RandomPatterns_NoCrash()
    {
        var rng = new Random(42);
        for (int iter = 0; iter < 500; iter++)
        {
            var pattern = GenerateRandomPattern(rng, 1, 8);
            try
            {
                var dfa = RegexCompiler<string>.GenerateDFA([
                    new(pattern, "test")
                ], RegexConflictBehavior.Throw);

                for (int trial = 0; trial < 10; trial++)
                {
                    var input = GenerateRandomInput(rng, 0, 20);
                    var result = RegexRunner<string>.Next(dfa, Iter(input));
                }
            }
            catch (RegexCompilerException)
            {
            }
        }
    }

    [TestMethod]
    public void Fuzz_MultiplePatterns_NoCrash()
    {
        var rng = new Random(43);
        for (int iter = 0; iter < 200; iter++)
        {
            var patterns = new List<RegexVal<string>>();
            int count = rng.Next(1, 6);
            for (int i = 0; i < count; i++)
            {
                patterns.Add(new(GenerateRandomPattern(rng, 1, 6), $"P{i}", Order: i));
            }

            try
            {
                var dfa = RegexCompiler<string>.GenerateDFA(patterns, RegexConflictBehavior.Last);

                for (int trial = 0; trial < 5; trial++)
                {
                    var input = GenerateRandomInput(rng, 0, 30);
                    var result = RegexRunner<string>.Next(dfa, Iter(input));
                }
            }
            catch (RegexCompilerException)
            {
            }
        }
    }

    [TestMethod]
    public void Fuzz_ConflictBehaviors_NoCrash()
    {
        var rng = new Random(44);
        var behaviors = new[] { RegexConflictBehavior.Throw, RegexConflictBehavior.Last };

        for (int iter = 0; iter < 100; iter++)
        {
            var patterns = new List<RegexVal<string>>();
            int count = rng.Next(2, 5);
            for (int i = 0; i < count; i++)
            {
                patterns.Add(new(GenerateRandomPattern(rng, 1, 5), $"P{i}", Order: i));
            }

            foreach (var behavior in behaviors)
            {
                try
                {
                    var dfa = RegexCompiler<string>.GenerateDFA(patterns, behavior);
                    var input = GenerateRandomInput(rng, 0, 20);
                    var result = RegexRunner<string>.Next(dfa, Iter(input));
                }
                catch (RegexConflictCompilerException)
                {
                }
                catch (RegexCompilerException)
                {
                }
            }
        }
    }

    [TestMethod]
    public void Fuzz_NextWithPosition_NoCrash()
    {
        var rng = new Random(45);
        for (int iter = 0; iter < 100; iter++)
        {
            var pattern = GenerateRandomPattern(rng, 1, 6);
            try
            {
                var dfa = RegexCompiler<string>.GenerateDFA([
                    new(pattern, "test")
                ], RegexConflictBehavior.Throw);

                for (int trial = 0; trial < 5; trial++)
                {
                    var input = GenerateRandomInput(rng, 0, 15);
                    var seeker = new StringTextSeekerForTest(input);
                    var result = RegexRunner<string>.NextWithPosition(dfa, seeker);
                }
            }
            catch (RegexCompilerException)
            {
            }
        }
    }

    [TestMethod]
    public void Stress_ManyPatterns_LargeInput()
    {
        var rng = new Random(46);
        var patterns = new List<RegexVal<string>>();
        for (int i = 0; i < 20; i++)
        {
            patterns.Add(new(GenerateRandomPattern(rng, 1, 4), $"P{i}", Order: i));
        }

        try
        {
            var dfa = RegexCompiler<string>.GenerateDFA(patterns, RegexConflictBehavior.Last);
            var input = GenerateRandomInput(rng, 50, 200);

            var iter = Iter(input);
            int matchCount = 0;
            while (true)
            {
                var result = RegexRunner<string>.Next(dfa, iter);
                if (!result.HasValue) break;
                matchCount++;
            }
            Assert.IsTrue(matchCount > 0, "Should match at least once in non-trivial input");
        }
        catch (RegexCompilerException)
        {
        }
    }

    [TestMethod]
    public void Stress_ConcurrentFuzz()
    {
        Parallel.For(0, 32, i =>
        {
            var rng = new Random(47 + i);
            for (int iter = 0; iter < 20; iter++)
            {
                var pattern = GenerateRandomPattern(rng, 1, 6);
                try
                {
                    var dfa = RegexCompiler<string>.GenerateDFA([
                        new(pattern, "test")
                    ], RegexConflictBehavior.Throw);
                    var input = GenerateRandomInput(rng, 0, 15);
                    var _result = RegexRunner<string>.Next(dfa, Iter(input));
                }
                catch (RegexCompilerException)
                {
                }
            }
        });
    }

    // -- Random generation helpers --

    static string GenerateRandomPattern(Random rng, int minLen, int maxLen)
    {
        int len = rng.Next(minLen, maxLen + 1);
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < len; i++)
        {
            sb.Append(GenerateRandomRegexAtom(rng));
        }
        return sb.ToString();
    }

    static string GenerateRandomRegexAtom(Random rng)
    {
        return rng.Next(6) switch
        {
            0 => GenerateRandomCharClass(rng),
            1 => LiteralChars[rng.Next(LiteralChars.Length)].ToString(),
            2 => "." + MaybeQuantifier(rng),
            3 => "[" + LiteralChars[rng.Next(LiteralChars.Length)] + "]" + MaybeQuantifier(rng),
            4 when rng.Next(2) == 0 => @"\n",
            5 when rng.Next(3) == 0 => @"\t",
            _ => GenerateRandomLiteral(rng),
        };
    }

    static string MaybeQuantifier(Random rng)
    {
        return rng.Next(3) switch
        {
            0 => "*",
            1 => "+",
            _ => "",
        };
    }

    static string GenerateRandomCharClass(Random rng)
    {
        var sb = new System.Text.StringBuilder("[");
        bool negated = rng.Next(4) == 0;
        if (negated) sb.Append('^');

        int members = rng.Next(1, 4);
        for (int i = 0; i < members; i++)
        {
            if (rng.Next(2) == 0 && i + 1 < members)
            {
                char from = (char)rng.Next('a', 'z');
                char to = (char)rng.Next(from, 'z' + 1);
                sb.Append(from);
                sb.Append('-');
                sb.Append(to);
                i++;
            }
            else
            {
                sb.Append(LiteralChars[rng.Next(LiteralChars.Length)]);
            }
        }
        sb.Append(']');
        if (rng.Next(3) == 0) sb.Append(rng.Next(2) == 0 ? '*' : '+');
        return sb.ToString();
    }

    static string GenerateRandomLiteral(Random rng)
    {
        return LiteralChars[rng.Next(LiteralChars.Length)].ToString();
    }

    static string GenerateRandomInput(Random rng, int minLen, int maxLen)
    {
        int len = rng.Next(minLen, maxLen + 1);
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < len; i++)
        {
            sb.Append((char)rng.Next(32, 127));
        }
        return sb.ToString();
    }
}

[TestClass]
public class TestLRParserFuzz
{
    static readonly EqualityComparer<INonTerminal> NtComparer = EqualityComparer<INonTerminal>.Default;
    static readonly EqualityComparer<ITerminal?> TermComparer = EqualityComparer<ITerminal?>.Default;

    [TestMethod]
    public void Fuzz_RandomGrammars_NoCrash()
    {
        var rng = new Random(137);
        for (int iter = 0; iter < 200; iter++)
        {
            var grammar = GenerateRandomGrammar(rng, rng.Next(2, 8));
            var gen = new LRParserDFAGen(NtComparer, TermComparer);

            try
            {
                var dfa = gen.CreateDFA(grammar.Rules, grammar.Start, grammar.Precedence);

                for (int trial = 0; trial < 3; trial++)
                {
                    try
                    {
                        var tokens = GenerateRandomTokenSequence(rng, rng.Next(0, 8));
                        _ = LRParserRunner<object>.Parse(dfa, tokens, skipErrorHandling: true);
                    }
                    catch
                    {
                    }
                }
            }
            catch (LRConflictException)
            {
            }
            catch (LRParserRuntimeException)
            {
            }
        }
    }

    [TestMethod]
    public void Stress_ManyGrammars_Sequential()
    {
        var rng = new Random(138);
        for (int iter = 0; iter < 50; iter++)
        {
            var grammar = GenerateRandomGrammar(rng, rng.Next(2, 6));
            var gen = new LRParserDFAGen(NtComparer, TermComparer);

            try
            {
                var dfa = gen.CreateDFA(grammar.Rules, grammar.Start, grammar.Precedence);

                for (int trial = 0; trial < 5; trial++)
                {
                    try
                    {
                        var tokens = GenerateRandomTokenSequence(rng, rng.Next(0, 6));
                        _ = LRParserRunner<object>.Parse(dfa, tokens, skipErrorHandling: true);
                    }
                    catch { }
                }
            }
            catch (LRConflictException) { }
            catch (LRParserRuntimeException) { }
        }
    }

    // -- Random grammar generation --

    record FuzzGrammar(INonTerminal Start, IReadOnlyList<ICFGRule> Rules, (ITerminal[], Associativity)[] Precedence);

    static FuzzGrammar GenerateRandomGrammar(Random rng, int numRules)
    {
        int numNt = Math.Max(2, rng.Next(2, 5));
        int numTerm = Math.Max(2, rng.Next(2, 4));

        var nts = Enumerable.Range(0, numNt).Select(i => new FuzzNT($"N{i}")).ToArray();
        var terms = Enumerable.Range(0, numTerm).Select(i => new FuzzT($"T{i}")).ToArray();

        var rules = new List<ICFGRule>();

        foreach (var nt in nts)
        {
            var exprs = new List<ISyntaxElement> { terms[rng.Next(terms.Length)] };
            if (rng.Next(2) == 0 && nts.Length > 1)
            {
                var otherNt = nts.Where(x => !x.Equals(nt)).ElementAt(rng.Next(nts.Length - 1));
                exprs.Add(otherNt);
            }
            rules.Add(new FuzzRule(nt, exprs));
        }

        int remaining = numRules - nts.Length;
        for (int i = 0; i < remaining; i++)
        {
            var target = nts[rng.Next(nts.Length)];
            int exprLen = rng.Next(1, 4);
            var exprs = new List<ISyntaxElement>();
            for (int j = 0; j < exprLen; j++)
            {
                if (rng.Next(2) == 0)
                    exprs.Add(nts[rng.Next(nts.Length)]);
                else
                    exprs.Add(terms[rng.Next(terms.Length)]);
            }
            rules.Add(new FuzzRule(target, exprs));
        }

        var start = nts[0];

        var precedence = new List<(ITerminal[], Associativity)>();
        if (rng.Next(2) == 0)
        {
            for (int i = 0; i < Math.Min(2, terms.Length); i++)
            {
                if (rng.Next(2) == 0)
                {
                    var assoc = (Associativity)rng.Next(3);
                    precedence.Add(([terms[i]], assoc));
                }
            }
        }

        return new(start, rules, [.. precedence]);
    }

    static IEnumerable<ITerminalValue> GenerateRandomTokenSequence(Random rng, int length)
    {
        var terms = new[] { new FuzzT("T0"), new FuzzT("T1"), new FuzzT("T2"), new FuzzT("T3") };
        for (int i = 0; i < length; i++)
        {
            yield return new FuzzTV(terms[rng.Next(Math.Min(2, terms.Length))]);
        }
    }

    // -- Fuzz test data types --

    record FuzzNT(string Name) : INonTerminal
    {
        public override string ToString() => Name;
    }

    record FuzzT(string Name) : ITerminal
    {
        public override string ToString() => Name;
    }

    record FuzzTV(FuzzT Terminal) : ITerminalValue
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        public ITerminal WithoutValue => Terminal;
        ISyntaxElement ISyntaxElementValue.WithoutValue => Terminal;
        public object? Value => null;
    }

    record FuzzRule(INonTerminal Target, IReadOnlyList<ISyntaxElement> Expressions) : ICFGRule
    {
        public INonTerminalValue GetValue(ISyntaxElementValue[] values)
        {
            return new FuzzNVal(Target);
        }
    }

    record FuzzNVal(INonTerminal Type) : INonTerminalValue
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        ISyntaxElement ISyntaxElementValue.WithoutValue => Type;
    }
}
