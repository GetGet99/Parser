using Get.RegexMachine;

namespace Get.Lexer;

public sealed class StringTextSeeker(string text) : ITextSeekable
{
    private readonly string text = text ?? throw new ArgumentNullException(nameof(text));
    private int index = -1;

    public int LineNo { get; private set; } = 0;
    public int CharNo { get; private set; } = -1;

    public int CurrentPosition => index;

    public char Current =>
        index >= 0 && index < text.Length
            ? text[index]
            : throw new InvalidOperationException("No current character.");

    public bool MoveNext()
    {
        if (index + 1 >= text.Length)
            return false;

        index++;
        char cur = text[index];

        if (cur == '\r')
        {
            LineNo++;
            CharNo = 0;
        }
        else if (cur == '\n')
        {
            // If previous was '\r', this is part of CRLF,
            // and we've already advanced the line.
            if (index > 0 && text[index - 1] == '\r')
            {
                // Do nothing: already handled
            }
            else
            {
                LineNo++;
                CharNo = 0;
            }
        }
        else
        {
            // First character in file
            if (index == 0)
            {
                CharNo = 0;
            }
            // First character after newline
            else if (text[index - 1] == '\n' ||
                     text[index - 1] == '\r')
            {
                CharNo = 0;
            }
            else
            {
                CharNo++;
            }
        }

        return true;
    }

    public void Reverse(int chars)
    {
        if (chars < 0)
            throw new ArgumentOutOfRangeException(nameof(chars));

        int target = index - chars;
        if (target < -1)
            target = -1;

        // Reset state and replay forward
        index = -1;
        LineNo = 0;
        CharNo = -1;

        for (int i = 0; i <= target; i++)
            MoveNext();
    }
}
