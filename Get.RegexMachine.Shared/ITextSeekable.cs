namespace Get.RegexMachine;
/// <summary>
/// A seekable character source that also tracks line and character numbers,
/// enabling position-aware regex matching.
/// </summary>
public interface ITextSeekable : ISeekable<char>
{
    /// <summary>The zero-based line number of the current position.</summary>
    int LineNo { get; }
    /// <summary>The zero-based character offset within the current line.</summary>
    int CharNo { get; }
}