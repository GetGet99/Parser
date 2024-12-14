using Get.RegexMachine;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Get.Lexer;

// Code By Gemini and I
public partial class RotatingBuffer(int BufferSize)
{
    // Buffer is a buffer that, when we read a lot of text such that if it would pass the end, it wraps back to the beginning.

    int start = 0, end = 0, length = 0; // suggested fields
    readonly byte[] buffer = new byte[BufferSize];
    public int TotalReadAmount { get; private set; } = 0;
    public byte this[int index]
    {
        get
        {
            if (index >= TotalReadAmount)
            {
                throw new IndexOutOfRangeException("Index is beyond the total amount of data read.");
            }
            var realIdx = (start + index) % BufferSize;
            return buffer[index];
        }
    }
    /// <summary>
    /// Helper for test function
    /// </summary>
    /// <param name="r">The range</param>
    /// <returns>byte array</returns>
    private byte[] this[Range r]
    {
        get
        {
            var (offset, length) = r.GetOffsetAndLength(TotalReadAmount);
            var arr = new byte[length];
            for (int i = 0; i < length; i++)
            {
                arr[i] = this[offset + i];
            }
            return arr;
        }
    }


    // Reads amount bytes from the stream.
    // If stream reaches the end before amount bytes, return false, otherwise, returns true.
    public bool Read(Stream s, int amount)
    {
        if (amount > BufferSize)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be greater than buffer size.");
        }

        // Calculate the remaining space in the buffer
        int remainingSpace = BufferSize - end;
        int bytesRead;
        // If there's enough space, read directly into the buffer
        if (remainingSpace >= amount)
        {
            bytesRead = s.Read(buffer, end, amount);
            TotalReadAmount += bytesRead;
            end += bytesRead;
            length += bytesRead;
            if (length > BufferSize) length = BufferSize;
            if (bytesRead < amount) return false;
            return true;
        }

        // Otherwise, we need to wrap around
        int firstPart = BufferSize - start;
        int secondPart = amount - firstPart;

        // Read the first part into the buffer
        bytesRead = s.Read(buffer, start, firstPart);
        TotalReadAmount += bytesRead;
        length += bytesRead;
        if (length > BufferSize) length = BufferSize;
        end = start + bytesRead;
        if (bytesRead < firstPart) return false;

        // If there's a second part, read it into the beginning of the buffer
        if (secondPart > 0)
        {
            bytesRead = s.Read(buffer, 0, secondPart);
            TotalReadAmount += bytesRead;
            length += bytesRead;
            if (length > BufferSize) length = BufferSize;
            end = bytesRead;
            start = 0;
            if (bytesRead < secondPart) return false;
        }

        return true;
    }
    // backtracks by amount bytes, if the amount is a little and can backtrack
    // buffer such that the old value is still there, backtracks.
    // otherwise, reset everything.
    // Regardless of what happens, stream should be backtrack by that amount.
    public void GoBack(Stream s, int amount)
    {
        TotalReadAmount -= amount;
        if (amount > length)
        {
            start = 0;
            end = 0;
        }
        else
        {
            end -= amount;
            if (end < 0) end += BufferSize;
        }
        s.Seek(-amount, SeekOrigin.Current);
    }
}
