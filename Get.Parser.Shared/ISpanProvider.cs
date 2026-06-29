using Get.PLShared;

namespace Get.Parser;

/// <summary>Allows setting source positions on a syntax element value from outside.</summary>
public interface ISpanSetter
{
    /// <summary>Sets the zero-based start position.</summary>
    Position Start { set; }
    /// <summary>Sets the zero-based end position.</summary>
    Position End { set; }
}
