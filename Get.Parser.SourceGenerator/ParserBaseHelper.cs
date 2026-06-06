using Get.EasyCSharp.GeneratorTools;
using Get.Lexer;
using Microsoft.CodeAnalysis;

namespace Get.Parser.SourceGenerator;

static class ParserBaseHelper
{
    public static readonly RuleAttrSyntaxParser RuleAttrSyntaxParser = new();
    public static readonly PrecedenceAttrSyntaxParser PrecedenceAttrSyntaxParser = new();
    public static bool TryGetParserBaseTypes(ITypeSymbol symbol,
        out ITypeSymbol? terminalType,
        out ITypeSymbol? nonTerminalType)
    {
        terminalType = null;
        nonTerminalType = null;
        if (symbol.BaseType?.ToString().StartsWith("Get.Parser.ParserBase") != true)
            return false;
        if (symbol.BaseType.TypeArguments.Length != 3)
            return false;
        terminalType = symbol.BaseType.TypeArguments[0];
        nonTerminalType = symbol.BaseType.TypeArguments[1];
        return true;
    }

    public static (ITypeSymbol TerminalType, ITypeSymbol NonTerminalType, ITypeSymbol? AssociativityType,
        ITypeSymbol KeywordType, FullType TerminalFT, FullType NonTerminalFT)
        SetupParserVariables(SemanticModel semanticModel, INamedTypeSymbol baseType)
    {
        var terminalType = baseType.TypeArguments[0];
        var nonTerminalType = baseType.TypeArguments[1];
        var associativityType = semanticModel.Compilation.GetTypeByMetadataName(typeof(Associativity).FullName);
        var keywordType = semanticModel.Compilation.GetTypeByMetadataName(typeof(ParserSourceGeneratorKeywords).FullName)!;
        return (terminalType, nonTerminalType, associativityType, keywordType,
            new FullType(terminalType), new FullType(nonTerminalType));
    }

    public static void CollectTerminalTypes(
        ITypeSymbol terminalType,
        Dictionary<object, ITypeSymbol?> output,
        Func<ISymbol, ITypeSymbol?> typeResolver)
    {
        foreach (var t in terminalType.GetMembers())
        {
            if (t is IFieldSymbol fieldSymbol)
            {
                var value = fieldSymbol.ConstantValue;
                if (value is null) continue;
                output[value] = typeResolver(t);
            }
        }
    }

    public static void CollectNonTerminalTypes(
        ITypeSymbol nonTerminalType,
        Dictionary<object, ITypeSymbol?> output,
        Func<ISymbol, ITypeSymbol?> typeResolver)
    {
        foreach (var nt in nonTerminalType.GetMembers())
        {
            if (nt is IFieldSymbol fieldSymbol)
            {
                var value = fieldSymbol.ConstantValue;
                if (value is null) continue;
                output[value] = typeResolver(nt);
            }
        }
    }

    public static string ConstantParameterToString(Option option)
    {
        var (type, value) = option.ConstantParameterValue;
        if (value is null)
            return "null";

        if (type is { TypeKind: TypeKind.Enum })
        {
            return $"({new FullType(type)}){value}";
        }
        if (value is string str)
        {
            return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(str, true);
        }
        return option.ConstantParameterValue.ConstantParameterValue switch
        {
            bool b => b ? "true" : "false",
            null => "null",
            var rest => rest.ToString()
        };
    }

    public static List<PrecedenceItem> ParsePrecedenceCore(
        SemanticModel semanticModel,
        ITypeSymbol terminalType,
        ITypeSymbol? associativityType,
        AttributeData raw)
    {
        var precedenceArgs = raw.ConstructorArguments[0].Values;
        if (associativityType is null)
            return [];
        return PrecedenceAttrSyntaxParser.Parse(precedenceArgs, terminalType, associativityType);
    }
}
