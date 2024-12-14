using Get.Lexer;
using Get.Lexer.Test;
using Get.PLShared;
using System.Diagnostics;
using System.Text;

RotatingBuffer.Test();
StreamSeeker.Test();

static StreamSeeker StreamOf(string text) => new(new MemoryStream(Encoding.UTF8.GetBytes(text)));
var stream = StreamOf("""
    1234
    + 123
    * 2
    - someVariable
    """);
var lexer = new CustomLexer(stream);
var enumerator = lexer.GetTokens().GetEnumerator();
enumerator.MoveNext();
Debug.Assert(enumerator.Current.TokenType == Terminals.Integer);
Debug.Assert(enumerator.Current is IToken<Terminals, int> a && a.Data is 1234);
Console.WriteLine(enumerator.Current);

enumerator.MoveNext();
Debug.Assert(enumerator.Current.TokenType == Terminals.Plus);
Console.WriteLine(enumerator.Current);

enumerator.MoveNext();
Debug.Assert(enumerator.Current.TokenType == Terminals.Integer);
Debug.Assert(enumerator.Current is IToken<Terminals, int> b && b.Data is 123);
Console.WriteLine(enumerator.Current);

enumerator.MoveNext();
Debug.Assert(enumerator.Current.TokenType == Terminals.Times);
Console.WriteLine(enumerator.Current);

enumerator.MoveNext();
Debug.Assert(enumerator.Current.TokenType == Terminals.Integer);
Debug.Assert(enumerator.Current is IToken<Terminals, int> c && c.Data is 2);
Console.WriteLine(enumerator.Current);

enumerator.MoveNext();
Debug.Assert(enumerator.Current.TokenType == Terminals.Minus);
Console.WriteLine(enumerator.Current);

enumerator.MoveNext();
Debug.Assert(enumerator.Current.TokenType == Terminals.Identifier);
Debug.Assert(enumerator.Current is IToken<Terminals, string> d && d.Data is "someVariable");
Console.WriteLine(enumerator.Current);
stream.Reset();
while (stream.MoveNext())
{
    Console.WriteLine($"{stream.LineNo}:{stream.CharNo} {stream.Current switch
    {
        '\r' => @"\r",
        '\n' => @"\n",
        _ => stream.Current.ToString(),
    }}");
}