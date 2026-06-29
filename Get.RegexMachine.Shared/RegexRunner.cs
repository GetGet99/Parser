using Get.PLShared;
using System.Text;
namespace Get.RegexMachine;

/// <summary>
/// Runs a compiled regex DFA over an <see cref="ISeekable{T}"/> input stream to find matches.
/// </summary>
/// <typeparam name="T">The type of the value associated with each accepting DFA state.</typeparam>
public static class RegexRunner<T> where T : class
{
    /// <summary>
    /// Finds the next match in the input, returning the matched value and text.
    /// Longest-match semantics are used: the DFA tracks the last accepting state
    /// encountered and backtracks to it when no further transitions are possible.
    /// </summary>
    /// <param name="startDFAState">The starting DFA state (typically from <see cref="RegexCompiler{T}.GenerateDFA"/>).</param>
    /// <param name="enumerator">The seekable character input to match against.</param>
    /// <returns>A tuple of (value, matchedText) on success, or null if no match was found.</returns>
    public static (T value, string matchedText)? Next(RegexCompiler<T>.DFAState startDFAState, ISeekable<char> enumerator)
    {
        var currentDFAState = startDFAState;
        RegexCompiler<T>.DFAState? lastSuccessfulState = null;
        int backtrackPosition = 0;
        var matchedTextBuilder = new StringBuilder();
        while (enumerator.MoveNext())
        {
            var c = enumerator.Current;
            matchedTextBuilder.Append(c);
            var nextState = currentDFAState[c];
            if (nextState is not null)
            {
                if (nextState.Value != null)
                {
                    lastSuccessfulState = nextState;
                    backtrackPosition = enumerator.CurrentPosition;
                }
                currentDFAState = nextState;
            } else
            {
                break;
            }
        }
        var matchedText = matchedTextBuilder.ToString();
        if (lastSuccessfulState != null)
        {
            var backtrackLength = enumerator.CurrentPosition - backtrackPosition;
            matchedText = matchedText[..^(backtrackLength)];
            enumerator.Reverse(backtrackLength);
        }
        // If we reached a final state, return its value
        if (lastSuccessfulState?.Value != null)
        {
            return (lastSuccessfulState.Value, matchedText);
        }

        // No match found
        // let's see if the start state is accepting (empty string)
        if (startDFAState.Value is { } v)
        {
            return (v, "");
        }
        // No match found
        return null;
    }
    /// <inheritdoc cref="Next(RegexCompiler{T}.DFAState, ISeekable{char})"/>
    /// <remarks>Also returns the zero-based <see cref="Position"/> of the match start and end.</remarks>
    public static (T value, string matchedText, Position Start, Position End)? NextWithPosition(RegexCompiler<T>.DFAState startDFAState, ITextSeekable enumerator)
    {
        var currentDFAState = startDFAState;
        RegexCompiler<T>.DFAState? lastSuccessfulState = null;
        int backtrackPosition = enumerator.CurrentPosition;
        var matchedTextBuilder = new StringBuilder();
        int lengthCheckpoint = 0;
        Position Start = new(0, -1), End = new(0, -1);
        while (enumerator.MoveNext())
        {
            if (Start.Char == -1) Start = new(enumerator.LineNo, enumerator.CharNo);
            var c = enumerator.Current;
            var nextState = currentDFAState[c];
            if (nextState is not null)
            {
                matchedTextBuilder.Append(c);
                if (nextState.Value != null)
                {
                    lastSuccessfulState = nextState;
                    backtrackPosition = enumerator.CurrentPosition;
                    lengthCheckpoint = matchedTextBuilder.Length;
                    End = new(enumerator.LineNo, enumerator.CharNo);
                }
                currentDFAState = nextState;
            } else
            {
                break;
            }
        }
        var matchedText = matchedTextBuilder.ToString();
        if (lastSuccessfulState != null)
        {
            matchedText = matchedText[..lengthCheckpoint];
            enumerator.Reverse(enumerator.CurrentPosition - backtrackPosition);
        }
        // If we reached a final state, return its value
        if (lastSuccessfulState?.Value != null)
        {
            return (lastSuccessfulState.Value, matchedText, Start, End);
        }

        // No match found
        // reverse
        if (lastSuccessfulState == null)
            enumerator.Reverse(enumerator.CurrentPosition - backtrackPosition);
        // let's see if the start state is accepting (empty string)
        if (startDFAState.Value is { } v)
        {
            var pos = new Position(enumerator.LineNo, enumerator.CharNo);
            return (v, "", pos, pos);
        }
        // No match found
        return null;
    }
}

