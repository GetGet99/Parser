using System.Diagnostics;
namespace Get.Parser;
public static class LRParserRunner<TProgram>
{
    public static TProgram Parse(ILRParserDFA dfa, IEnumerable<ITerminalValue?> tokens)
    {
        List<ISyntaxElementValue> stack = [];
        foreach (var nextTokenForLoop in Infinite(tokens))
        {
            var nextToken = nextTokenForLoop;
        rerun:
            var act = dfa.GetAction(stack, nextToken);
            if (act is null) // SHIFT
            {
                if (nextToken is null)
                    throw new InvalidOperationException();
                stack.Add(nextToken);
            }
            else if (act is LRDFAReduce reduce)
            {
                var rule = reduce.Rule;
                var values = new ISyntaxElementValue[rule.Expressions.Count];
                foreach (int i in ..values.Length)
                {
                    values[^(i+1)] = stack[^(i+1)];
                }
                stack.RemoveRange(stack.Count - values.Length, values.Length);
                var val = rule.GetValue(values);
                stack.Add(val);
                goto rerun; // rerun, as we do not want to read the next token yet.
            } else if (act is ILRDFAAccept accept)
            {
                if (stack[0] is not INonTerminalValue<TProgram> programNode)
                    throw new InvalidOperationException("Not accepting on the program node");
                return programNode.Value;
            }
        }
        throw new UnreachableException("Infinite() should be infinte? How?");
    }
    static IEnumerable<T?> Infinite<T>(IEnumerable<T?> values) where T : class
    {
        foreach (var val in values)
            yield return val;
        while (true)
            yield return null;
    }
}
public partial interface ICFGRule
{
    INonTerminalValue GetValue(ISyntaxElementValue[] value);
}
public interface ISyntaxElementValue
{
    ISyntaxElement WithoutValue { get; }
}
public interface ISyntaxElementValue<T> : ISyntaxElementValue
{
    T Value { get; }
}
public interface ITerminalValue : ISyntaxElementValue
{
    new ITerminal WithoutValue { get; }
}
public interface ITerminalValue<T> : ITerminalValue, ISyntaxElementValue<T>;
public interface INonTerminalValue : ISyntaxElementValue;
public interface INonTerminalValue<T> : INonTerminalValue, ISyntaxElementValue<T>;