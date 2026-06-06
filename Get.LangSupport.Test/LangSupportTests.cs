using Get.Lexer;
using System.Text.Json;

namespace Get.LangSupport.Test;

[TestClass]
public class LangSupportTests
{
    static TextmateGrammarMetadata CreateMetadata() => new()
    {
        LanguageId = "testlang",
        LanguageExtensions = [".testlang"]
    };

    [TestMethod]
    public void TextmateGrammarMetadata_ValidInput_CreatesMetadata()
    {
        var metadata = CreateMetadata();
        Assert.AreEqual("testlang", metadata.LanguageId);
        Assert.AreEqual(".testlang", metadata.LanguageExtensions[0]);
        Assert.AreEqual("source.testlang", metadata.ScopeName);
    }

    [TestMethod]
    public void TextmateGrammarMetadata_InvalidLanguageId_Throws()
    {
        Assert.ThrowsException<ArgumentException>(() => new TextmateGrammarMetadata
        {
            LanguageId = "Invalid Lang!",
            LanguageExtensions = [".test"]
        });
    }

    [TestMethod]
    public void TextmateGrammarMetadata_InvalidExtension_Throws()
    {
        Assert.ThrowsException<ArgumentException>(() => new TextmateGrammarMetadata
        {
            LanguageId = "testlang",
            LanguageExtensions = ["test"]
        });
    }

    [TestMethod]
    public void GetContributionsJSON_ReturnsValidJson()
    {
        var json = CreateMetadata().GetContributionsJSON();
        using var doc = JsonDocument.Parse(json);
        var contributes = doc.RootElement.GetProperty("contributes");
        var languages = contributes.GetProperty("languages");
        Assert.AreEqual("testlang", languages[0].GetProperty("id").GetString());
        var extensions = languages[0].GetProperty("extensions");
        Assert.AreEqual(".testlang", extensions[0].GetString());
        var grammars = contributes.GetProperty("grammars");
        Assert.AreEqual("source.testlang", grammars[0].GetProperty("scopeName").GetString());
    }

    [TestMethod]
    public void GenerateRepository_CustomLexer_ReturnsMainPatterns()
    {
        var repo = TextmateGrammarGenerator.GenerateRepository<CustomLexerSourceGen>();
        Assert.IsTrue(repo.ContainsKey("main"));
        var main = repo["main"];
        Assert.IsTrue(main.ContainsKey("patterns"));
        var patterns = main["patterns"];
        Assert.IsTrue(patterns.Count > 0);
    }

    [TestMethod]
    public void GetGrammarJSON_ReturnsValidJsonWithPatterns()
    {
        var metadata = CreateMetadata();
        var repo = TextmateGrammarGenerator.GenerateRepository<CustomLexerSourceGen>();
        var json = metadata.GetGrammarJSON(repo);
        using var doc = JsonDocument.Parse(json);

        Assert.AreEqual("source.testlang", doc.RootElement.GetProperty("scopeName").GetString());
        Assert.IsTrue(doc.RootElement.TryGetProperty("patterns", out var patterns));
        Assert.IsTrue(patterns.GetArrayLength() > 0);
        Assert.IsTrue(doc.RootElement.TryGetProperty("repository", out _));
    }

    [TestMethod]
    public void GetGrammarJSON_ContainsIntegerPattern()
    {
        var metadata = CreateMetadata();
        var repo = TextmateGrammarGenerator.GenerateRepository<CustomLexerSourceGen>();
        var json = metadata.GetGrammarJSON(repo);
        using var doc = JsonDocument.Parse(json);
        var repository = doc.RootElement.GetProperty("repository");
        var main = repository.GetProperty("main");
        var patterns = main.GetProperty("patterns");

        var hasNumericPattern = false;
        foreach (var p in patterns.EnumerateArray())
        {
            if (p.TryGetProperty("match", out var match) &&
                match.GetString() == @"[0-9]+")
            {
                hasNumericPattern = true;
                break;
            }
        }
        Assert.IsTrue(hasNumericPattern, "Expected a pattern matching [0-9]+");
    }

    [TestMethod]
    public void GetGrammarJSON_ContainsOperatorPatterns()
    {
        var metadata = CreateMetadata();
        var repo = TextmateGrammarGenerator.GenerateRepository<CustomLexerSourceGen>();
        var json = metadata.GetGrammarJSON(repo);
        using var doc = JsonDocument.Parse(json);
        var repository = doc.RootElement.GetProperty("repository");
        var main = repository.GetProperty("main");
        var patterns = main.GetProperty("patterns");

        var operatorScopes = new List<string>();
        foreach (var p in patterns.EnumerateArray())
        {
            if (p.TryGetProperty("name", out var name))
                operatorScopes.Add(name.GetString()!);
        }

        Assert.IsTrue(operatorScopes.Contains("keyword.operator.arithmetic"),
            "Expected keyword.operator.arithmetic for operators");
    }

    [TestMethod]
    public void GetGrammarJSON_IncludeOrder_MainLast()
    {
        var metadata = CreateMetadata();
        var repo = TextmateGrammarGenerator.GenerateRepository<CustomLexerSourceGen>();
        var json = metadata.GetGrammarJSON(repo);
        using var doc = JsonDocument.Parse(json);
        var patterns = doc.RootElement.GetProperty("patterns");

        var includes = patterns.EnumerateArray()
            .Select(p => p.GetProperty("include").GetString())
            .ToList();

        Assert.IsTrue(includes.Count > 0);
        Assert.AreEqual("#main", includes.Last());
    }
}
