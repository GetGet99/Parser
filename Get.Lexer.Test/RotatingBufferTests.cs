using System.Text;

namespace Get.Lexer.Test;

[TestClass]
public class RotatingBufferTests
{
    static MemoryStream ToStream(string text) => new(Encoding.UTF8.GetBytes(text));

    [TestMethod]
    public void BasicRead()
    {
        var stream = ToStream("abcdef");
        var buf = new RotatingBuffer(8);

        Assert.IsTrue(buf.Read(stream, 6));
        Assert.AreEqual(6, buf.TotalReadAmount);

        CollectionAssert.AreEqual("abcdef"u8.ToArray(), buf[0..6]);
        Assert.AreEqual((byte)'a', buf[0]);
        Assert.AreEqual((byte)'f', buf[5]);
    }

    [TestMethod]
    public void WrapAround()
    {
        var stream = ToStream("abcdefgh");
        var buf = new RotatingBuffer(8);

        buf.Read(stream, 4);
        buf.Read(stream, 4);

        Assert.AreEqual(8, buf.TotalReadAmount);
        CollectionAssert.AreEqual("abcdefgh"u8.ToArray(), buf[0..8]);
    }

    [TestMethod]
    public void GoBack()
    {
        var stream = ToStream("abcdef");
        var buf = new RotatingBuffer(8);

        buf.Read(stream, 6);
        buf.GoBack(stream, 2);

        Assert.AreEqual(4, buf.TotalReadAmount);
        CollectionAssert.AreEqual("abcd"u8.ToArray(), buf[0..4]);

        buf.Read(stream, 2);
        CollectionAssert.AreEqual("abcdef"u8.ToArray(), buf[0..6]);
    }

    [TestMethod]
    public void RangeAccess()
    {
        var stream = ToStream("hello world");
        var buf = new RotatingBuffer(16);

        buf.Read(stream, 11);

        CollectionAssert.AreEqual("world"u8.ToArray(), buf[^5..]);
        CollectionAssert.AreEqual("world"u8.ToArray(), buf[6..11]);
        CollectionAssert.AreEqual("hello"u8.ToArray(), buf[..5]);
    }

    [TestMethod]
    public void OverwrittenRangeThrows()
    {
        var stream = ToStream("abcdefghijklmnopqrstuvwxyz");
        var buf = new RotatingBuffer(8);

        buf.Read(stream, 8);  // fill buffer
        buf.Read(stream, 8);  // overwrite with next 8 bytes

        // Buffer now contains bytes 8..15; range 0..5 was overwritten
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = buf[0..5]);
    }
}
