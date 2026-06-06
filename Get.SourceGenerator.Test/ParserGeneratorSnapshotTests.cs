namespace Get.SourceGenerator.Test;

[TestClass]
public class ParserGeneratorSnapshotTests : SnapshotTestBase
{
    [TestMethod]
    public void Generator_CanBeLoaded()
    {
        var generator = LoadParserGenerator();
        Assert.IsNotNull(generator);
    }

    [TestMethod]
    public void Generator_RunsWithoutError()
    {
        var source = """
            using Get.Parser;
            using static TestParser.Terminal;
            using static TestParser.NonTerminal;

            [Parser(StartNode)]
            [Precedence(Plus, Associativity.Left)]
            public partial class TestParser : ParserBase<TestParser.Terminal, TestParser.NonTerminal, int>
            {
                public enum Terminal { Plus, Number }
                public enum NonTerminal
                {
                    [Type<int>]
                    [Rule(Expr, AS, "val", nameof(Identity))]
                    StartNode,
                    [Type<int>]
                    [Rule(Expr, AS, "x", Plus, Expr, AS, "y", nameof(Add))]
                    [Rule(Number, AS, "val", nameof(Identity))]
                    Expr,
                }
                static int Add(int x, int y) => x + y;
                static T Identity<T>(T val) => val;
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = LoadParserGenerator();
        var result = RunGenerator(compilation, generator);

        Assert.AreEqual(0, result.Diagnostics.Length,
            $"Generator produced diagnostics: {string.Join("; ", result.Diagnostics.Select(d => d.GetMessage()))}");

        var sources = GetGeneratedSources(result);
        Assert.AreEqual(1, sources.Count);

        MatchSnapshot(sources[0].Source, "ParserGenerator_ValidInput");
    }

    [TestMethod]
    public void ParserWithPrecedence_GeneratesPrecedenceList()
    {
        var source = """
            using Get.Parser;
            using static PrecedenceParser.Terminal;
            using static PrecedenceParser.NonTerminal;

            [Parser(StartNode)]
            [Precedence(Times, Divide, Associativity.Left, Plus, Minus, Associativity.Left)]
            public partial class PrecedenceParser : ParserBase<PrecedenceParser.Terminal, PrecedenceParser.NonTerminal, int>
            {
                public enum Terminal { Plus, Minus, Times, Divide, Number }
                public enum NonTerminal
                {
                    [Type<int>]
                    [Rule(Expr, AS, "val", nameof(Identity))]
                    StartNode,
                    [Type<int>]
                    [Rule(Expr, AS, "x", Plus, Expr, AS, "y", nameof(Add))]
                    [Rule(Expr, AS, "x", Times, Expr, AS, "y", nameof(Multiply))]
                    [Rule(Number, AS, "val", nameof(Identity))]
                    Expr,
                }
                static int Add(int x, int y) => x + y;
                static int Multiply(int x, int y) => x * y;
                static T Identity<T>(T val) => val;
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = LoadParserGenerator();
        var result = RunGenerator(compilation, generator);

        var sources = GetGeneratedSources(result);
        Assert.AreEqual(1, sources.Count);

        MatchSnapshot(sources[0].Source, "ParserGenerator_PrecedenceGrammar");
    }

    [TestMethod]
    public void InvalidInput_NoAttribute_ProducesNoOutput()
    {
        var source = "public class NotAParser { }";
        var compilation = CreateCompilation(source);
        var generator = LoadParserGenerator();
        var result = RunGenerator(compilation, generator);

        var sources = GetGeneratedSources(result);
        Assert.AreEqual(0, sources.Count, "Generator should not produce output for unrelated classes");
    }
}
