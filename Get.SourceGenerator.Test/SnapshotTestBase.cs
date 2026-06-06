using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;

namespace Get.SourceGenerator.Test;

public abstract class SnapshotTestBase
{
    protected static CSharpCompilation CreateCompilation(string source, CSharpCompilationOptions? options = null)
    {
        var references = GetMetadataReferences();
        return CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            options ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    private static (Assembly, Type) LoadGeneratorType(string projectName, string dllName, string typeName)
    {
        var dllPath = GetSourceGeneratorDllPath(projectName, dllName);
        var assembly = Assembly.LoadFrom(dllPath);
        var type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type {typeName} not found in {dllPath}");
        return (assembly, type);
    }

    protected static IIncrementalGenerator LoadParserGenerator()
    {
        var (assembly, type) = LoadGeneratorType("Get.Parser.SourceGenerator", "Get.Parser.SourceGenerator.dll", "Get.Parser.SourceGenerator.ParserGenerator");
        var instance = Activator.CreateInstance(type, nonPublic: true)!;
        return (IIncrementalGenerator)instance;
    }

    protected static IIncrementalGenerator LoadLexerGenerator()
    {
        var (assembly, type) = LoadGeneratorType("Get.Lexer.SourceGenerator", "Get.Lexer.SourceGenerator.dll", "Get.Lexer.SourceGenerator.LexerGenerator");
        var instance = Activator.CreateInstance(type, nonPublic: true)!;
        return (IIncrementalGenerator)instance;
    }

    protected static GeneratorDriverRunResult RunGenerator(CSharpCompilation compilation, IIncrementalGenerator generator)
    {
        var sourceGenerator = generator.AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(ImmutableArray.Create(sourceGenerator));
        driver = driver.RunGenerators(compilation, CancellationToken.None);
        return driver.GetRunResult();
    }

    protected static IReadOnlyList<(string HintName, string Source)> GetGeneratedSources(GeneratorDriverRunResult result)
    {
        return result.Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => (s.HintName, s.SourceText.ToString()))
            .ToList();
    }

    protected static void MatchSnapshot(string actual, string name)
    {
        var dir = FindTestProjectDir();
        var path = Path.Combine(dir, "Snapshots", $"{name}.cs.snap");

        if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
            return;
        }

        if (!File.Exists(path))
            throw new AssertFailedException(
                $"Snapshot not found: {path}{Environment.NewLine}" +
                $"Set UPDATE_SNAPSHOTS=1 and re-run to create it.");

        var expected = File.ReadAllText(path);
        Assert.AreEqual(
            NormalizeLineEndings(expected),
            NormalizeLineEndings(actual),
            $"Snapshot mismatch: {name}");
    }

    protected static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n");
    }

    private static string FindTestProjectDir()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !dir.EnumerateFiles("*.csproj").Any(f => f.Name.StartsWith("Get.SourceGenerator.Test")))
            dir = dir.Parent;
        return dir?.FullName
            ?? throw new DirectoryNotFoundException("Cannot find Get.SourceGenerator.Test project directory");
    }

    private static string GetSourceGeneratorDllPath(string projectName, string dllName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, projectName)))
            dir = dir.Parent;
        if (dir == null)
            throw new DirectoryNotFoundException($"Cannot find solution root from {baseDir}");
        return Path.Combine(dir.FullName, projectName, "bin", "Debug", "netstandard2.0", dllName);
    }

    private static MetadataReference[] GetMetadataReferences()
    {
        var refs = new List<MetadataReference>();

        // System references from loaded assemblies
        var systemAssemblies = new[]
        {
            typeof(object).Assembly,
            typeof(System.Collections.Generic.List<>).Assembly,
            typeof(System.Linq.Enumerable).Assembly,
            typeof(System.ComponentModel.EditorBrowsableAttribute).Assembly,
            typeof(System.Threading.CancellationToken).Assembly,
            typeof(System.Text.StringBuilder).Assembly,
            typeof(ImmutableArray).Assembly,
            typeof(Compilation).Assembly,
            typeof(CSharpCompilation).Assembly,
        };

        foreach (var asm in systemAssemblies)
            refs.Add(MetadataReference.CreateFromFile(asm.Location));

        // netstandard reference (required by netstandard2.0 dependencies)
        try
        {
            var netstandard = Assembly.Load("netstandard");
            refs.Add(MetadataReference.CreateFromFile(netstandard.Location));
        }
        catch { }

        // System.Runtime reference (provides netstandard facade on .NET 8)
        try
        {
            var systemRuntime = Assembly.Load("System.Runtime");
            refs.Add(MetadataReference.CreateFromFile(systemRuntime.Location));
        }
        catch { }

        // Project assemblies (non-source-generator)
        var projectTypes = new[]
        {
            typeof(Get.Parser.ParserBase<,,>),
            typeof(Get.PLShared.IToken<>),
            typeof(Get.RegexMachine.RegexCompiler<>),
            typeof(Get.Lexer.LexerBase<,>),
        };

        foreach (var t in projectTypes)
            if (t.Assembly.Location is { } loc && !string.IsNullOrEmpty(loc))
                refs.Add(MetadataReference.CreateFromFile(loc));

        return refs
            .GroupBy(r => r is PortableExecutableReference pe ? pe.FilePath : r.Display)
            .Select(g => g.First())
            .ToArray();
    }
}
