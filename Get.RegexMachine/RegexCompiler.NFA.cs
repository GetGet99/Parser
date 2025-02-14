﻿using Get.Parser;
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
        parser.CreateEmptyNFAState = () => new NFAState(ruleId, order);
        try
        {
            var nfastates = parser.Parse(regex);
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
        Dictionary<char, HashSet<NFAState>> Transitions { get; } = [];
        public HashSet<NFAState> this[char c]
        {
            get
            {
                if (!Transitions.TryGetValue(c, out var result))
                {
                    Transitions[c] = result = [];
                }
                return result;
            }
        }
        public IEnumerable<char> TransitionKeys => Transitions.Keys;
        internal void RemoveTransition(char c)
        {
            Transitions.Remove(c);
        }
        public HashSet<NFAState> Epsilon { get => _epsilon ??= []; }
        public T? Value { get; set; }
        public bool IsAccepting => Value != null;

        void INFAState.AddTransition(char c, Get.RegexMachine.INFAState next)
        {
            this[c].Add((NFAState)next);
        }

        public override string ToString()
        {
            return $"({Value as object ?? "null"}) => {{{string.Join(", ", from key in (_epsilon != null ? Transitions.Keys.Append('ε') : Transitions.Keys) select $"'{key}'")}}}";
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