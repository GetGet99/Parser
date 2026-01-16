using Get.RegexMachine;

namespace Get.Lexer;

public partial class StreamSeeker : ITextSeekable
{
    private readonly Stream stream;
    private readonly RotatingBuffer buffer;
    private readonly int readAmount;

    private int bufferIndexCurrent = -1;
    private readonly List<int> lineLengths = new() { 0 };

    public int LineNo { get; private set; }
    public int CharNo { get; private set; } = -1;

    public int CurrentPosition => bufferIndexCurrent;

    public char Current =>
        bufferIndexCurrent >= 0
            ? (char)buffer[bufferIndexCurrent]
            : throw new InvalidOperationException();

    public StreamSeeker(Stream stream, int readAmount = 256, int bufferSize = 1024)
    {
        this.stream = stream;
        this.readAmount = readAmount;
        buffer = new RotatingBuffer(bufferSize);
    }

    public bool MoveNext()
    {
        if (bufferIndexCurrent + 1 >= buffer.TotalReadAmount)
        {
            if (!buffer.Read(stream, readAmount))
            {
                if (bufferIndexCurrent + 1 >= buffer.TotalReadAmount)
                    return false;
            }
        }

        char prev = bufferIndexCurrent >= 0 ? (char)buffer[bufferIndexCurrent] : '\0';

        bufferIndexCurrent++;

        char cur = (char)buffer[bufferIndexCurrent];

        if (cur == '\r' || (cur == '\n' && prev != '\r'))
        {
            lineLengths.Add(0);
            LineNo++;
            CharNo = 0;
        }
        else
        {
            CharNo++;
            lineLengths[LineNo]++;
        }

        return true;
    }

    public void Reverse(int characters)
    {
        if (characters < 0)
            throw new ArgumentOutOfRangeException(nameof(characters));

        if (characters > buffer.Capacity)
            throw new InvalidOperationException("Reverse exceeds buffer capacity.");

        if (characters > bufferIndexCurrent)
        {
            Reset();
            return;
        }

        bufferIndexCurrent -= characters;
        buffer.GoBack(stream, characters);

        for (int i = 0; i < characters; i++)
        {
            if (CharNo == 0)
            {
                LineNo--;
                CharNo = lineLengths[LineNo];
            }
            else
            {
                CharNo--;
            }
        }
    }

    public void Reset()
    {
        if (!stream.CanSeek)
            throw new InvalidOperationException();

        LineNo = 0;
        CharNo = -1;
        bufferIndexCurrent = -1;
        buffer.GoBack(stream, buffer.TotalReadAmount);
    }
}
