using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
namespace Get.Parser.Test;
static partial class TestRegex
{
    public static void Test()
    {
        var dfa = GetDFA();
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"[\t \r\n]*")).Expression;
            if (expr.AssertIs<StarExpr>(out var star))
            {
                if (star.Expression.AssertIs<ClassExpr>(out var cls))
                {
                    Debug.Assert(cls.IsInverse is false);
                    AssertEqualsAnyOrder(cls.Chars, "\t \r\n"); // these should compare with unescaped version
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"[\r\n \t]*[\r\n]")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Debug.Assert(cat.Expressions.Length is 2);
                if (cat.Expressions[0].AssertIs<StarExpr>(out var star))
                {
                    if (star.Expression.AssertIs<ClassExpr>(out var cls))
                    {
                        Debug.Assert(cls.IsInverse is false);
                        AssertEqualsAnyOrder(cls.Chars, "\r\n \t");
                    }
                }
                if (cat.Expressions[1].AssertIs<ClassExpr>(out var cls2))
                {
                    Debug.Assert(cls2.IsInverse is false);
                    AssertEqualsAnyOrder(cls2.Chars, "\r\n");
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@";")).Expression;
            if (expr.AssertIs<CharExpr>(out var ch))
            {
                Debug.Assert(ch.Char is ';');
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"[ \t]*")).Expression;
            if (expr.AssertIs<StarExpr>(out var star))
            {
                if (star.Expression.AssertIs<ClassExpr>(out var cls))
                {
                    Debug.Assert(cls.IsInverse is false);
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
                    Debug.Assert(cls.IsInverse is false);
                    AssertEqualsAnyOrder(cls.Chars, " \t");
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Debug.Assert(cat.Expressions.Length is 0);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"[a-zA-Z][a-zA-Z0-9]*")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Debug.Assert(cat.Expressions.Length is 2);
                if (cat.Expressions[0].AssertIs<ClassExpr>(out var cls))
                {
                    Debug.Assert(cls.IsInverse is false);
                    AssertEqualsAnyOrder(cls.Chars, CharRange('a', 'z').Concat(CharRange('A', 'Z')));
                }
                if (cat.Expressions[1].AssertIs<StarExpr>(out var star))
                {
                    if (star.Expression.AssertIs<ClassExpr>(out var cls2))
                    {
                        Debug.Assert(cls2.IsInverse is false);
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
                Debug.Assert(ch.Char is ':');
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
                Debug.Assert(ch.Char is '(');
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"\)")).Expression;
            if (expr.AssertIs<CharExpr>(out var ch))
            {
                Debug.Assert(ch.Char is ')');
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
                Debug.Assert(cat.Expressions.Length is 3);
                if (cat.Expressions[0].AssertIs<AlternationExpr>(out var alt))
                {
                    Debug.Assert(alt.Expressions.Length is 2);
                    if (alt.Expressions[0].AssertIs<CharExpr>(out var ch))
                    {
                        Debug.Assert(ch.Char is '-');
                    }
                    if (alt.Expressions[1].AssertIs<CatExpr>(out var cat2))
                    {
                        Debug.Assert(cat2.Expressions.Length is 0);
                    }
                }
                if (cat.Expressions[1].AssertIs<ClassExpr>(out var cls))
                {
                    Debug.Assert(cls.IsInverse is false);
                    AssertEqualsAnyOrder(cls.Chars, CharRange('0', '9'));
                }
                if (cat.Expressions[2].AssertIs<StarExpr>(out var star))
                {
                    if (star.Expression.AssertIs<ClassExpr>(out var cls2))
                    {
                        Debug.Assert(cls2.IsInverse is false);
                        AssertEqualsAnyOrder(cls2.Chars, CharRange('0', '9').Append('_'));
                    }
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"0x[0-9a-fA-F]+")).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Debug.Assert(cat.Expressions.Length is 3);
                AssertSequenceEqual(cat.Expressions[..2], "0x");
                if (cat.Expressions[2].AssertIs<PlusExpr>(out var plus))
                {
                    if (plus.Expression.AssertIs<ClassExpr>(out var cls))
                    {
                        Debug.Assert(cls.IsInverse is false);
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
                Debug.Assert(cat.Expressions.Length is 3);
                AssertSequenceEqual(cat.Expressions[..2], "0b");
                if (cat.Expressions[2].AssertIs<PlusExpr>(out var plus))
                {
                    if (plus.Expression.AssertIs<ClassExpr>(out var cls))
                    {
                        Debug.Assert(cls.IsInverse is false);
                        AssertEqualsAnyOrder(cls.Chars, CharRange('0', '1'));
                    }
                }
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens("""
                "([^\r\n\"\\]|(\\(n|t|r|\'|\")))*"
                """
                // should've used class instead of several alternations for second one but I wasn't smart
                // i guess it's a good test case
            )).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Debug.Assert(cat.Expressions.Length is 3); // "(something)*" counting quotes
                if (cat.Expressions[0].AssertIs<CharExpr>(out var ch))
                    Debug.Assert(ch.Char is '"');
                if (cat.Expressions[1].AssertIs<StarExpr>(out var star))
                {
                    if (star.Expression.AssertIs<AlternationExpr>(out var alt))
                    {
                        Debug.Assert(alt.Expressions.Length is 2);
                        if (alt.Expressions[0].AssertIs<ClassExpr>(out var cls2))
                        {
                            Debug.Assert(cls2.IsInverse is true);
                            AssertEqualsAnyOrder(cls2.Chars, "\r\n\"\\");
                        }
                        if (alt.Expressions[1].AssertIs<CatExpr>(out var cat2))
                        {
                            Debug.Assert(cat2.Expressions.Length is 2);
                            if (cat2.Expressions[0].AssertIs<CharExpr>(out var ch2))
                            {
                                Debug.Assert(ch2.Char is '\\');
                            }
                            if (cat2.Expressions[1].AssertIs<AlternationExpr>(out var alt2))
                            {
                                // at least i can still reuse this for alternation
                                AssertSequenceEqual(alt2.Expressions, "ntr\'\"");
                            }
                        }
                    }
                }
                if (cat.Expressions[2].AssertIs<CharExpr>(out var ch3))
                    Debug.Assert(ch3.Char is '"');
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens("""
                '([^\r\n\'\\]|(\\(n|t|r|\'|\")))'
                """
            // again should've used class instead of several alternations
            )).Expression;
            if (expr.AssertIs<CatExpr>(out var cat))
            {
                Debug.Assert(cat.Expressions.Length is 3); // "(something)*" counting quotes
                if (cat.Expressions[0].AssertIs<CharExpr>(out var ch))
                    Debug.Assert(ch.Char is '\'');
                if (cat.Expressions[1].AssertIs<AlternationExpr>(out var alt))
                {
                    Debug.Assert(alt.Expressions.Length is 2);
                    if (alt.Expressions[0].AssertIs<ClassExpr>(out var cls2))
                    {
                        Debug.Assert(cls2.IsInverse is true);
                        AssertEqualsAnyOrder(cls2.Chars, "\r\n\'\\");
                    }
                    if (alt.Expressions[1].AssertIs<CatExpr>(out var cat2))
                    {
                        Debug.Assert(cat2.Expressions.Length is 2);
                        if (cat2.Expressions[0].AssertIs<CharExpr>(out var ch2))
                        {
                            Debug.Assert(ch2.Char is '\\');
                        }
                        if (cat2.Expressions[1].AssertIs<AlternationExpr>(out var alt2))
                        {
                            // at least i can still reuse this for alternation
                            AssertSequenceEqual(alt2.Expressions, "ntr\'\"");
                        }
                    }
                }
                if (cat.Expressions[2].AssertIs<CharExpr>(out var ch3))
                    Debug.Assert(ch3.Char is '\'');
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"|")).Expression;
            if (expr.AssertIs<AlternationExpr>(out var alt))
            {
                Debug.Assert(alt.Expressions.Length is 2);
                if (alt.Expressions[0].AssertIs<CatExpr>(out var cat))
                    Debug.Assert(cat.Expressions.Length is 0);
                if (alt.Expressions[1].AssertIs<CatExpr>(out var cat1))
                    Debug.Assert(cat1.Expressions.Length is 0);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"|abcdefg")).Expression;
            if (expr.AssertIs<AlternationExpr>(out var alt))
            {
                Debug.Assert(alt.Expressions.Length is 2);
                if (alt.Expressions[0].AssertIs<CatExpr>(out var cat))
                    Debug.Assert(cat.Expressions.Length is 0);
                if (alt.Expressions[1].AssertIs<CatExpr>(out var cat1))
                    AssertSequenceEqual(cat1.Expressions, "abcdefg");
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"(|)")).Expression;
            if (expr.AssertIs<AlternationExpr>(out var alt))
            {
                Debug.Assert(alt.Expressions.Length is 2);
                if (alt.Expressions[0].AssertIs<CatExpr>(out var cat))
                    Debug.Assert(cat.Expressions.Length is 0);
                if (alt.Expressions[1].AssertIs<CatExpr>(out var cat1))
                    Debug.Assert(cat1.Expressions.Length is 0);
            }
        }
        {
            var expr = LRParserRunner<FinalRegex>.Parse(dfa, Tokens(@"(|abcdefg)")).Expression;
            if (expr.AssertIs<AlternationExpr>(out var alt))
            {
                Debug.Assert(alt.Expressions.Length is 2);
                if (alt.Expressions[0].AssertIs<CatExpr>(out var cat))
                    Debug.Assert(cat.Expressions.Length is 0);
                if (alt.Expressions[1].AssertIs<CatExpr>(out var cat1))
                    AssertSequenceEqual(cat1.Expressions, "abcdefg");
            }
        }
    }
    static void AssertSequenceEqual(RegexExpr[] exprs, string expected)
    {
        Debug.Assert(exprs.Length == expected.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            if (exprs[i].AssertIs<CharExpr>(out var ch))
            {
                Debug.Assert(ch.Char == expected[i]);
            }
        }
    }
    static bool AssertIs<T>(this object value, [NotNullWhen(true)] out T? casted)
    {
        if (value is T val)
        {
            casted = val;
            return true;
        }
        Debugger.Break();
        casted = default;
        return false;
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
    static bool AssertEqualsAnyOrder(char[] c1, IEnumerable<char> c2)
    {
        bool result = c1.ToHashSet().SetEquals(c2);
        Debug.Assert(result);
        return true;
    }
    record TerminalValue(char RawChar, Terminal Type) : ITerminalValue<char>
    {
        ISyntaxElement ISyntaxElementValue.WithoutValue => new TerminalWrapper(Type);
        ITerminal ITerminalValue.WithoutValue => new TerminalWrapper(Type);

        char ISyntaxElementValue<char>.Value => RawChar;

        public override string ToString()
        {
            return $"{RawChar} ({Type})";
        }
    }
}
