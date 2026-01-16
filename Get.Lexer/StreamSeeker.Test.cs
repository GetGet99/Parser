using System.Diagnostics;
using static Get.Lexer.TestUtils;
namespace Get.Lexer;

partial class StreamSeeker
{
    internal static void Test() => TestBasicMultiline();
    static void TestBasicMultiline()
    {
        // for differnet machines that use \r\n, \r, \n
        var nl = """
            a
            b
            """[1..^1];
        var nloffset = nl.Length - 1 /* \r\n = 1 or \r = 0 or \n = 0 */;
        var stream = StreamOf(
            """
            1234
            + 123
            * 2
            - someVariable
            """);
        var length = (int)stream.Length;
        var seeker = new StreamSeeker(stream);

        Debug.Assert(seeker.LineNo == 0);
        Debug.Assert(seeker.CharNo == -1);
        AssertRange(seeker.TestRead(5), $"1234{nl[0]}");
        Debug.Assert(seeker.LineNo == 1);
        Debug.Assert(seeker.CharNo == 0);
        //Debug.Assert(seeker.LineCharCount[0] == 4);
        //Debug.Assert(seeker.LineCharCount[1] == 0);
        seeker.Reverse(1);
        Debug.Assert(seeker.LineNo == 0);
        Debug.Assert(seeker.CharNo == 4);
        
        AssertRange(seeker.TestRead(nl.Length + 1), $"{nl}+");
        Debug.Assert(seeker.LineNo == 1);
        Debug.Assert(seeker.CharNo == nloffset + 1 /* "[nl]+" */);
        seeker.Reverse(1);
        Debug.Assert(seeker.LineNo == 1);
        Debug.Assert(seeker.CharNo == nloffset);

        AssertRange(seeker.TestRead(2), $"+ ");
        Debug.Assert(seeker.LineNo == 1);
        Debug.Assert(seeker.CharNo == nloffset + 2 /* "[nl]+ " */);
        seeker.Reverse(1);
        Debug.Assert(seeker.LineNo == 1);
        Debug.Assert(seeker.CharNo == nloffset + 1 /* "[nl]+" */);

        AssertRange(seeker.TestRead(2), $" 1");
        Debug.Assert(seeker.LineNo == 1);
        Debug.Assert(seeker.CharNo == nloffset + 3 /* "[nl]+ 1" */);
        seeker.Reverse(1);
        Debug.Assert(seeker.LineNo == 1);
        Debug.Assert(seeker.CharNo == nloffset + 2 /* "[nl]+ " */);

        AssertRange(seeker.TestRead(4), $"123{nl[0]}");
        Debug.Assert(seeker.LineNo == 2);
        Debug.Assert(seeker.CharNo == 0);
        //Debug.Assert(seeker.LineCharCount[1] == nloffset + 5);
        //Debug.Assert(seeker.LineCharCount[2] == 0);
        seeker.Reverse(1);
        Debug.Assert(seeker.LineNo == 1);
        Debug.Assert(seeker.CharNo == nloffset + 5 /* "[nl]+ 123" */);
    }
    char[] TestRead(int length)
    {
        char[] toRet = new char[length];
        for (int i = 0; i < length; i++)
        {
            MoveNext();
            toRet[i] = Current;
        }
        return toRet;
    }
}
