namespace Get.RegexMachine;

public static partial class RegexCompiler<T>
{
    public static DFAState /* startState */ GenerateDFA(IEnumerable<RegexVal<T>> regexes, RegexConflictBehavior conflictBehavior = RegexConflictBehavior.Last)
    {
        return ConvertToDFA(Generate(regexes), conflictBehavior);
    }
}
public enum RegexConflictBehavior
{
    Throw,
    Last
}