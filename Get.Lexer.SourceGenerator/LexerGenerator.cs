using Get.EasyCSharp.GeneratorTools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Get.Lexer.SourceGenerator;
[Generator]
[AddAttributeConverter(typeof(LexerAttribute<TempType>))]
[AddAttributeConverter(typeof(RegexAttribute))]
[AddAttributeConverter(typeof(StringAttribute))]
[AddAttributeConverter(typeof(TypeAttribute<TempType>))]
[AddAttributeConverter(typeof(RegexAttribute<int>), StructName = "RegexAttributeGenericWrapper")]
partial class LexerGenerator : AttributeBaseGenerator<LexerAttributeBase, LexerGenerator.LexerAttributeWarpper, TypeDeclarationSyntax, INamedTypeSymbol>
{
    class NonLocalizableString(string s) : LocalizableString
    {
        public string DiagonosticString { get; } = s;
        protected override bool AreEqual(object? other) => other is NonLocalizableString l && l.DiagonosticString == DiagonosticString;
        protected override int GetHash() => DiagonosticString.GetHashCode();
        protected override string GetText(IFormatProvider? formatProvider) => DiagonosticString;
    }
    static DiagnosticDescriptor NotLexerBase = new(
        "GETLEXER1001",
        new NonLocalizableString("Type does not implement Get.Lexer.LexerBase"),
        new NonLocalizableString("Type must not implement Get.Lexer.LexerBase"),
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );
    protected override string? OnPointVisit(GeneratorSyntaxContext genContext, TypeDeclarationSyntax syntaxNode, INamedTypeSymbol symbol, (AttributeData Original, LexerAttributeWarpper Wrapper)[] attributeDatas, List<Diagnostic> diagnostics)
    {
        if (!(symbol.BaseType?.ToString().StartsWith("Get.Lexer.LexerBase") ?? false))
        {
            diagnostics.Add(Diagnostic.Create(NotLexerBase, syntaxNode.BaseList?.GetLocation() ?? syntaxNode.Identifier.GetLocation()));
            return null;
        }
        var (attr, attrdata) = attributeDatas[0];
        var lexerTokensType = attrdata.TLexerTokens;
        var members = lexerTokensType.GetMembers();
        foreach (var item in members)
        {
            AttributeHelper.TryGetAttribute<TypeAttribute<TempType>, RegexAttributeGenericWrapper>(genContext, item, AttributeDataToRegexAttribute);

        }
    }

    protected override LexerAttributeWarpper? TransformAttribute(AttributeData attributeData, Compilation compilation)
    {
        return AttributeDataToLexerAttribute(attributeData, compilation);
    }
}
enum TempType : byte { }