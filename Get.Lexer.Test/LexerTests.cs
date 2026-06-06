using Get.PLShared;
using System.Text;

namespace Get.Lexer.Test;

[TestClass]
public class LexerTests
{
    static MemoryStream ToStream(string text) => new(Encoding.UTF8.GetBytes(text));

    [TestMethod]
    public void CustomLexerSourceGen_TokenizesExpression()
    {
        var stream = new StreamSeeker(ToStream("1234+123*2-someVariable"));
        var lexer = new CustomLexerSourceGen(stream);
        var tokens = lexer.GetTokens().ToArray();

        Assert.AreEqual(7, tokens.Length, $"Expected 7 tokens, got {tokens.Length}");

        Assert.AreEqual(CustomLexerSourceGen.Terminals.Integer, tokens[0].TokenType);
        Assert.IsTrue(tokens[0] is IToken<CustomLexerSourceGen.Terminals, int> a && a.Data is 1234);

        Assert.AreEqual(CustomLexerSourceGen.Terminals.Plus, tokens[1].TokenType);

        Assert.AreEqual(CustomLexerSourceGen.Terminals.Integer, tokens[2].TokenType);
        Assert.IsTrue(tokens[2] is IToken<CustomLexerSourceGen.Terminals, int> b && b.Data is 123);

        Assert.AreEqual(CustomLexerSourceGen.Terminals.Times, tokens[3].TokenType);

        Assert.AreEqual(CustomLexerSourceGen.Terminals.Integer, tokens[4].TokenType);
        Assert.IsTrue(tokens[4] is IToken<CustomLexerSourceGen.Terminals, int> c && c.Data is 2);

        Assert.AreEqual(CustomLexerSourceGen.Terminals.Minus, tokens[5].TokenType);

        Assert.AreEqual(CustomLexerSourceGen.Terminals.Identifier, tokens[6].TokenType);
        Assert.IsTrue(tokens[6] is IToken<CustomLexerSourceGen.Terminals, string> d && d.Data is "someVariable");
    }

    [TestMethod]
    public void StreamSeeker_TracksPosition()
    {
        var seeker = new StreamSeeker(ToStream("1234\n+ 123\n* 2\n- someVariable\n"));

        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(-1, seeker.CharNo);

        // Read "1234\n"
        for (int i = 0; i < 4; i++)
        {
            Assert.IsTrue(seeker.MoveNext());
            Assert.AreEqual(0, seeker.LineNo);
            Assert.AreEqual(i, seeker.CharNo);
        }
        Assert.IsTrue(seeker.MoveNext()); // \n
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(0, seeker.CharNo);

        // Reverse back to '4': CharNo becomes lineLengths[0] = 4
        seeker.Reverse(1);
        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(4, seeker.CharNo);

        // Read forward again past \n and "+ "
        Assert.IsTrue(seeker.MoveNext()); // \n
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(0, seeker.CharNo);
        Assert.IsTrue(seeker.MoveNext()); // +
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(1, seeker.CharNo);
        Assert.IsTrue(seeker.MoveNext()); // ' '
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(2, seeker.CharNo);

        // Read rest of "123\n"
        Assert.IsTrue(seeker.MoveNext()); // 1
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(3, seeker.CharNo);
        Assert.IsTrue(seeker.MoveNext()); // 2
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(4, seeker.CharNo);
        Assert.IsTrue(seeker.MoveNext()); // 3
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(5, seeker.CharNo);
        Assert.IsTrue(seeker.MoveNext()); // \n
        Assert.AreEqual(2, seeker.LineNo);
        Assert.AreEqual(0, seeker.CharNo);
    }
}
