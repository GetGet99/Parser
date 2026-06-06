using Get.PLShared;
using System.Diagnostics.CodeAnalysis;
namespace Get.Parser.Test;

[TestClass]
public partial class TestRegex
{
    [TestMethod]
    public void Test()
    {
        var dfa = GetDFA();
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"[\t \r\n]*")).Expression;
            if (expr.AssertIs<StarExpr>(out var star))
            {
                if (star.Expression.AssertIs<ClassExpr>(out var cls))
                {
                    Assert.IsFalse(cls.IsInverse);
                    AssertEqualsAnyOrder(cls.Chars, "\t \r\n");
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"[\r\n \t]*[\r\n]")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Assert.AreEqual(2, cat.Expressions.Length);
                if (cat.Expressions[0].AssertIs<StarExpr>(out var star))
                {
                    if (star.Expression.AssertIs<ClassExpr>(out var cls))
                    {
                        Assert.IsFalse(cls.IsInverse);
                        AssertEqualsAnyOrder(cls.Chars, "\r\n \t");
                    }
                }
                if (cat.Expressions[1].AssertIs<ClassExpr>(out var cls2))
                {
                    Assert.IsFalse(cls2.IsInverse);
                    AssertEqualsAnyOrder(cls2.Chars, "\r\n");
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@";")).Expression;
            if (expr.AssertIs<CharExpr>(out var ch))
            {
                Assert.AreEqual(';', ch.Char);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"[ \t]*")).Expression;
            if (expr.AssertIs<StarExpr>(out var star))
            {
                if (star.Expression.AssertIs<ClassExpr>(out var cls))
                {
                    Assert.IsFalse(cls.IsInverse);
                    AssertEqualsAnyOrder(cls.Chars, " \t");
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"[ \t]+")).Expression;
            if (expr.AssertIs<PlusExpr>(out var plus))
            {
                if (plus.Expression.AssertIs<ClassExpr>(out var cls))
                {
                    Assert.IsFalse(cls.IsInverse);
                    AssertEqualsAnyOrder(cls.Chars, " \t");
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Assert.AreEqual(0, cat.Expressions.Length);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"[a-zA-Z][a-zA-Z0-9]*")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Assert.AreEqual(2, cat.Expressions.Length);
                if (cat.Expressions[0].AssertIs<ClassExpr>(out var cls))
                {
                    Assert.IsFalse(cls.IsInverse);
                    AssertEqualsAnyOrder(cls.Chars, CharRange('a', 'z').Concat(CharRange('A', 'Z')));
                }
                if (cat.Expressions[1].AssertIs<StarExpr>(out var star))
                {
                    if (star.Expression.AssertIs<ClassExpr>(out var cls2))
                    {
                        Assert.IsFalse(cls2.IsInverse);
                        AssertEqualsAnyOrder(cls2.Chars,
                            CharRange('a', 'z')
                            .Concat(CharRange('A', 'Z'))
                            .Concat(CharRange('0', '9'))
                        );
                    }
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"func")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                AssertSequenceEqual(cat.Expressions, "func");
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"return")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                AssertSequenceEqual(cat.Expressions, "return");
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@":")).Expression;
            if (expr.AssertIs<CharExpr>(out var ch))
            {
                Assert.AreEqual(':', ch.Char);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"->")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                AssertSequenceEqual(cat.Expressions, "->");
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"\(")).Expression;
            if (expr.AssertIs<CharExpr>(out var ch))
            {
                Assert.AreEqual('(', ch.Char);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"\)")).Expression;
            if (expr.AssertIs<CharExpr>(out var ch))
            {
                Assert.AreEqual(')', ch.Char);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"true")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                AssertSequenceEqual(cat.Expressions, "true");
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"false")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                AssertSequenceEqual(cat.Expressions, "false");
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"(-|)[0-9][0-9_]*")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Assert.AreEqual(3, cat.Expressions.Length);
                if (cat.Expressions[0].AssertIs<AlternationExpr>(out var alt))
                {
                    Assert.AreEqual(2, alt.Expressions.Length);
                    if (alt.Expressions[0].AssertIs<CharExpr>(out var ch))
                    {
                        Assert.AreEqual('-', ch.Char);
                    }
                    if (alt.Expressions[1].AssertIs<CatExpr>(out var cat2))
                    {
                        Assert.AreEqual(0, cat2.Expressions.Length);
                    }
                }
                if (cat.Expressions[1].AssertIs<ClassExpr>(out var cls))
                {
                    Assert.IsFalse(cls.IsInverse);
                    AssertEqualsAnyOrder(cls.Chars, CharRange('0', '9'));
                }
                if (cat.Expressions[2].AssertIs<StarExpr>(out var star))
                {
                    if (star.Expression.AssertIs<ClassExpr>(out var cls2))
                    {
                        Assert.IsFalse(cls2.IsInverse);
                        AssertEqualsAnyOrder(cls2.Chars, CharRange('0', '9').Append('_'));
                    }
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"0x[0-9a-fA-F]+")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Assert.AreEqual(3, cat.Expressions.Length);
                AssertSequenceEqual(cat.Expressions[..2], "0x");
                if (cat.Expressions[2].AssertIs<PlusExpr>(out var plus))
                {
                    if (plus.Expression.AssertIs<ClassExpr>(out var cls))
                    {
                        Assert.IsFalse(cls.IsInverse);
                        AssertEqualsAnyOrder(cls.Chars,
                            CharRange('0', '9')
                            .Concat(CharRange('a', 'f'))
                            .Concat(CharRange('A', 'F')));
                    }
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"0b[01]+")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Assert.AreEqual(3, cat.Expressions.Length);
                AssertSequenceEqual(cat.Expressions[..2], "0b");
                if (cat.Expressions[2].AssertIs<PlusExpr>(out var plus))
                {
                    if (plus.Expression.AssertIs<ClassExpr>(out var cls))
                    {
                        Assert.IsFalse(cls.IsInverse);
                        AssertEqualsAnyOrder(cls.Chars, CharRange('0', '1'));
                    }
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens("""
                "([^\r\n\"\\]|(\\(n|t|r|\'|\")))*"
                """
            )).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Assert.AreEqual(3, cat.Expressions.Length);
                if (cat.Expressions[0].AssertIs<CharExpr>(out var ch))
                    Assert.AreEqual('"', ch.Char);
                if (cat.Expressions[1].AssertIs<StarExpr>(out var star))
                {
                    if (star.Expression.AssertIs<AlternationExpr>(out var alt))
                    {
                        Assert.AreEqual(2, alt.Expressions.Length);
                        if (alt.Expressions[0].AssertIs<ClassExpr>(out var cls2))
                        {
                            Assert.IsTrue(cls2.IsInverse);
                            AssertEqualsAnyOrder(cls2.Chars, "\r\n\"\\");
                        }
                        if (alt.Expressions[1].AssertIs<CatExpr>(out var cat2))
                        {
                            Assert.AreEqual(2, cat2.Expressions.Length);
                            if (cat2.Expressions[0].AssertIs<CharExpr>(out var ch2))
                            {
                                Assert.AreEqual('\\', ch2.Char);
                            }
                            if (cat2.Expressions[1].AssertIs<AlternationExpr>(out var alt2))
                            {
                                AssertSequenceEqual(alt2.Expressions, "ntr\'\"");
                            }
                        }
                    }
                }
                if (cat.Expressions[2].AssertIs<CharExpr>(out var ch3))
                    Assert.AreEqual('"', ch3.Char);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens("""
                '([^\r\n\'\\]|(\\(n|t|r|\'|\")))'
                """
            )).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Assert.AreEqual(3, cat.Expressions.Length);
                if (cat.Expressions[0].AssertIs<CharExpr>(out var ch))
                    Assert.AreEqual('\'', ch.Char);
                if (cat.Expressions[1].AssertIs<AlternationExpr>(out var alt))
                {
                    Assert.AreEqual(2, alt.Expressions.Length);
                    if (alt.Expressions[0].AssertIs<ClassExpr>(out var cls2))
                    {
                        Assert.IsTrue(cls2.IsInverse);
                        AssertEqualsAnyOrder(cls2.Chars, "\r\n\'\\");
                    }
                    if (alt.Expressions[1].AssertIs<CatExpr>(out var cat2))
                    {
                        Assert.AreEqual(2, cat2.Expressions.Length);
                        if (cat2.Expressions[0].AssertIs<CharExpr>(out var ch2))
                        {
                            Assert.AreEqual('\\', ch2.Char);
                        }
                        if (cat2.Expressions[1].AssertIs<AlternationExpr>(out var alt2))
                        {
                            AssertSequenceEqual(alt2.Expressions, "ntr\'\"");
                        }
                    }
                }
                if (cat.Expressions[2].AssertIs<CharExpr>(out var ch3))
                    Assert.AreEqual('\'', ch3.Char);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"|")).Expression;
            if (expr.AssertIs<AlternationExpr>(out var alt))
            {
                Assert.AreEqual(2, alt.Expressions.Length);
                if (alt.Expressions[0].AssertIs<CatExpr>(out var cat))
                    Assert.AreEqual(0, cat.Expressions.Length);
                if (alt.Expressions[1].AssertIs<CatExpr>(out var cat1))
                    Assert.AreEqual(0, cat1.Expressions.Length);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"|abcdefg")).Expression;
            if (expr.AssertIs<AlternationExpr>(out var alt))
            {
                Assert.AreEqual(2, alt.Expressions.Length);
                if (alt.Expressions[0].AssertIs<CatExpr>(out var cat))
                    Assert.AreEqual(0, cat.Expressions.Length);
                if (alt.Expressions[1].AssertIs<CatExpr>(out var cat1))
                    AssertSequenceEqual(cat1.Expressions, "abcdefg");
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"(|)")).Expression;
            if (expr.AssertIs<AlternationExpr>(out var alt))
            {
                Assert.AreEqual(2, alt.Expressions.Length);
                if (alt.Expressions[0].AssertIs<CatExpr>(out var cat))
                    Assert.AreEqual(0, cat.Expressions.Length);
                if (alt.Expressions[1].AssertIs<CatExpr>(out var cat1))
                    Assert.AreEqual(0, cat1.Expressions.Length);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"(|abcdefg)")).Expression;
            if (expr.AssertIs<AlternationExpr>(out var alt))
            {
                Assert.AreEqual(2, alt.Expressions.Length);
                if (alt.Expressions[0].AssertIs<CatExpr>(out var cat))
                    Assert.AreEqual(0, cat.Expressions.Length);
                if (alt.Expressions[1].AssertIs<CatExpr>(out var cat1))
                    AssertSequenceEqual(cat1.Expressions, "abcdefg");
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"/\*[^*]*\*+([^/*][^*]*\*+)\*/")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat1))
            {
                Assert.AreEqual(8, cat1.Expressions.Length);
                AssertSequenceEqual(cat1.Expressions[..2], "/*");
                if (cat1.Expressions[2].AssertIs<StarExpr>(out var star1))
                {
                    if (star1.Expression.AssertIs<ClassExpr>(out var class1))
                    {
                        Assert.IsTrue(class1.IsInverse);
                        AssertEqualsAnyOrder(class1.Chars, "*");
                    }
                }
                if (cat1.Expressions[3].AssertIs<PlusExpr>(out var plus1))
                {
                    if (plus1.Expression.AssertIs<CharExpr>(out var char1))
                    {
                        Assert.AreEqual('*', char1.Char);
                    }
                }
                if (cat1.Expressions[4].AssertIs<CatExpr>(out var cat2))
                {
                    Assert.AreEqual(3, cat2.Expressions.Length);
                    if (cat2.Expressions[0].AssertIs<ClassExpr>(out var class2))
                    {
                        Assert.IsTrue(class2.IsInverse);
                        AssertEqualsAnyOrder(class2.Chars, "/*");
                    }
                    if (cat2.Expressions[1].AssertIs<StarExpr>(out var star2))
                    {
                        if (star2.Expression.AssertIs<ClassExpr>(out var class3))
                        {
                            Assert.IsTrue(class3.IsInverse);
                            AssertEqualsAnyOrder(class3.Chars, "*");
                        }
                    }
                    if (cat2.Expressions[2].AssertIs<PlusExpr>(out var plus2))
                    {
                        if (plus2.Expression.AssertIs<CharExpr>(out var char2))
                        {
                            Assert.AreEqual('*', char2.Char);
                        }
                    }
                }
                AssertSequenceEqual(cat1.Expressions[^2..], "*/");
            }
        }
    }
    static void AssertSequenceEqual(RegexExpr[] exprs, string expected)
    {
        Assert.AreEqual(expected.Length, exprs.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            if (exprs[i].AssertIs<CharExpr>(out var ch))
            {
                Assert.AreEqual(expected[i], ch.Char);
            }
        }
    }
    static IEnumerable<ITerminalValue<char>> Tokens([StringSyntax(StringSyntaxAttribute.Regex)] string str)
    {
        foreach (var c in str)
        {
            yield return new TerminalValue(c, c switch
            {
                '(' => Terminal.OpenBracket,
                ')' => Terminal.CloseBracket,
                '[' => Terminal.OpenSquareBracket,
                ']' => Terminal.CloseSquareBracket,
                '|' => Terminal.Alternation,
                '*' => Terminal.Star,
                '+' => Terminal.Plus,
                '^' => Terminal.Caret,
                '\\' => Terminal.Backslash,
                '\'' => Terminal.SingleQuote,
                '\"' => Terminal.DoubleQuote,
                '.' => Terminal.Dot,
                '-' => Terminal.Dash,
                'n' or 'r' or 't' => Terminal.NormalCharOrSpecialEscape,
                _ => Terminal.Other
            });
        }
    }
    static void AssertEqualsAnyOrder(char[] c1, IEnumerable<char> c2)
    {
        Assert.IsTrue(c1.ToHashSet().SetEquals(c2));
    }
    record TerminalValue(char RawChar, Terminal Type) : ITerminalValue<char>
    {
        public Position Start { get; set; }
        public Position End { get; set; }
        ISyntaxElement ISyntaxElementValue.WithoutValue => new TerminalWrapper(Type);
        ITerminal ITerminalValue.WithoutValue => new TerminalWrapper(Type);

        char ISyntaxElementValue<char>.Value => RawChar;

        public override string ToString()
        {
            return $"{RawChar} ({Type})";
        }
    }
}
static class TestRegexExtensions
{
    public static bool AssertIs<T>(this object value, [NotNullWhen(true)] out T? casted)
    {
        if (value is T val)
        {
            casted = val;
            return true;
        }
        casted = default;
        return false;
    }
    public static T As<T>(this ISyntaxElementValue ele)
    {
        return (T)ele;
    }
}
