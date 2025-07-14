using Get.Lexer;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Get.LangSupport;


public partial class TextmateGrammarMetadata
{
    private static readonly Regex LanguageIdRegex = _LanguageIdRegex();
    private static readonly Regex FileExtensionRegex = _FileExtensionRegex();

    public required string LanguageId
    {
        get;
        init => field = LanguageIdRegex.IsMatch(value)
            ? value
            : throw new ArgumentException($"Invalid LanguageId '{value}'. Must match: {LanguageIdRegex}");
    }

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

    public string ScopeName => $"source.{LanguageId}";

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

    public string GetGrammarJSON(Dictionary<string, object> repository)
    {
        var grammar = new
        {
            scopeName = ScopeName,
            patterns = new object[]
            {
                new { include = "#main" }
            },
            repository
        };

        return JsonSerializer.Serialize(grammar, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    [GeneratedRegex(@"^\.\w[\w.-]*$", RegexOptions.Compiled)]
    private static partial Regex _FileExtensionRegex();
    [GeneratedRegex("^[a-z0-9-]+$", RegexOptions.Compiled)]
    private static partial Regex _LanguageIdRegex();
}

public static class TextmateGrammarGenerator
{
    public static Dictionary<string, object> GenerateRepository<TLexer>()
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

        // Priority -> List of rules with that priority, preserving order
        var rulesByPriority = new SortedDictionary<int, List<Dictionary<string, object>>>(Comparer<int>.Create((a, b) => b.CompareTo(a))); // descending order

        foreach (var field in terminalType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            foreach (var scopeAttr in field.GetCustomAttributes<TextmateScopeAttribute>())
            {
                List<string> regexes = new();

                if (scopeAttr.Regexes != null)
                {
                    regexes.AddRange(scopeAttr.Regexes);
                }
                else
                {
                    var regexAttrs = field.GetCustomAttributes<RegexAttribute>();
                    regexes.AddRange(regexAttrs.Select(x => x.InputRegex));
                }

                if (scopeAttr.Begin != null && scopeAttr.End != null)
                {
                    // begin/end rule (no regexes)
                    var rule = new Dictionary<string, object>
                    {
                        ["name"] = scopeAttr.Scope,
                        ["begin"] = scopeAttr.Begin,
                        ["end"] = scopeAttr.End
                    };

                    if (scopeAttr.InsideIncludes is { Length: > 0 })
                    {
                        rule["patterns"] = scopeAttr.InsideIncludes
                            .Select(include => new Dictionary<string, string> { ["include"] = include })
                            .ToArray();
                    }

                    if (!rulesByPriority.TryGetValue(scopeAttr.Priority, out var list))
                    {
                        list = new List<Dictionary<string, object>>();
                        rulesByPriority[scopeAttr.Priority] = list;
                    }
                    list.Add(rule);
                }
                else
                {
                    // one rule per regex
                    foreach (var regex in regexes)
                    {
                        var rule = new Dictionary<string, object>
                        {
                            ["name"] = scopeAttr.Scope,
                            ["match"] = scopeAttr.AddBoundary ? @$"\b{regex}\b" : regex
                        };

                        if (!rulesByPriority.TryGetValue(scopeAttr.Priority, out var list))
                        {
                            list = new List<Dictionary<string, object>>();
                            rulesByPriority[scopeAttr.Priority] = list;
                        }
                        list.Add(rule);
                    }
                }
            }
        }

        // Flatten all rules into a single list ordered by priority descending
        var allRules = rulesByPriority.Values.SelectMany(rules => rules).ToArray();

        // Construct repository dictionary with main -> { patterns = [...] }
        var repository = new Dictionary<string, object>
        {
            ["main"] = new Dictionary<string, object>
            {
                ["patterns"] = allRules
            }
        };

        return repository;
    }
}
