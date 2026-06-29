namespace Get.PLShared;

/// <summary>
/// Represents a zero-based position (line, character offset) within a source text.
/// <see cref="ToString"/> converts to 1-based human-readable format (<c>L:C</c>).
/// </summary>
/// <param name="Line">Zero-based line number.</param>
/// <param name="Char">Zero-based character offset within the line.</param>
public record struct Position(int Line, int Char)
{
    /// <summary>
    /// Returns the position in 1-based human-readable format: <c>Line+1:Char+1</c>.
    /// For example, the first character of the file is <c>1:1</c>.
    /// </summary>
    public override string ToString() => $"{Line + 1}:{Char + 1}";
}
