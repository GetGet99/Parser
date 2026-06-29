using Get.Lexer;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Get.LangSupport;


/// <summary>
/// Metadata describing a TextMate grammar language definition.
/// Used with <see cref="TextmateGrammarGenerator"/> to produce VS Code-compatible
/// TextMate grammar JSON and package contributions.
/// </summary>
public partial class TextmateGrammarMetadata
{

    /// <summary>
    /// The language identifier (e.g. <c>"my-lang"</c>).
    /// Must match <c>^[a-z0-9-]+$</c>.
    /// </summary>
    public required string LanguageId
    {
        get;
        init => field = LanguageIdRegex.IsMatch(value)
            ? value
            : throw new ArgumentException($"Invalid LanguageId '{value}'. Must match: {LanguageIdRegex}");
    }

    /// <summary>
    /// File extensions associated with this language (e.g. <c>".my"</c>, <c>".mylang"</c>).
    /// Must match <c>^\.[\w.-]+$</c> and cannot be empty.
    /// </summary>
    public required string[] LanguageExtensions
    {
        get;
        init
        {
            if (value.Length == 0)
                throw new ArgumentException("LanguageExtensions cannot be empty.");

            foreach (var ext in value)
            {
                if (!FileExtensionRegex.IsMatch(ext))
                    throw new ArgumentException($"Invalid file extension '{ext}'. Must match: {FileExtensionRegex}");
            }

            field = value;
        }
    }

    /// <summary>The TextMate scope name derived from <see cref="LanguageId"/> (e.g. <c>source.my-lang</c>).</summary>
    public string ScopeName => $"source.{LanguageId}";

    /// <summary>
    /// Returns the VS Code <c>package.json</c> contributions snippet for this language.
    /// </summary>
    public string GetContributionsJSON()
    {
        var extensionsJson = string.Join(", ", LanguageExtensions.Select(ext => $"\"{ext}\""));

        return $$"""
        {
          "contributes": {
            "languages": [
              {
                "id": "{{LanguageId}}",
                "extensions": [{{extensionsJson}}]
              }
            ],
            "grammars": [
              {
                "language": "{{LanguageId}}",
                "scopeName": "{{ScopeName}}",
                "path": "./syntaxes/{{LanguageId}}.tmGrammar.json"
              }
            ]
          }
        }
        """;
    }

    /// <summary>
    /// Generates the full TextMate grammar JSON for this language using the given repository.
    /// </summary>
    /// <param name="repository">The repository of grammar rules.</param>
    public string GetGrammarJSON<T>(StringDict<T> repository) =>
        GetGrammarJSON(repository, additionalEntries: null, repositoryIncludeOrder: null);

    /// <summary>
    /// Generates the full TextMate grammar JSON with optional additional entries and include ordering.
    /// </summary>
    /// <param name="repository">The repository of grammar rules.</param>
    /// <param name="additionalEntries">Optional additional entries to merge into the repository.</param>
    /// <param name="repositoryIncludeOrder">Order of repository keys in the <c>patterns</c> array. Defaults to alphabetical with <c>"main"</c> last.</param>
    public string GetGrammarJSON<T>(
        StringDict<T> repository,
        StringDict<T>? additionalEntries,
        IReadOnlyList<string>? repositoryIncludeOrder = null)
    {
        if (additionalEntries != null)
        {
            foreach (var kvp in additionalEntries)
                repository[kvp.Key] = kvp.Value;
        }

        var includeOrder = repositoryIncludeOrder ?? GetDefaultRepositoryIncludeOrder(repository.Keys);
        var grammar = new
        {
            scopeName = ScopeName,
            patterns = includeOrder
                .Where(repository.ContainsKey)
                .Select(key => (object)new { include = $"#{key}" })
                .ToArray(),
            repository
        };

        return JsonSerializer.Serialize(grammar, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Fallback when <paramref name="repositoryIncludeOrder"/> is not supplied:
    /// all keys alphabetically, with <c>main</c> last if present.
    /// </summary>
    private static List<string> GetDefaultRepositoryIncludeOrder(IEnumerable<string> keys) =>
        keys.OrderBy(k => k == "main" ? 1 : 0)
            .ThenBy(k => k, StringComparer.Ordinal)
            .ToList();
#if NET7_0_OR_GREATER
    private static readonly Regex LanguageIdRegex = _LanguageIdRegex();
    private static readonly Regex FileExtensionRegex = _FileExtensionRegex();
    [GeneratedRegex(@"^\.\w[\w.-]*$", RegexOptions.Compiled)]
    private static partial Regex _FileExtensionRegex();
    [GeneratedRegex("^[a-z0-9-]+$", RegexOptions.Compiled)]
    private static partial Regex _LanguageIdRegex();
#else

    private static readonly Regex LanguageIdRegex = new(@"^\.\w[\w.-]*$");
    private static readonly Regex FileExtensionRegex = new("^[a-z0-9-]+$");
#endif
}

/// <summary>
/// Generates TextMate grammar repositories from lexer types annotated with
/// <see cref="TextmateScopeAttribute"/> and related attributes.
/// </summary>
public static class TextmateGrammarGenerator
{
    /// <summary>
    /// Generates the grammar repository by reflecting over <typeparamref name="TLexer"/>.
    /// </summary>
    /// <typeparam name="TLexer">The lexer type (typically generated by the source generator).</typeparam>
    public static StringDict<StringDict<List<StringDict<object>>>> GenerateRepository<TLexer>() =>
        GenerateRepository<TLexer>(additionalEntries: null);

    /// <summary>
    /// Generates the grammar repository with additional entries merged in.
    /// </summary>
    /// <typeparam name="TLexer">The lexer type (typically generated by the source generator).</typeparam>
    /// <param name="additionalEntries">Optional additional repository entries to merge.</param>
    public static StringDict<StringDict<List<StringDict<object>>>> GenerateRepository<TLexer>(
        StringDict<StringDict<List<StringDict<object>>>>? additionalEntries)
    {
        var type = typeof(TLexer);
        var lexerAttr = type.GetCustomAttributes()
            .FirstOrDefault(attr => attr.GetType().IsGenericType &&
                                    attr.GetType().GetGenericTypeDefinition() == typeof(LexerAttribute<>));
        if (lexerAttr == null)
            throw new InvalidDataException($"The given type {type.Name} does not have a LexerAttribute<>.");

        var terminalType = lexerAttr.GetType().GetGenericArguments()[0];
        if (!terminalType.IsEnum)
            throw new InvalidDataException("The type in the generic parameter must be an enum.");

        // repositoryKey -> priority -> rules
        var rulesByRepoAndPriority = new Dictionary<string, SortedDictionary<int, List<StringDict<object>>>>(
            StringComparer.Ordinal);

        foreach (var field in terminalType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            foreach (var scopeAttr in field.GetCustomAttributes<TextmateScopeAttribute>())
            {
                var repoKey = scopeAttr.RepositoryKey;
                if (!rulesByRepoAndPriority.TryGetValue(repoKey, out var rulesByPriority))
                {
                    rulesByPriority = new SortedDictionary<int, List<StringDict<object>>>(
                        Comparer<int>.Create((a, b) => b.CompareTo(a)));
                    rulesByRepoAndPriority[repoKey] = rulesByPriority;
                }

                if (scopeAttr.Begin != null && scopeAttr.End != null)
                {
                    var rule = BuildBeginEndRule(scopeAttr);
                    AddRule(rulesByPriority, scopeAttr.Priority, rule);
                }
                else
                {
                    var regexes = CollectRegexes(field, scopeAttr);
                    if (scopeAttr.DeduplicateRegexes)
                        regexes = regexes.Distinct().ToList();

                    foreach (var regex in regexes)
                    {
                        var rule = BuildMatchRule(scopeAttr, regex);
                        AddRule(rulesByPriority, scopeAttr.Priority, rule);
                    }
                }
            }
        }

        var repository = new StringDict<StringDict<List<StringDict<object>>>>();
        foreach (var kvp in rulesByRepoAndPriority)
        {
            var patterns = kvp.Value.Values.SelectMany(rules => rules).ToList();
            DeduplicateIdenticalRules(patterns);
            repository[kvp.Key] = new StringDict<List<StringDict<object>>>
            {
                ["patterns"] = patterns
            };
        }

        if (additionalEntries != null)
        {
            foreach (var kvp in additionalEntries)
                repository[kvp.Key] = kvp.Value;
        }

        if (!repository.ContainsKey("main"))
        {
            repository["main"] = new StringDict<List<StringDict<object>>>
            {
                ["patterns"] = []
            };
        }

        return repository;
    }

    private static List<string> CollectRegexes(FieldInfo field, TextmateScopeAttribute scopeAttr)
    {
        if (scopeAttr.Regexes != null)
            return scopeAttr.Regexes.ToList();

        return field.GetCustomAttributes<RegexAttribute>()
            .Select(x => x.InputRegex)
            .ToList();
    }

    private static StringDict<object> BuildMatchRule(TextmateScopeAttribute scopeAttr, string regex)
    {
        var rule = new StringDict<object>
        {
            ["name"] = scopeAttr.Scope,
            ["match"] = ApplyBoundary(scopeAttr.AddBoundary, regex)
        };

        AddCaptures(rule, "captures", scopeAttr.MatchCaptures);
        return rule;
    }

    private static StringDict<object> BuildBeginEndRule(TextmateScopeAttribute scopeAttr)
    {
        var ruleName = scopeAttr.EmbeddedLanguage != null
            ? $"meta.embedded.{scopeAttr.EmbeddedLanguage}"
            : scopeAttr.Scope;

        var rule = new StringDict<object>
        {
            ["name"] = ruleName,
            ["begin"] = scopeAttr.Begin!,
            ["end"] = scopeAttr.End!
        };

        AddCaptures(rule, "beginCaptures", scopeAttr.BeginCaptures);
        AddCaptures(rule, "endCaptures", scopeAttr.EndCaptures);

        var insidePatterns = BuildInsidePatterns(scopeAttr);
        if (insidePatterns.Length > 0)
            rule["patterns"] = insidePatterns;
        return rule;
    }

    private static object[] BuildInsidePatterns(TextmateScopeAttribute scopeAttr)
    {
        var patterns = new List<StringDict<object>>();

        if (scopeAttr.EmbeddedLanguage != null)
        {
            patterns.Add(new StringDict<object>
            {
                ["include"] = scopeAttr.EmbeddedGrammarScope ?? $"source.{scopeAttr.EmbeddedLanguage}"
            });
        }
        else if (scopeAttr.ContentScope != null)
        {
            patterns.Add(new StringDict<object>
            {
                ["match"] = @"(?s).+",
                ["name"] = scopeAttr.ContentScope
            });
        }

        if (scopeAttr.InsideIncludes is { Length: > 0 })
        {
            patterns.AddRange(scopeAttr.InsideIncludes.Select(include => new StringDict<object>
            {
                ["include"] = include
            }));
        }

        return patterns.ToArray();
    }

    private static void AddCaptures(
        StringDict<object> rule,
        string key,
        Dictionary<string, string>? captures)
    {
        if (captures is not { Count: > 0 })
            return;

        var captureDict = new StringDict<StringDict<string>>();
        foreach (var kvp in captures)
            captureDict[kvp.Key] = new StringDict<string> { ["name"] = kvp.Value };

        rule[key] = captureDict;
    }

    private static string ApplyBoundary(bool addBoundary, string regex)
    {
        if (!addBoundary)
            return regex;

        if (regex.Length == 0)
            return regex;

        var start = regex[0];
        var end = regex[^1];
        if (char.IsLetterOrDigit(start) && char.IsLetterOrDigit(end))
            return $@"\b{regex}\b";

        return regex;
    }

    private static void AddRule(
        SortedDictionary<int, List<StringDict<object>>> rulesByPriority,
        int priority,
        StringDict<object> rule)
    {
        if (!rulesByPriority.TryGetValue(priority, out var list))
        {
            list = [];
            rulesByPriority[priority] = list;
        }
        list.Add(rule);
    }

    private static void DeduplicateIdenticalRules(List<StringDict<object>> rules)
    {
        var seen = new HashSet<string>();
        for (var i = rules.Count - 1; i >= 0; i--)
        {
            var key = JsonSerializer.Serialize(rules[i]);
            if (!seen.Add(key))
                rules.RemoveAt(i);
        }
    }
}

/// <summary>A dictionary keyed by string with <c>InvariantCultureIgnoreCase</c>-like semantics (inherits default <see cref="Dictionary{TKey, TValue}"/>).</summary>
public class StringDict<T> : Dictionary<string, T> { }
