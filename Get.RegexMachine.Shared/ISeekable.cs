namespace Get.RegexMachine;

/// <summary>
/// Represents a seekable, forward-and-backward sequence of elements.
/// Used by the regex engine to traverse input text with backtracking support.
/// </summary>
/// <typeparam name="T">The type of elements in the sequence.</typeparam>
public interface ISeekable<T>
{
    /// <summary>The current position (0-based index) in the sequence.</summary>
    int CurrentPosition { get; }
    /// <summary>The current element at the active position.</summary>
    T Current { get; }
    /// <summary>Advances to the next element. Returns false when the end is reached.</summary>
    bool MoveNext();
    /// <summary>Moves backward by the given number of elements.</summary>
    void Reverse(int chars);
}