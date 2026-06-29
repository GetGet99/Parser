using System.Collections;

namespace Get.RegexMachine;

public class ListSeekable<T>(IReadOnlyList<T> values) : ISeekable<T>
{
    int idx = -1;
    public T Current => values[idx];

    public int CurrentPosition => idx;

    public bool MoveNext()
    {
        if (idx + 1 >= values.Count) return false;
        idx++;
        return true;
    }

    public void Reset()
    {
        idx = -1;
    }

    public void Reverse(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "count must be positive");
        if (idx - count < 0) throw new ArgumentOutOfRangeException(nameof(count), "count is greater than the number of elements read.");
        idx -= count;
    }
}

public struct StringSeekable(string values) : ISeekable<char>
{
    int idx = -1;
    public readonly char Current => values[idx];

    public readonly int CurrentPosition => idx;

    public bool MoveNext()
    {
        if (idx + 1 >= values.Length) return false;
        idx++;
        return true;
    }

    public void Reset()
    {
        idx = -1;
    }

    public void Reverse(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "count must be positive");
        if (idx - count < 0) throw new ArgumentOutOfRangeException(nameof(count), "count is greater than the number of elements read.");
        idx -= count;
    }
}
