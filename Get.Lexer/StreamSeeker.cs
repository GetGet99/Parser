using Get.RegexMachine;
using System.Diagnostics;
using System.Text;

namespace Get.Lexer;

// Code By Gemini
public partial class StreamSeeker(Stream stream, int ReadAmount = 256, int BufferSize = 1024) : ITextSeekable
{
    readonly RotatingBuffer buffer = new(BufferSize: BufferSize);
    private int bufferIndexCurrent = -1;
    readonly List<int> LineCharCount = [0];

    public int CurrentPosition => bufferIndexCurrent;
    public char Current => (char)buffer[bufferIndexCurrent];

    public bool MoveNext()
    {
        if (bufferIndexCurrent + 1 >= buffer.TotalReadAmount)
        {
            buffer.Read(stream, ReadAmount);
        }

        if (bufferIndexCurrent + 1 >= buffer.TotalReadAmount)
        {
            // no more characters
            return false;
        }
        char prevChar = bufferIndexCurrent >= 0 ? (char)buffer[bufferIndexCurrent] : '\0';
        bufferIndexCurrent++;
        if ((char)buffer[bufferIndexCurrent] is '\r' || ((char)buffer[bufferIndexCurrent] is '\n' && prevChar is not '\r'))
        {
            LineCharCount.Add(0);
            LineNo++;
            CharNo = 0;
        }
        else
        {
            CharNo++;
            LineCharCount[LineNo]++;
        }
        return true;
    }

    public void Reset()
    {
        LineNo = 0;
        CharNo = -1;
        bufferIndexCurrent = -1;
        buffer.GoBack(stream, buffer.TotalReadAmount);
    }

    public int LineNo { get; private set; }
    public int CharNo { get; private set; } = -1;

    public void Reverse(int characters)
    {
        if (CurrentPosition - characters is -1)
        {
            // allow resetting
            Reset();
            return;
        }
        if (characters > bufferIndexCurrent)
            throw new ArgumentOutOfRangeException();
        bufferIndexCurrent -= characters;
        buffer.GoBack(stream, characters);

        for (int i = 0; i < characters; i++)
        {
            if (CharNo == 0)
            {
                LineNo--;
                CharNo = LineCharCount[LineNo];
            }
            else
            {
                CharNo--;
                LineCharCount[LineNo]--;
            }
        }
    }
}