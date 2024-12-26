using Get.RegexMachine;
using System.Diagnostics;


ListSeekable<char> Iter(string str) => new([.. str]);


const string Int = "Integer", Id = "Identifier", Plus = "Plus", Minus = "Minus", Times = "Times", Whitespace = "Whitespace";
// DateTime t1, t2;
RegexCompiler<string>.DFAState dfa = RegexCompiler<string>.GenerateDFA([
    new(@"[0-9]+", Int),
    new(@"[a-zA-Z_][a-zA-Z_0-9]*", Id),
    new(@"\+", Plus),
    new(@"-", Minus),
    new(@"[\t ]+", Whitespace),
    new(@"\*", Times)
], RegexConflictBehavior.Throw);
/*
int i;
t1 = DateTime.Now;
for (i = 0; i < 10; i++)
    dfa = RegexCompiler<string>.GenerateDFA([
    new(@"[0-9]+", Int),
    new(@"[a-zA-Z_][a-zA-Z_0-9]*", Id),
    new(@"\+", Plus),
    new(@"-", Minus),
    new(@"[\t ]+", Whitespace),
    new(@"\*", Times)
], RegexConflictBehavior.Throw);
t2 = DateTime.Now;
Console.WriteLine($"Average time used on DFA creation on runtime: {(t2 - t1).TotalMilliseconds / 10}ms");
*/
var iter = Iter("1234 + 123 * 2 - someVariable");
void Assert(string? output, string expected)
{
    Debug.Assert(output == expected);
}
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Int); // 1
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Whitespace); // [ws]
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Plus); // +
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Whitespace); // [ws]
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Int); // 1
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Whitespace); // [ws]
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Times); // *
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Whitespace); // [ws]
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Int); // 2
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Whitespace); // [ws]
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Minus); // -
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Whitespace); // [ws]
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Id); // x

iter.Reset();
/*
while (RegexRunner<string>.Next(dfa, iter) != null)
{
    // do nothing
}
iter.Reset();
t1 = DateTime.Now;
i = 0;
while (RegexRunner<string>.Next(dfa, iter) != null)
{
    i++;
    // do nothing
}
t2 = DateTime.Now;
Console.WriteLine($"Time used on average on runtime: {(t2 - t1).TotalNanoseconds / i}ns/call");
*/
iter = Iter("1234 +");
dfa = RegexCompiler<string>.GenerateDFA([
    new(@"[0-9]+", Int),
    new(@"[\t ]+", Whitespace),
    new(@"", "fallback")
], RegexConflictBehavior.Throw);
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Int); // 1234
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, Whitespace); // [ws]
Assert(RegexRunner<string>.Next(dfa, iter)!.Value.value, "fallback"); // can't match anything else