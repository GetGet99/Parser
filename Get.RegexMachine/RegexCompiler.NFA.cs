﻿using System.Data;
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
        var (startState, endState) = Generate(regex.Regex.GetEnumerator(), ruleId, regex.Order);
        endState.Value = regex.Value;
        return (startState, endState);
    }
    static char GetChar(IEnumerator<char> regex)
    {
        switch (regex.Current)
        {
            case '\\':
                if (!regex.MoveNext()) throw new Exception("'\\' expects another character");
                return regex.Current switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\'' or '\"' or '.' or '[' or ']' or '(' or ')' or '\\' or '+' or '-' or '*' or '|' => regex.Current,
                    _ => throw new Exception($"'\\{regex.Current}' is not defined escaped character"),
                };
            default:
                return regex.Current;
        }

    }
    static (NFAState startState, NFAState endState) Generate(IEnumerator<char> regex, int Rule, int order, bool expectsClosedBracket = false)
    {
        NFAState startState = new(Rule, order);
        NFAState currentEndState = startState;
        (NFAState startState, NFAState endState, bool shouldSkipMoveNext, bool terminate) MoveNextPostProcess((NFAState startState, NFAState endState) box)
        {
            bool terminate = !regex.MoveNext();
            if (!terminate)
                switch (regex.Current)
                {
                    case '*':
                        {
                            NFAState newStartState = new(Rule, order);
                            newStartState.Epsilon.Add(box.startState);
                            box.endState.Epsilon.Add(newStartState);
                            box.startState = newStartState;
                            box.endState = new(Rule, order);
                            newStartState.Epsilon.Add(box.endState);
                            return (box.startState, box.endState, false, false);
                        }
                    case '+':
                        {
                            NFAState newStartState = new(Rule, order);
                            NFAState newEndState = new(Rule, order);
                            newStartState.Epsilon.Add(box.startState);
                            box.endState.Epsilon.Add(newEndState);
                            newEndState.Epsilon.Add(newStartState);
                            box.startState = newStartState;
                            box.endState = newEndState;
                            return (box.startState, box.endState, false, false);
                        }
                }
            return (box.startState, box.endState, true, terminate);
        }
        if (regex.MoveNext())
            while (true)
            {
                switch (regex.Current)
                {
                    case '(':
                        {
                            var child = Generate(regex, Rule, order, expectsClosedBracket: true);
                            var (childStart, childEnd, skip, terminate) = MoveNextPostProcess(child);
                            currentEndState.Epsilon.Add(childStart);
                            currentEndState = childEnd;
                            if (terminate) goto BreakWhile;
                            if (skip) continue;
                            break;
                        }
                    case ')':
                        if (!expectsClosedBracket) throw new Exception();
                        return (startState, currentEndState);
                    case '|':
                        {
                            NFAState newStartState = new(Rule, order);
                            var next = Generate(regex, order, Rule);
                            newStartState.Epsilon.Add(startState);
                            newStartState.Epsilon.Add(next.startState);
                            currentEndState = new(Rule, order);
                            startState.Epsilon.Add(currentEndState);
                            next.startState.Epsilon.Add(currentEndState);
                            startState = newStartState;
                        }
                        break;
                    case '[':
                        {
                            var next = GenerateClass(regex, Rule, order);
                            var (nextStart, nextEnd, skip, terminate) = MoveNextPostProcess(next);
                            currentEndState.Epsilon.Add(nextStart);
                            // think below should not be here?
                            // nextEnd.Epsilon.Add(currentEndState);
                            currentEndState = nextEnd;
                            if (terminate) goto BreakWhile;
                            if (skip) continue;
                        }
                        break;
                    default:
                        {
                            char c = GetChar(regex);
                            NFAState newStartState = new(Rule, order);
                            NFAState newEndState = new(Rule, order);
                            newStartState[c].Add(newEndState);
                            var (nextStart, nextEnd, skip, terminate) = MoveNextPostProcess((newStartState, newEndState));
                            currentEndState.Epsilon.Add(nextStart);
                            currentEndState = nextEnd;
                            if (terminate) goto BreakWhile;
                            if (skip) continue;
                        }
                        break;
                }
                if (!regex.MoveNext()) break;
            }
    BreakWhile:
        if (expectsClosedBracket)
            throw new Exception("Expects ')'");
        return (startState, currentEndState);
    }
    static (NFAState startState, NFAState endState) GenerateClass(IEnumerator<char> regex, int Rule, int order)
    {
        NFAState startState = new(Rule, order);
        NFAState endState = new(Rule, order);
        void AddChar(char c)
        {
            startState[c].Add(endState);
        }
        char? previous = null;
        while (regex.MoveNext() && regex.Current != ']')
        {
            char current = GetChar(regex);
            if (current == '-')
            {
                if (previous == null) throw new Exception("Range: Expects a character before '-'");
                if (!regex.MoveNext()) throw new Exception("Range: Expects a character after '-'");
                current = GetChar(regex);
                if (current == '-') throw new Exception("Range: unexpected '--'");
                for (char c = previous.Value; c <= current; c++)
                {
                    AddChar(c);
                }
                previous = null;
            }
            else
            {
                previous = current;
                AddChar(current);
            }
        }
        return (startState, endState);
    }
    class NFAState(int rule, int order)
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
        public HashSet<NFAState> Epsilon { get => _epsilon ??= []; }
        public T? Value { get; set; }
        public bool IsAccepting => Value != null;
        public override string ToString()
        {
            return $"({Value as object ?? "null"}) => {{{string.Join(", ", from key in (_epsilon != null ? Transitions.Keys.Append('ε') : Transitions.Keys) select $"'{key}'")}}}";
        }
    }
}
public record class RegexVal<T>([StringSyntax(StringSyntaxAttribute.Regex)] string Regex, T? Value, int Order = 0) where T : class;
public class RegexCompilerException : Exception
{
    public RegexCompilerException(string message) : base(message) { }
    protected RegexCompilerException(string message, RegexCompilerException innerException) : base(message, innerException) { }
}
public class MultiRegexCompilerException(int id1, RegexCompilerException innerException) : RegexCompilerException($"Rule {id1} is invalid: {innerException.Message}", innerException);