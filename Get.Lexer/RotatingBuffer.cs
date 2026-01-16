namespace Get.Lexer;

public partial class RotatingBuffer
{
    private readonly byte[] buffer;
    private readonly int capacity;

    private int start = 0;   // logical index 0
    private int length = 0;  // valid data length

    public int TotalReadAmount { get; private set; }

    public int Capacity => capacity;

    public RotatingBuffer(int bufferSize)
    {
        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        capacity = bufferSize;
        buffer = new byte[capacity];
    }

    private int End => (start + length) % capacity;

    /// <summary>
    /// Helper for test functions – returns a copy of the requested range
    /// from the still-buffered data.
    /// </summary>
    private byte[] this[Range range]
    {
        get
        {
            // Translate Range into absolute indices
            var (offset, count) = range.GetOffsetAndLength(TotalReadAmount);

            // Oldest index still available in the buffer
            int earliestAvailable = TotalReadAmount - length;

            if (offset < earliestAvailable)
                throw new ArgumentOutOfRangeException(nameof(range),
                    "Requested range has been overwritten by the buffer.");

            if (offset + count > TotalReadAmount)
                throw new ArgumentOutOfRangeException(nameof(range));

            var result = new byte[count];

            // Convert absolute index → buffer-relative index
            int bufferOffset = offset - earliestAvailable;

            for (int i = 0; i < count; i++)
            {
                result[i] = this[bufferOffset + i];
            }

            return result;
        }
    }

    /// <summary>
    /// Logical indexer: 0 is the oldest byte still in buffer
    /// </summary>
    public byte this[int index]
    {
        get
        {
            if ((uint)index >= (uint)TotalReadAmount)
                throw new IndexOutOfRangeException();

            int realIndex = (start + index) % capacity;
            return buffer[realIndex];
        }
    }

    /// <summary>
    /// Reads up to <paramref name="amount"/> bytes from the stream.
    /// Returns false if the stream ended before reading that amount.
    /// </summary>
    public bool Read(Stream stream, int amount)
    {
        if (amount < 0 || amount > capacity)
            throw new ArgumentOutOfRangeException(nameof(amount));

        int remaining = amount;

        while (remaining > 0)
        {
            int writeIndex = End;
            int contiguous = Math.Min(remaining, capacity - writeIndex);

            int read = stream.Read(buffer, writeIndex, contiguous);
            if (read == 0)
                return false;

            TotalReadAmount += read;
            remaining -= read;

            length += read;
            if (length > capacity)
            {
                int overflow = length - capacity;
                start = (start + overflow) % capacity;
                length = capacity;
            }
        }

        return true;
    }

    /// <summary>
    /// Rewinds the buffer and stream by <paramref name="amount"/> bytes.
    /// If rewinding beyond buffer capacity, the buffer resets.
    /// </summary>
    public void GoBack(Stream stream, int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        TotalReadAmount -= amount;

        if (amount >= length)
        {
            start = 0;
            length = 0;
        }
        else
        {
            length -= amount;
        }

        if (!stream.CanSeek)
            throw new InvalidOperationException("Stream must support seeking.");

        stream.Seek(-amount, SeekOrigin.Current);
    }
}
