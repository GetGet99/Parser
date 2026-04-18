using Microsoft.CodeAnalysis;

namespace Get.Lexer.SourceGenerator;

static class LexerRegexStateHelper
{
    public readonly static DiagnosticDescriptor InvalidRegexState = new(
        "GR1003",
        "Invalid Regex State",
        "Regex State must be an integer or enum value",
        "Get.Lexer",
        DiagnosticSeverity.Error,
        true
    );

    public static bool TryGetRegexStates(AttributeData attributeData, Action<Diagnostic> reportDiagnostic, out int[] states)
    {
        states = [0];
        var hasState = false;
        TypedConstant stateConstant = default;
        foreach (var v in attributeData.NamedArguments)
        {
            if (v.Key != nameof(RegexAttribute.State))
                continue;
            hasState = true;
            stateConstant = v.Value;
            break;
        }
        if (!hasState)
            return true;

        var location = GetAttributeLocation(attributeData);
        if (stateConstant.Kind == TypedConstantKind.Enum && stateConstant.Type is INamedTypeSymbol enumType)
        {
            try
            {
                var state = Convert.ToInt64(stateConstant.Value);
                states = enumType.GetAttributes().Any(x => x.AttributeClass?.Name is nameof(FlagsAttribute))
                    ? ExpandFlags(enumType, state)
                    : [checked((int)state)];
                return true;
            }
            catch
            {
                reportDiagnostic(Diagnostic.Create(InvalidRegexState, location));
                states = [];
                return false;
            }
        }
        if (TryGetIntegerState(stateConstant, out var stateValue))
        {
            states = [stateValue];
            return true;
        }

        reportDiagnostic(Diagnostic.Create(InvalidRegexState, location));
        states = [];
        return false;

        static Location? GetAttributeLocation(AttributeData attributeData)
        {
            var syntax = attributeData.ApplicationSyntaxReference;
            return syntax is null ? null : Location.Create(syntax.SyntaxTree, syntax.Span);
        }
        static int[] ExpandFlags(INamedTypeSymbol enumType, long state)
        {
            if (state == 0)
                return [0];

            var states = new HashSet<int>();
            var remaining = state;
            foreach (var field in enumType.GetMembers().OfType<IFieldSymbol>())
            {
                if (!field.HasConstantValue)
                    continue;
                var value = Convert.ToInt64(field.ConstantValue);
                if (!IsSingleBit(value))
                    continue;
                if ((state & value) != value)
                    continue;
                states.Add(checked((int)value));
                remaining &= ~value;
            }
            if (remaining != 0 || states.Count == 0)
                states.Add(checked((int)state));
            return states.ToArray();
        }
        static bool IsSingleBit(long value) => value > 0 && (value & (value - 1)) == 0;
        static bool TryGetIntegerState(TypedConstant stateConstant, out int state)
        {
            state = 0;
            switch (stateConstant.Type?.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    try
                    {
                        state = Convert.ToInt32(stateConstant.Value);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }
    }
}
