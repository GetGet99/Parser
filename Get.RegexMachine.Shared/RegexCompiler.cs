namespace Get.RegexMachine;

/// <summary>
/// Compiles regex patterns into a deterministic finite automaton (DFA) for efficient matching.
/// </summary>
/// <typeparam name="T">The type of the value associated with each accepting DFA state.</typeparam>
public static partial class RegexCompiler<T>
{
    /// <summary>
    /// Compiles the given regexes into a DFA start state.
    /// </summary>
    /// <param name="regexes">The regex value/pattern pairs to compile.</param>
    /// <param name="conflictBehavior">How to handle conflicts when multiple regexes match the same input.</param>
    /// <returns>The DFA start state, from which matching can be performed via <see cref="RegexRunner{T}.Next"/>.</returns>
    public static DFAState /* startState */ GenerateDFA(IEnumerable<RegexVal<T>> regexes, RegexConflictBehavior conflictBehavior = RegexConflictBehavior.Last)
    {
        return ConvertToDFA(Generate(regexes), conflictBehavior);
    }
}
/// <summary>
/// Determines how conflicts are resolved when multiple compiled regex patterns
/// can match the same input at the same position.
/// </summary>
public enum RegexConflictBehavior
{
    /// <summary>Throws <see cref="RegexConflictCompilerException"/> when a conflict is detected.</summary>
    Throw,
    /// <summary>The last registered pattern wins (default).</summary>
    Last
}