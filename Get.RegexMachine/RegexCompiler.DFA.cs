﻿namespace Get.RegexMachine;

partial class RegexCompiler<T>
{
    // Code Generated by Gemini with some modifications
    static DFAState ConvertToDFA(NFAState startState, RegexConflictBehavior conflictBehavior)
    {
        var unprocessedStates = new Queue<HashSet<NFAState>>();
        var processedStates = new Dictionary<HashSet<NFAState>, DFAState>(new HashSetEqualityComparer());
        var nextId = 0;

        // Start with the epsilon closure of the start state
        var initialState = EpsilonClosure([startState]);
        unprocessedStates.Enqueue(initialState);

        while (unprocessedStates.Count > 0)
        {
            var currentState = unprocessedStates.Dequeue();

            // Check if the state has already been processed
            if (!processedStates.TryGetValue(currentState, out var dfaState))
            {
                // Create a new DFA state
                processedStates[currentState] = dfaState = new DFAState(nextId++, GetValue(currentState, conflictBehavior));
            }

            // For each input symbol, calculate the epsilon closure of the next states
            foreach (var symbol in currentState.SelectMany(s => s.TransitionKeys).Distinct())
            {
                var nextState = new HashSet<NFAState>(EpsilonClosure(currentState.SelectMany(s => s[symbol])));
                if (!processedStates.TryGetValue(nextState, out var a))
                {
                    unprocessedStates.Enqueue(nextState);
                    processedStates[nextState] = a = new DFAState(nextId++, GetValue(nextState, conflictBehavior));
                }
                dfaState.Transitions[symbol] = a;
            }
        }

        return processedStates[initialState];
    }
    // Code Generated by Gemini
    static T? GetValue(HashSet<NFAState> states, RegexConflictBehavior conflictBehavior)
    {
        NFAState? maxState = null;
        int maxOrder = int.MinValue;

        int maxOrderCount = 0;
        foreach (var state in states)
        {
            if (state.IsAccepting)
            {
                if (state.Order > maxOrder)
                {
                    maxOrderCount = 1;
                    maxOrder = state.Order;
                    maxState = state;
                } else
                {
                    maxOrderCount++;
                }
            }
        }

        if (maxState == null)
        {
            return default;
        }

        if (conflictBehavior == RegexConflictBehavior.Last)
        {
            return maxState.Value;
        }
        if (maxOrderCount > 1)
        {
            throw new RegexCompilerException($"Conflict Detected! Id = {string.Join(", ", states.Where(s => s.IsAccepting && s.Order == maxOrder))}");
        }
        return maxState.Value;
    }
    // Code Generated by Gemini
    class HashSetEqualityComparer : IEqualityComparer<HashSet<NFAState>>
    {
        public bool Equals(HashSet<NFAState>? x, HashSet<NFAState>? y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }

            return x.SetEquals(y);
        }

        public int GetHashCode(HashSet<NFAState> obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (var state in obj)
                {
                    hash = hash * 31 + state.GetHashCode();
                }
                return hash;
            }
        }
    }
    // Code Generated by Gemini with some modifications
    // Helper function to calculate the epsilon closure of a set of NFA states
    static HashSet<NFAState> EpsilonClosure(IEnumerable<NFAState> states)
    {
        var closure = new HashSet<NFAState>(states);
        var worklist = new Queue<NFAState>(states);

        while (worklist.Count > 0)
        {
            var state = worklist.Dequeue();
            foreach (var epsilonState in state.Epsilon)
            {
                if (closure.Add(epsilonState))
                {
                    worklist.Enqueue(epsilonState);
                }
            }
        }

        return closure;
    }
    
    public class DFAState(int id, T? value)
    {
        public Dictionary<char, DFAState> Transitions { get; } = [];
        public int Id { get; } = id;
        public T? Value { get; internal set; } = value;
        public bool IsAccepting => Value != null;
        public override string ToString()
        {
            return $"{Id} ({Value as object ?? "null"}) => {{{string.Join(", ", from key in Transitions.Keys select $"'{key}'")}}}";
        }
    }
}

public class RuleConflictException(int id1, int id2) : Exception($"Rule {id1} and Rule {id2} conflicts!");
