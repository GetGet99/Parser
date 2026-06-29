using Get.Parser;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Get.RegexMachine;

partial class RegexCompiler<T> where T : class
{
    static NFAState /* startState */ Generate(IEnumerable<RegexVal<T>> regexes)
    {
        NFAState startState = new(-1, 0);
        int ruleId = 0;
        try
        {
            foreach (var r in regexes)
            {
                startState.Epsilon.Add(Generate(r, ruleId).startState);
                ruleId++;
            }
        }
        catch (RegexCompilerException ex)
        {
            throw new MultiRegexCompilerException(ruleId, ex);
        }
        return startState;
    }

    static (NFAState startState, NFAState endState) Generate(RegexVal<T> regex, int ruleId)
    {
        var (startState, endState) = Generate(regex.Regex, ruleId, regex.Order);
        endState.Value = regex.Value;
        return (startState, endState);
    }
    static (NFAState startState, NFAState endState) Generate(string regex, int ruleId, int order)
    {
        var parser = RegexParser.Instance;
        try
        {
            var nfastates = parser.Parse(regex, () => new NFAState(ruleId, order));
            return ((NFAState)nfastates.StartState, (NFAState)nfastates.EndState);
        } catch (LRParserRuntimeException e)
        {
            throw new RegexCompilerException(e.Message);
        }
    }

    public class NFAState(int rule, int order) : INFAState
    {
        public int Order { get; } = order;
        public int Rule { get; } = rule;
        HashSet<NFAState>? _epsilon;
        internal List<(CharRange Range, HashSet<NFAState> Targets)> Transitions { get; } = [];

        /// <summary>
        /// Gets the set of NFA states reachable from this state via the given character.
        /// Uses range-based lookup for full Unicode support.
        /// </summary>
        public HashSet<NFAState>? this[char c]
        {
            get
            {
                foreach (var (range, targets) in Transitions)
                    if (range.Contains(c))
                        return targets;
                return null;
            }
        }
        public HashSet<NFAState> Epsilon { get => _epsilon ??= []; }
        public T? Value { get; set; }
        public bool IsAccepting => Value != null;

        void INFAState.AddTransition(char c, Get.RegexMachine.INFAState next)
        {
            AddTransitionRange(c, c, (NFAState)next);
        }

        void INFAState.AddTransition(char from, char to, INFAState next)
        {
            AddTransitionRange(from, to, (NFAState)next);
        }

        void AddTransitionRange(char from, char to, NFAState next)
        {
            var newRange = new CharRange(from, to);
            // Try to merge with existing ranges when targeting the same state set
            for (int i = 0; i < Transitions.Count; i++)
            {
                var (range, targets) = Transitions[i];
                if (range.CanMerge(newRange) && targets.Contains(next))
                {
                    Transitions[i] = (CharRange.Merge(range, newRange), targets);
                    return;
                }
            }
            // No merge possible, add new entry
            for (int i = 0; i < Transitions.Count; i++)
            {
                var (range, targets) = Transitions[i];
                if (newRange.From < range.From)
                {
                    Transitions.Insert(i, (newRange, [next]));
                    return;
                }
            }
            Transitions.Add((newRange, [next]));
        }

        public override string ToString()
        {
            var parts = new List<string>();
            foreach (var (range, _) in Transitions)
                parts.Add(range.ToString());
            if (_epsilon != null) parts.Add("ε");
            return $"({Value as object ?? "null"}) => {{{string.Join(", ", parts)}}}";
        }

        void INFAState.AddEpsilonTransition(INFAState next)
        {
            Epsilon.Add((NFAState)next);
        }
    }
}
public interface INFAState
{
    void AddEpsilonTransition(INFAState next);
    void AddTransition(char c, INFAState next);
    void AddTransition(char from, char to, INFAState next);
}
public record class RegexVal<T>([StringSyntax(StringSyntaxAttribute.Regex)] string Regex, T? Value, int Order = 0) where T : class;
public class RegexCompilerException : Exception
{
    public RegexCompilerException(string message) : base(message) { }
    protected RegexCompilerException(string message, RegexCompilerException innerException) : base(message, innerException) { }
}
public class MultiRegexCompilerException(int id1, RegexCompilerException innerException) : RegexCompilerException($"Rule {id1} is invalid: {innerException.Message}", innerException)
{
    public int RuleId { get; } = id1;
}
public class RegexConflictCompilerException(int[] ids) : RegexCompilerException($"Conflict Detected! Id = {string.Join(", ", ids)}")
{
    public int[] ConflictIds { get; } = ids;
}