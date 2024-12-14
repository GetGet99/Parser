using Get.PLShared;
namespace Get.RegexMachine;

public static class RegexRunner<T> where T : class
{
    public static (T value, string matchedText)? Next(RegexCompiler<T>.DFAState startDFAState, ISeekable<char> enumerator)
    {
        var currentDFAState = startDFAState;
        RegexCompiler<T>.DFAState? lastSuccessfulState = null;
        int backtrackPosition = 0;
        string matchedText = "";
        while (enumerator.MoveNext())
        {
            var c = enumerator.Current;
            matchedText += c;
            if (currentDFAState.Transitions.TryGetValue(c, out var nextState))
            {
                if (nextState.Value != null)
                {
                    lastSuccessfulState = nextState;
                    backtrackPosition = enumerator.CurrentPosition;
                }
                currentDFAState = nextState;
            }
        }
        if (lastSuccessfulState != null)
        {
            matchedText = matchedText[..^(enumerator.CurrentPosition - backtrackPosition)];
            enumerator.Reverse(enumerator.CurrentPosition - backtrackPosition);
        }
        // If we reached a final state, return its value
        if (lastSuccessfulState?.Value != null)
        {
            return (lastSuccessfulState.Value, matchedText);
        }

        // No match found
        return null;
    }
    public static (T value, string matchedText, Position Start, Position End)? NextWithPosition(RegexCompiler<T>.DFAState startDFAState, ITextSeekable enumerator)
    {
        var currentDFAState = startDFAState;
        RegexCompiler<T>.DFAState? lastSuccessfulState = null;
        int backtrackPosition = 0;
        string matchedText = "";
        int lengthCheckpoint = 0;
        Position Start = new(0, -1), End = new(0, -1);
        while (enumerator.MoveNext())
        {
            if (Start.Char == -1) Start = new(enumerator.LineNo, enumerator.CharNo);
            var c = enumerator.Current;
            if (currentDFAState.Transitions.TryGetValue(c, out var nextState))
            {
                matchedText += c;
                if (nextState.Value != null)
                {
                    lastSuccessfulState = nextState;
                    backtrackPosition = enumerator.CurrentPosition;
                    lengthCheckpoint = matchedText.Length;
                    End = new(enumerator.LineNo, enumerator.CharNo);
                }
                currentDFAState = nextState;
            } else
            {
                break;
            }
        }
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
        return null;
    }
}

