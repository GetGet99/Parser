using System.Diagnostics;
using System.Text;
using static Get.Lexer.TestUtils;
namespace Get.Lexer;

partial class RotatingBuffer
{

    internal static void Test()
    {
        TestBasic();
    }
    static void TestBasic()
    {
        const int BufSize = 1024;
        const int ReadSize = 256;
        var stream = StreamOf("1234 + 123 * 2 - someVariable");
        var length = stream.Length;
        var buf = new RotatingBuffer(BufSize);
        Debug.Assert(stream.Position == 0);
        buf.Read(stream, ReadSize);
        AssertRange(buf[..5], "1234 ");
        buf.GoBack(stream, (int)length - 4);
        Debug.Assert(buf.start == 0);
        Debug.Assert(buf.end == 4);
        AssertRange(buf.buffer.AsSpan()[..(int)length], "1234 + 123 * 2 - someVariable");
    }
}
