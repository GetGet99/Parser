using Get.PLShared;
using System.Data;
using System.Diagnostics;
namespace Get.Parser;
/// <summary>
/// Runs the LR(1) DFA against a sequence of input tokens to produce a parse result.
/// </summary>
/// <typeparam name="TProgram">The type of the final parse result.</typeparam>
public static class LRParserRunner<TProgram>
{
    /// <summary>
    /// Parses the token sequence using the given LR(1) DFA.
    /// </summary>
    /// <param name="dfa">The LR(1) DFA (from <see cref="LRParserDFAGen.CreateDFA"/>).</param>
    /// <param name="tokens">The sequence of input terminals to parse.</param>
    /// <param name="debug">When true, prints debug information to the console.</param>
    /// <param name="handledErrors">When set, error recovery entries are recorded here instead of throwing.</param>
    /// <param name="skipErrorHandling">When false, error recovery is attempted using the <c>Error</c> terminal. When true (default), parsing errors throw immediately.</param>
    /// <returns>The typed parse result.</returns>
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

            bool shouldRerun;
            do
            {
                shouldRerun = false;
                ILRDFAAction? act = null;

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
                            // skip this token, continue foreach
                            shouldRerun = false;
                            break;
                        }

                        throw;
                    }

                    ListSpan<ISyntaxElementValue> tempStack = new(stack, startIdx: 0, count: stack.Count);
                    var err = new ErrorTerminalValue(e) { Start = stack[^1].End, End = stack[^1].End };
                    bool recovered = false;
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
                            recovered = true;
                            break;
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

                    if (!recovered)
                        throw;

                    // act was set by error recovery, fall through to action dispatch
                }

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
                    shouldRerun = true; // rerun without consuming next token
                }
                else if (act is ILRDFAAccept accept)
                {
                    if (debug) Console.WriteLine($"ACCEPT");
                    if (stack[0] is not INonTerminalValue<TProgram> programNode)
                        throw new InvalidOperationException("Not accepting on the program node or program does not implement INonTerminalValue<TProgram>");
                    return programNode.Value;
                }
            } while (shouldRerun);
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
/// <summary>
/// Represents an error terminal value produced during error recovery.
/// Wraps the original <see cref="LRParserRuntimeException"/> and implements <see cref="ITerminalValue{T}"/>.
/// </summary>
public class ErrorTerminalValue(LRParserRuntimeException exception) : ITerminalValue<LRParserRuntimeException>
{
    /// <inheritdoc />
    public ITerminal WithoutValue => ErrorTerminal.Singleton;
    ISyntaxElement ISyntaxElementValue.WithoutValue => ErrorTerminal.Singleton;
    /// <summary>The original parser runtime exception that triggered error recovery.</summary>
    public LRParserRuntimeException Value { get; } = exception;

    /// <inheritdoc />
    public required Position Start { get; set; }
    /// <inheritdoc />
    public required Position End { get; set; }
}
/// <summary>Base exception for LR parser runtime errors.</summary>
public class LRParserRuntimeException(string message) : Exception(message);
/// <summary>Thrown when the parser encounters an unexpected input token.</summary>
public class LRParserRuntimeUnexpectedInputException(ISyntaxElementValue unexpectedElement) : LRParserRuntimeException($"Unexpected element: {unexpectedElement}")
{
    /// <summary>The unexpected input element.</summary>
    public ISyntaxElementValue UnexpectedElement { get; } = unexpectedElement;
}
/// <summary>Thrown when the parser reaches the end of input while still expecting more tokens.</summary>
public class LRParserRuntimeUnexpectedEndingException(ISyntaxElement[] expectedInputs) : LRParserRuntimeException($"Expecting any of {string.Join(", ", (object?[])expectedInputs)}, but got no more inputs")
{
    /// <summary>The set of expected input symbols that would have been valid.</summary>
    public ISyntaxElement[] ExpectedInputs { get; } = expectedInputs;
}
public partial interface ICFGRule
{
    /// <summary>Invokes the rule's semantic action with the given child values.</summary>
    INonTerminalValue GetValue(ISyntaxElementValue[] value);
}
/// <summary>Represents a value on the parser stack, with source position information.</summary>
public interface ISyntaxElementValue
{
    /// <summary>The zero-based start position of this syntax element.</summary>
    Position Start { get; set; }
    /// <summary>The zero-based end position of this syntax element.</summary>
    Position End { get; set; }
    /// <summary>Returns the syntax element without its value (type-only representation).</summary>
    ISyntaxElement WithoutValue { get; }
}
/// <summary>A syntax element value with a typed payload.</summary>
public interface ISyntaxElementValue<T> : ISyntaxElementValue
{
    /// <summary>The typed value of this syntax element.</summary>
    T Value { get; }
}
/// <summary>Represents a terminal value on the parser stack.</summary>
public interface ITerminalValue : ISyntaxElementValue
{
    /// <summary>Returns the terminal symbol without its value.</summary>
    new ITerminal WithoutValue { get; }
}
/// <summary>A terminal value with a typed payload.</summary>
public interface ITerminalValue<T> : ITerminalValue, ISyntaxElementValue<T>;
/// <summary>Represents a non-terminal value on the parser stack.</summary>
public interface INonTerminalValue : ISyntaxElementValue;
/// <summary>A non-terminal value with a typed payload.</summary>
public interface INonTerminalValue<T> : INonTerminalValue, ISyntaxElementValue<T>;
