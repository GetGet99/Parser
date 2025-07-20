#pragma warning disable IDE0240
#nullable enable
#pragma warning restore IDE0240
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Get.EasyCSharp.GeneratorTools;
abstract class AttributeBaseAnalyzer<TAttribute1, TAttributeDataType1, TSyntaxNode, TSymbol>(SyntaxKind syntaxKind) : DiagnosticAnalyzer
    // The User-Defined Attribute Type to process
    where TAttribute1 : Attribute
    // TAttributeDataType1: The Attribute Type to be used for AttributeData to custom struct
    // The SyntaxNode type to process
    where TSyntaxNode : MemberDeclarationSyntax
    // The Symbol type to process
    where TSymbol : ISymbol
{
    protected virtual bool CountAttributeSubclass => true;
    static readonly string FullAttributeName;
    static AttributeBaseAnalyzer()
    {
        var fn = typeof(TAttribute1).FullName;
        //var idx = fn.IndexOf('`');
        //if (idx != -1) // generic type, we ignore all generic variable
        //    FullAttributeName = fn[..idx];
        //else
        FullAttributeName = fn;
    }

    protected virtual void OnInitialize(IncrementalGeneratorPostInitializationContext context) { }
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction((genContext) =>
        {
#if DEBUG
            //System.Diagnostics.Debugger.Launch();
            DateTime TransformBegin = DateTime.Now;
#endif
            var syntaxNode = (TSyntaxNode)genContext.Node;
            // Filter out everything which has no attribute
            if (syntaxNode.AttributeLists.Count is 0) return;

            // Get Symbol
            var symbols = GetSymbols(genContext, syntaxNode).ToArray();
            if (symbols.Length is 0) return;

            // Get Attributes
            var Class = genContext.SemanticModel.Compilation.GetTypeByMetadataName(FullAttributeName);
            var attributes = (
                from x in symbols[0].GetAttributes()
                where CountAttributeSubclass ?
                    x.AttributeClass?.IsSubclassFrom(Class) ?? false :
                    x.AttributeClass?.IsTheSameAs(Class) ?? false
                select (RealAttr: x, WrapperAttr: TransformAttribute(x, genContext.SemanticModel.Compilation))
            ).Where(x => x.RealAttr is not null && x.WrapperAttr is not null).ToArray();
            if (attributes.Length is 0) return;
            foreach (var symbol in symbols)
                OnPointVisit(new(genContext, syntaxNode, symbol, attributes, CancellationToken.None));
        }, syntaxKind);
    }
    protected abstract TAttributeDataType1? TransformAttribute(AttributeData attributeData, Compilation compilation);
    protected abstract void OnPointVisit(OnPointVisitArguments args);
    protected readonly record struct OnPointVisitArguments(
        SyntaxNodeAnalysisContext Context,
        TSyntaxNode SyntaxNode,
        TSymbol Symbol,
        (AttributeData Original, TAttributeDataType1 Wrapper)[] AttributeDatas,
        CancellationToken CancellationToken
    )
    {
        public void ReportDiagnostic(Diagnostic diagnostic)
            => Context.ReportDiagnostic(diagnostic);
    }
    protected virtual IEnumerable<TSymbol> GetSymbols(SyntaxNodeAnalysisContext genContext, TSyntaxNode syntaxNode)
    {
        if (genContext.SemanticModel.GetDeclaredSymbol(syntaxNode) is TSymbol symbol) yield return symbol;
    }
}
abstract class AttributeBaseGenerator<TAttribute1, TAttributeDataType1, TSyntaxNode, TSymbol> : IIncrementalGenerator
    // The User-Defined Attribute Type to process
    where TAttribute1 : Attribute
    // TAttributeDataType1: The Attribute Type to be used for AttributeData to custom struct
    // The SyntaxNode type to process
    where TSyntaxNode : MemberDeclarationSyntax
    // The Symbol type to process
    where TSymbol : ISymbol
{
    protected virtual bool CountAttributeSubclass => true;
    static readonly string FullAttributeName;
    static AttributeBaseGenerator()
    {
        var fn = typeof(TAttribute1).FullName;
        //var idx = fn.IndexOf('`');
        //if (idx != -1) // generic type, we ignore all generic variable
        //    FullAttributeName = fn[..idx];
        //else
        FullAttributeName = fn;
    }

    protected virtual void OnInitialize(IncrementalGeneratorPostInitializationContext context) { }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(OnInitialize);
        var output = context.SyntaxProvider.CreateSyntaxProvider(
            static (syntaxNode, cancelationToken) => syntaxNode is TSyntaxNode,
            Transform
        );
        output = output.Where(static x => x.FileName is not null);

        context.RegisterSourceOutput(output, (sourceProductionContext, value) =>
        {
            foreach (var diag in value.Item3)
                sourceProductionContext.ReportDiagnostic(diag);

            sourceProductionContext.AddSource(value.FileName!.Replace("?", "Nullable"), value.Content!);
        });
    }
    protected abstract TAttributeDataType1? TransformAttribute(AttributeData attributeData, Compilation compilation);
    protected abstract string? OnPointVisit(OnPointVisitArguments args);
    protected record struct OnPointVisitArguments(
        GeneratorSyntaxContext GenContext,
        TSyntaxNode SyntaxNode,
        TSymbol Symbol,
        (AttributeData Original, TAttributeDataType1 Wrapper)[] AttributeDatas,
        List<Diagnostic> Diagnostics,
        CancellationToken CancellationToken
    );
    (string? FileName, string? Content, Diagnostic[]) Transform(GeneratorSyntaxContext genContext, CancellationToken cancelationToken)
    {
#if DEBUG
        //System.Diagnostics.Debugger.Launch();
        DateTime TransformBegin = DateTime.Now;
#endif
        var syntaxNode = (TSyntaxNode)genContext.Node;
        // Filter out everything which has no attribute
        if (syntaxNode.AttributeLists.Count is 0) return (null, null, []);

        // Get Symbol
        var symbols = GetSymbols(genContext, syntaxNode).ToArray();
        if (symbols.Length is 0) return (null, null, []);

        // Get Attributes
        var Class = genContext.SemanticModel.Compilation.GetTypeByMetadataName(FullAttributeName);
        var attributes = (
            from x in symbols[0].GetAttributes()
            where CountAttributeSubclass ?
                x.AttributeClass?.IsSubclassFrom(Class) ?? false :
                x.AttributeClass?.IsTheSameAs(Class) ?? false
            select (RealAttr: x, WrapperAttr: TransformAttribute(x, genContext.SemanticModel.Compilation))
        ).Where(x => x.RealAttr is not null && x.WrapperAttr is not null).ToArray();
        if (attributes.Length is 0) return (null, null, []);
        string? output;
#if DEBUG
        DateTime BeforeProcess = DateTime.Now;
#endif
        // All conditions satistfy except for actual running generator
        List<Diagnostic> diagnostics = [];
        try
        {
            output = (from symbol in symbols select OnPointVisit(new(genContext, syntaxNode, symbol, attributes, diagnostics, cancelationToken))).JoinDoubleNewLine();
            if (output is null) return (null, null, [.. diagnostics]);
        }
        catch (Exception e)
        {
            // Log the exception
            output = $"""
            /*
                Exception Occured: {e.GetType().FullName}{e.Message}
                Messsage: {e.Message}
                Stack Trace:
                    {e.StackTrace.IndentWOF(2)}
            */
            """;
        }
#if DEBUG
        DateTime ProcessCompleted = DateTime.Now;
#endif
        // All conditions satisfy
        var containingClass = symbols[0] is INamedTypeSymbol nts ? nts : symbols[0].ContainingType;
        var genericParams = containingClass.TypeParameters;
        var classHeader =
            genericParams.Length is 0 ?
                containingClass.Name :
                $"{containingClass.Name}<{string.Join(", ", from x in genericParams select x.Name)}>";
#if DEBUG
        TimeSpan EntireProcess = ProcessCompleted - TransformBegin;
        TimeSpan SubProcess = ProcessCompleted - BeforeProcess;
#endif
        return ($"{string.Join(" ", from x in symbols select x.ToString().Replace('<', '[').Replace('>', ']'))}.g.cs",
#if DEBUG
            $"""
            // This Generator took {EntireProcess.TotalMilliseconds}ms ({EntireProcess.Ticks} ticks) in total
            // SubProcess took {SubProcess.TotalMilliseconds}ms ({SubProcess.Ticks} ticks)
            """ + Extension.InSourceNewLine +
#endif
            $$"""
            #nullable enable
            // Autogenerated for {{string.Join(", ", symbols)}}
            
            namespace {{containingClass.ContainingNamespace}}
            {
                partial {{containingClass.TypeKind switch
            {
                TypeKind.Interface => "interface",
                TypeKind.Struct => "struct",
                TypeKind.Class or _ => "class",
            }}} {{classHeader}}
                {
                    {{
                    // Original
                    /*
                    {{syntaxNode.ToString().IndentWOF(2)}}
                    */
                    ""}}
                    
                    {{output.IndentWOF(2)}}
                }
            }
            """, [.. diagnostics]);
    }
    protected virtual IEnumerable<TSymbol> GetSymbols(GeneratorSyntaxContext genContext, TSyntaxNode syntaxNode)
    {
        if (genContext.SemanticModel.GetDeclaredSymbol(syntaxNode) is TSymbol symbol) yield return symbol;
    }
}
abstract class AttributeBaseGenerator<TAttribute1, TAttributeDataType1, TAttribute2, TAttributeDataType2, TSyntaxNode, TSymbol> : IIncrementalGenerator
    // The User-Defined Attribute Type to process
    where TAttribute1 : Attribute
    // The Attribute Type to be used for AttributeData to custom struct
    where TAttributeDataType1 : struct
    // The User-Defined Attribute Type to process
    where TAttribute2 : Attribute
    // The Attribute Type to be used for AttributeData to custom struct
    where TAttributeDataType2 : struct
    // The SyntaxNode type to process
    where TSyntaxNode : MemberDeclarationSyntax
    // The Symbol type to process
    where TSymbol : ISymbol
{
    static readonly string FullAttribute1Name = typeof(TAttribute1).FullName;
    static readonly string FullAttribute2Name = typeof(TAttribute2).FullName;

    protected virtual void Initialize(IncrementalGeneratorPostInitializationContext context) { }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(Initialize);
        var output = context.SyntaxProvider.CreateSyntaxProvider(
            static (syntaxNode, cancelationToken) => syntaxNode is TSyntaxNode,
            Transform
        );
        output = output.Where(static x => x.FileName is not null);

        context.RegisterSourceOutput(output, (sourceProductionContext, value) =>
        {
            sourceProductionContext.AddSource(value.FileName!, value.Content!);
        });
    }
    protected abstract TAttributeDataType1 TransformAttribute1(AttributeData attributeData, Compilation compilation);
    protected abstract TAttributeDataType2 TransformAttribute2(AttributeData attributeData, Compilation compilation);
    protected abstract string? OnPointVisit(GeneratorSyntaxContext genContext, TSyntaxNode syntaxNode, TSymbol symbol, TAttributeDataType1[] attribute1Data, TAttributeDataType2[] attribute2Data);
    (string? FileName, string? Content) Transform(GeneratorSyntaxContext genContext, CancellationToken cancelationToken)
    {
#if DEBUG
        DateTime TransformBegin = DateTime.Now;
#endif
        var syntaxNode = (TSyntaxNode)genContext.Node;
        // Filter out everything which has no attribute
        if (syntaxNode.AttributeLists.Count is 0) return (null, null);

        // Get Symbol
        var uncastedSymbol = genContext.SemanticModel.GetDeclaredSymbol(syntaxNode);
        if (uncastedSymbol is not TSymbol symbol) return (null, null);

        // Get Attributes
        var attribute1s = (
            from x in symbol.GetAttributes()
            where x.AttributeClass?.ToDisplayString() == FullAttribute1Name
            select TransformAttribute1(x, genContext.SemanticModel.Compilation)
        ).ToArray();
        var attribute2s = (
            from x in symbol.GetAttributes()
            where x.AttributeClass?.ToDisplayString() == FullAttribute2Name
            select TransformAttribute2(x, genContext.SemanticModel.Compilation)
        ).ToArray();

        if (attribute1s.Length is 0 && attribute2s.Length is 0) return (null, null);

        string? output;
#if DEBUG
        DateTime BeforeProcess = DateTime.Now;
#endif
        // All conditions satistfy except for actual running generator
        try
        {
            output = OnPointVisit(genContext, syntaxNode, symbol, attribute1s, attribute2s);
            if (output is null) return (null, null);
        }
        catch (Exception e)
        {
            // Log the exception
            output = $"""
            /*
                Exception Occured: {e.GetType().FullName}{e.Message}
                Messsage: {e.Message}
                Stack Trace:
                    {e.StackTrace.IndentWOF(2)}
            */
            """;
        }
#if DEBUG
        DateTime ProcessCompleted = DateTime.Now;
#endif
        // All conditions satisfy
        var containingClass = symbol.ContainingType;
        var genericParams = containingClass.TypeParameters;
        var classHeader =
            genericParams.Length is 0 ?
                containingClass.Name :
                $"{containingClass.Name}<{string.Join(", ", from x in genericParams select x.Name)}>";
#if DEBUG
        TimeSpan EntireProcess = ProcessCompleted - TransformBegin;
        TimeSpan SubProcess = ProcessCompleted - BeforeProcess;
#endif
        return ($"{symbol}.g.cs",
#if DEBUG
            $"""
            // This Generator took {EntireProcess.TotalMilliseconds}ms ({EntireProcess.Ticks} ticks) in total
            // SubProcess took {SubProcess.TotalMilliseconds}ms ({SubProcess.Ticks} ticks)
            """ +
#endif
            $$"""
            #nullable enable
            // Autogenerated for {{symbol}}
            
            namespace {{containingClass.ContainingNamespace}}
            {
                partial class {{classHeader}}
                {
                    {{output.IndentWOF(2)}}
                }
            }
            """);
    }
}