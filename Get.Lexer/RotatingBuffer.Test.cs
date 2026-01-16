using System.Diagnostics;
using static Get.Lexer.TestUtils;
namespace Get.Lexer;

partial class RotatingBuffer
{
    public static void Test()
    {
        TestBasicRead();
        TestWrapAround();
        //TestOverwrite();
        TestGoBack();
        TestRangeAccess();
        TestOverwrittenRangeThrows();
    }
    static void TestBasicRead()
    {
        var stream = StreamOf("abcdef");
        var buf = new RotatingBuffer(8);

        Debug.Assert(buf.Read(stream, 6));
        Debug.Assert(buf.TotalReadAmount == 6);

        AssertRange(buf[0..6], "abcdef");
        Debug.Assert(buf[0] == (byte)'a');
        Debug.Assert(buf[5] == (byte)'f');
    }

    static void TestWrapAround()
    {
        var stream = StreamOf("abcdefgh");
        var buf = new RotatingBuffer(8);

        buf.Read(stream, 4);
        buf.Read(stream, 4);

        Debug.Assert(buf.TotalReadAmount == 8);
        AssertRange(buf[0..8], "abcdefgh");
    }
    //static void TestOverwrite()
    //{
    //    var stream = StreamOf("0123456789");
    //    var buf = new RotatingBuffer(4);

    //    buf.Read(stream, 10);

    //    Debug.Assert(buf.TotalReadAmount == 10);

    //    // Only last 4 bytes should remain
    //    AssertRange(buf[6..10], "6789");
    //}
    static void TestGoBack()
    {
        var stream = StreamOf("abcdef");
        var buf = new RotatingBuffer(8);

        buf.Read(stream, 6);
        buf.GoBack(stream, 2);

        Debug.Assert(buf.TotalReadAmount == 4);
        AssertRange(buf[0..4], "abcd");

        // Re-read
        buf.Read(stream, 2);
        AssertRange(buf[0..6], "abcdef");
    }
    static void TestRangeAccess()
    {
        var stream = StreamOf("hello world");
        var buf = new RotatingBuffer(16);

        buf.Read(stream, 11);

        AssertRange(buf[^5..], "world");
        AssertRange(buf[6..11], "world");
        AssertRange(buf[..5], "hello");
    }
    static void TestOverwrittenRangeThrows()
    {
        var stream = StreamOf("abcdefghijklmnopqrstuvwxyz");
        var buf = new RotatingBuffer(8);

        buf.Read(stream, 26);

        bool threw = false;
        try
        {
            _ = buf[0..5];
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Debug.Assert(threw, "Expected overwritten range to throw.");
    }
}
