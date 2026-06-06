using System.Text;

namespace Get.Lexer.Test;

[TestClass]
public class StreamSeekerTests
{
    static MemoryStream ToStream(string text) => new(Encoding.UTF8.GetBytes(text));

    [TestMethod]
    public void ForwardTracking()
    {
        var stream = ToStream("ab\ncd");
        var seeker = new StreamSeeker(stream);

        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(-1, seeker.CharNo);

        Assert.IsTrue(seeker.MoveNext());
        Assert.AreEqual('a', seeker.Current);
        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(0, seeker.CharNo);

        Assert.IsTrue(seeker.MoveNext());
        Assert.AreEqual('b', seeker.Current);
        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(1, seeker.CharNo);

        Assert.IsTrue(seeker.MoveNext());
        Assert.AreEqual('\n', seeker.Current);
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(0, seeker.CharNo);

        Assert.IsTrue(seeker.MoveNext());
        Assert.AreEqual('c', seeker.Current);
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(1, seeker.CharNo);

        Assert.IsTrue(seeker.MoveNext());
        Assert.AreEqual('d', seeker.Current);
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(2, seeker.CharNo);
    }

    [TestMethod]
    public void ReverseWithinLine()
    {
        var stream = ToStream("abcdef");
        var seeker = new StreamSeeker(stream);

        for (int i = 0; i < 5; i++)
            seeker.MoveNext();
        Assert.AreEqual('e', seeker.Current);
        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(4, seeker.CharNo);

        seeker.Reverse(2);

        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(2, seeker.CharNo);
    }

    [TestMethod]
    public void ReverseAcrossNewline()
    {
        var stream = ToStream("ab\ncd");
        var seeker = new StreamSeeker(stream);

        for (int i = 0; i < 3; i++)
            seeker.MoveNext();
        Assert.AreEqual('\n', seeker.Current);
        Assert.AreEqual(1, seeker.LineNo);
        Assert.AreEqual(0, seeker.CharNo);

        seeker.Reverse(1);

        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(2, seeker.CharNo);
    }

    [TestMethod]
    public void ResetWorks()
    {
        var stream = ToStream("hello world");
        var seeker = new StreamSeeker(stream);

        for (int i = 0; i < 6; i++)
            seeker.MoveNext();
        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(5, seeker.CharNo);

        seeker.Reset();

        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(-1, seeker.CharNo);

        seeker.MoveNext();
        Assert.AreEqual('h', seeker.Current);
        Assert.AreEqual(0, seeker.LineNo);
        Assert.AreEqual(0, seeker.CharNo);
    }
}
