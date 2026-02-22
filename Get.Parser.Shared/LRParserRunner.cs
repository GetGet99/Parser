using Get.PLShared;
using System.Data;
using System.Diagnostics;
namespace Get.Parser;
public static class LRParserRunner<TProgram>
{
    public static TProgram Parse(ILRParserDFA dfa, IEnumerable<ITerminalValue?> tokens, bool debug = false, List<ErrorTerminalValue>? handledErrors = null, bool skipErrorHandling = true)
    {
        List<ISyntaxElementValue> stack = [];
        foreach (var nextTokenForLoop in Infinite(tokens))
        {
            bool insertionAttemptedForThisToken = false;
            var nextToken = nextTokenForLoop;
            if (debug)
            {
                Console.WriteLine($"Stack is now {string.Join(", ", stack)}");
                Console.WriteLine($"Next Token: {nextToken}");
            }
        rerun:
            ILRDFAAction? act;
            try
            {
                act = dfa.GetAction(stack, nextToken);
            }
            catch (LRParserRuntimeException e)
            {
                // error handling

                // backward compatability: if grammar wasn't made with error handling in mind
                // new skipping token logic may lead to this function complete without throwing
                // on invalid grammar
                // grammar must opt in to error handling to do error handling
                if (skipErrorHandling)
                    throw;
                
                if (stack.Count is 0)
                    // i don't know
                    throw;

                if (insertionAttemptedForThisToken)
                {
                    if (nextToken != null)
                    {
                        handledErrors?.Add(new ErrorTerminalValue(e)
                        {
                            Start = nextToken.Start,
                            End = nextToken.End
                        });
                        continue;
                    }

                    throw;
                }

                ListSpan<ISyntaxElementValue> tempStack = new(stack, startIdx: 0, count: stack.Count);
                var err = new ErrorTerminalValue(e) { Start = stack[^1].End, End = stack[^1].End };
                //stack[^1] = err;
                do
                {
                    try
                    {
                        act = dfa.GetAction(tempStack, err);
                        handledErrors?.Add(err);
                        insertionAttemptedForThisToken = true;
                        var itemsRemoved = stack.Count - tempStack.Count;
                        if (itemsRemoved > 0)
                            stack.RemoveRange(index: stack.Count - itemsRemoved, count: itemsRemoved);
                        goto resolved;
                    }
                    catch
                    {
                        if (tempStack.Count >= 1)
                        {
                            err.Start = tempStack[^1].Start;
                            // remove stack[^1]
                            tempStack = new(stack, startIdx: 0, count: tempStack.Count - 1);
                            continue;
                        }
                        // if no more to delete from stack,
                        // break out of the loop and try to skip this problematic token.
                        break;
                    }
                }
                while (tempStack.Count > 0);
                throw;
            }
        resolved:
            if (act is null) // SHIFT
            {
                if (debug) Console.WriteLine($"SHIFT");
                if (nextToken is null)
                    throw new InvalidOperationException(
                        "Can no longer shift the object, the DFA should be handling this."
                    );
                stack.Add(nextToken);
            }
            else if (act is LRDFAReduce reduce)
            {
                var rule = reduce.Rule;
                if (debug) Console.WriteLine($"REDUCE WITH {rule.Target} <- {string.Join(" ", rule.Expressions)}");
                var values = new ISyntaxElementValue[rule.Expressions.Count];
                foreach (int i in ..values.Length)
                {
                    values[^(i + 1)] = stack[^(i + 1)];
                }
                stack.RemoveRange(stack.Count - values.Length, values.Length);
                var val = rule.GetValue(values);
                if (!val.WithoutValue.Equals(reduce.Rule.Target))
                    throw new InvalidDataException("The returned symbol does not match the given rule");
                if (values.Length > 0)
                {
                    val.Start = values[0].Start;
                    val.End = values[^1].End;
                }
                else if (stack.Count > 0)
                {
                    val.Start = val.End = stack[^1].End;
                }
                stack.Add(val);
                goto rerun; // rerun, as we do not want to read the next token yet.
            }
            else if (act is ILRDFAAccept accept)
            {
                if (debug) Console.WriteLine($"ACCEPT");
                if (stack[0] is not INonTerminalValue<TProgram> programNode)
                    throw new InvalidOperationException("Not accepting on the program node or program does not implement INonTerminalValue<TProgram>");
                return programNode.Value;
            }
        }
        throw new UnreachableException("Infinite() should be infinte? How?");
    }
#if !NET8_0_OR_GREATER
    class UnreachableException(string message) : Exception(message);
#endif
    static IEnumerable<T?> Infinite<T>(IEnumerable<T?> values) where T : class
    {
        foreach (var val in values)
            yield return val;
        while (true)
            yield return null;
    }

}
public class ErrorTerminalValue(LRParserRuntimeException exception) : ITerminalValue<LRParserRuntimeException>
{
    public ITerminal WithoutValue => ErrorTerminal.Singleton;
    ISyntaxElement ISyntaxElementValue.WithoutValue => ErrorTerminal.Singleton;
    public LRParserRuntimeException Value { get; } = exception;

    public required Position Start { get; set; }

    public required Position End { get; set; }
}
public class LRParserRuntimeException(string message) : Exception(message);
public class LRParserRuntimeUnexpectedInputException(ISyntaxElementValue unexpectedElement) : LRParserRuntimeException($"Unexpected element: {unexpectedElement}")
{
    public ISyntaxElementValue UnexpectedElement { get; } = unexpectedElement;
}
public class LRParserRuntimeUnexpectedEndingException(ISyntaxElement[] expectedInputs) : LRParserRuntimeException($"Expecting any of {string.Join(", ", (object?[])expectedInputs)}, but got no more inputs")
{
    public ISyntaxElement[] ExpectedInputs { get; } = expectedInputs;
}
public partial interface ICFGRule
{
    INonTerminalValue GetValue(ISyntaxElementValue[] value);
}
public interface ISyntaxElementValue
{
    Position Start { get; set; }
    Position End { get; set; }
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
