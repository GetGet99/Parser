using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Get.Lexer;

class TestUtils
{
    public static Stream StreamOf(string str) => new MemoryStream(Encoding.UTF8.GetBytes(str));
    public static void AssertRange(ReadOnlySpan<byte> b, ReadOnlySpan<char> c)
    {
        Debug.Assert(b.Length == c.Length);
        for (int i = 0; i < b.Length; i++)
        {
            Debug.Assert(b[i] == c[i]);
        }
    }
    public static void AssertRange(ReadOnlySpan<char> b, ReadOnlySpan<char> c)
    {
        Debug.Assert(b.Length == c.Length);
        for (int i = 0; i < b.Length; i++)
        {
            Debug.Assert(b[i] == c[i]);
        }
    }
}
