using Get.Parser;
using System.Collections;

namespace Get.Parser;

internal class LRParserDFA(IEqualityComparer<INonTerminal> nontermComparer, IEqualityComparer<ITerminal> termComparer) : ILRParserDFA
{
    public ILRDFAAction? OnEndSymbol;
    public Dictionary<ITerminal, ILRDFAAction> Actions = new(termComparer);
    // maybe useful as a cache?
    internal required HashSet<LRItem> Items { get; init; }
    public Dictionary<ISyntaxElement, LRParserDFA> NextDFANode = new(new SyntaxElementComparer(termComparer, nontermComparer));
    public ILRDFAAction? GetAction(IReadOnlyList<ISyntaxElementValue> stack, ITerminalValue? nextToken)
    {
        if (stack.Count == 0)
        {
            if (nextToken is null) return OnEndSymbol;
            Actions.TryGetValue(nextToken.WithoutValue, out var act);
            // return null (SHIFT) if we don't find it.
            return act;
        }
        if (NextDFANode.TryGetValue(stack[0].WithoutValue, out var next))
        {
            return next.GetAction(new ListSpan<ISyntaxElementValue>(stack, 1), nextToken);
        }
        throw new InvalidOperationException("Invalid Program");
    }
}

readonly struct ListSpan<T>(IReadOnlyList<T> values, int startIdx, int count) : IReadOnlyList<T>
{
    public ListSpan(IReadOnlyList<T> values, int startIdx) : this(values, startIdx, values.Count - startIdx) { }
    readonly IReadOnlyList<T> values = values is ListSpan<T> s ? s.values : values;
    readonly int startIdx = values is ListSpan<T> s ? s.startIdx + startIdx : startIdx;
    public T this[int index] => values[index + startIdx];

    public int Count => count;

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}