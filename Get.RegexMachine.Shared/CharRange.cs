namespace Get.RegexMachine;

/// <summary>
/// Represents an inclusive range of characters [From, To].
/// Used to replace character-by-character transitions with range-based transitions
/// for full Unicode support.
/// </summary>
public readonly record struct CharRange(char From, char To)
{
    public bool Contains(char c) => c >= From && c <= To;

    public bool Overlaps(CharRange other) => From <= other.To && other.From <= To;

    /// <summary>
    /// Returns true if this range is adjacent to or overlapping with other,
    /// meaning they could be merged into a single contiguous range.
    /// </summary>
    public bool CanMerge(CharRange other) => Overlaps(other)
        || From == other.To + 1
        || other.From == To + 1;

    /// <summary>
    /// Merges two ranges that are overlapping or adjacent into one.
    /// </summary>
    public static CharRange Merge(CharRange a, CharRange b) =>
        new((char)Math.Min(a.From, b.From), (char)Math.Max(a.To, b.To));

    public override string ToString() =>
        From == To ? $"'{From}'" : $"['{From}'-'{To}']";
}
