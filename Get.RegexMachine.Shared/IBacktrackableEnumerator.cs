namespace Get.RegexMachine;

/// <summary>
/// An <see cref="IEnumerator{T}"/> that supports backtracking (reversing) by a given number of elements.
/// </summary>
[Obsolete("Use ISeekable<T> instead, which provides the same functionality.")]
public interface IBacktrackableEnumerator<T> : IEnumerator<T>
{
    /// <summary>The current position (0-based index) in the sequence.</summary>
    int CurrentPosition { get; }
    /// <summary>Moves backward by the given number of elements.</summary>
    void Reverse(int count);
}
