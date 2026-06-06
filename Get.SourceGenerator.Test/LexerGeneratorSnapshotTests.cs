namespace Get.SourceGenerator.Test;

[TestClass]
public class LexerGeneratorSnapshotTests : SnapshotTestBase
{
    [TestMethod]
    public void Generator_CanBeLoaded()
    {
        var generator = LoadLexerGenerator();
        Assert.IsNotNull(generator);
    }

    [TestMethod]
    public void Generator_RunsWithoutError()
    {
        var source = """
            using Get.RegexMachine;
            using Get.Lexer;

            [Lexer<SampleLexer.Terminals>]
            partial class SampleLexer(ITextSeekable text) : LexerBase<
                SampleLexer.State,
                SampleLexer.Terminals
            >(text, State.Initial)
            {
                private partial int BuildInt()
                {
                    return int.Parse(MatchedText);
                }
                partial void SkipWhitespace() { }
                public enum State { Initial }
                [CompileTimeConflictCheck]
                public enum Terminals
                {
                    [Type<int>]
                    [Regex<int>(@"[0-9]+", "BuildInt")]
                    Integer,
                    [Regex(@"\+")]
                    Plus,
                    [Regex(@"\-")]
                    Minus,
                    [Regex(@"\s+", ShouldReturnToken = false)]
                    Whitespace,
                }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = LoadLexerGenerator();
        var result = RunGenerator(compilation, generator);

        Assert.AreEqual(0, result.Diagnostics.Length,
            $"Generator produced diagnostics: {string.Join("; ", result.Diagnostics.Select(d => d.GetMessage()))}");

        var sources = GetGeneratedSources(result);
        Assert.AreEqual(1, sources.Count);

        MatchSnapshot(sources[0].Source, "LexerGenerator_ValidInput");
    }

    [TestMethod]
    public void InvalidInput_NoAttribute_ProducesNoOutput()
    {
        var source = "public class NotALexer { }";
        var compilation = CreateCompilation(source);
        var generator = LoadLexerGenerator();
        var result = RunGenerator(compilation, generator);

        var sources = GetGeneratedSources(result);
        Assert.AreEqual(0, sources.Count, "Generator should not produce output for unrelated classes");
    }
}
