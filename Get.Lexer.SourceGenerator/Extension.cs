#pragma warning disable IDE0240
#nullable enable
#pragma warning restore IDE0240

using Get.EasyCSharp.GeneratorTools.SyntaxCreator.Members;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Get.EasyCSharp.GeneratorTools;

static class Extension
{
    public static readonly string InSourceNewLine =
        """
            a
            b
            """[1..^1];
    public static string Indent(this string Original, int IndentTimes = 1, int IndentSpace = 4)
    {
        var Indent = new string(' ', IndentSpace * IndentTimes);
        var slashNindent = $"{InSourceNewLine}{Indent}";
        return Indent + Original.Replace(InSourceNewLine, slashNindent);
    }
    public static string GetEscapedName(this ISymbol symbol)
        => symbol.Name.StartsWith("@") ? symbol.Name : $"@{symbol.Name}";
    public static string Join(this IEnumerable<string> Original, string joinchar)
        => string.Join(joinchar, Original);
    public static string JoinNewLine(this IEnumerable<string> Original)
        => string.Join(InSourceNewLine, Original);
    public static string JoinDoubleNewLine(this IEnumerable<string> Original)
        => string.Join($"{InSourceNewLine}{InSourceNewLine}", Original);
    public static string ToSyntaxString(this object obj)
    {
        if (obj is bool booleanValue)
        {
            return booleanValue ? "true" : "false";
        }
        else if (obj is string stringValue)
        {
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in stringValue)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\0': sb.Append("\\0"); break;
                    case '\a': sb.Append("\\a"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\v': sb.Append("\\v"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
        else if (obj.GetType().IsEnum)
        {
            return $"{obj.GetType().FullName}.{Enum.GetName(obj.GetType(), obj)}";
        }

        return obj.ToString();
    }
    public static string IndentWOF(this string Original, int IndentTimes = 1, int IndentSpace = 4)
    {
        var Indent = new string(' ', IndentSpace * IndentTimes);
        var slashNindent = $"{InSourceNewLine}{Indent}";
        return Original.Replace(InSourceNewLine, slashNindent);
    }
    public static IEnumerable<(uint Index, T Item)> Enumerate<T>(this IEnumerable<T> TEnu)
    {
        uint i = 0;
        foreach (var a in TEnu)
            yield return (i++, a);
    }
    public static T CastOrDefault<T>(this object? Value, T DefaultValue)
    {
        if (Value is T CastedValue) return CastedValue;
        else return DefaultValue;
    }
    static SymbolDisplayFormat full = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );
    public static string FullName(this ITypeSymbol Symbol, bool NullableReferenceType = false)
    {
        //if (Symbol.ToString().Contains('.'))
        //    return $"global::{Symbol.ToDisplayString(full)}";
        //else
        NullableReferenceType = NullableReferenceType || Symbol.NullableAnnotation == NullableAnnotation.Annotated;
        if (NullableReferenceType && !Symbol.IsValueType)
            return Symbol.FullNameWithoutAnnotation() + "?";
        else
            return Symbol.FullNameWithoutAnnotation();
        //return Symbol.WithNullableAnnotation(NullableReferenceType ? NullableAnnotation.Annotated : NullableAnnotation.None).ToString();
    }
    public static string FullNameWithoutAnnotation(this ITypeSymbol Symbol)
    {
        return Symbol.ToDisplayString(full);
    }
    public static T GetProperty<T>(this AttributeData attribute, string AttributeName, T defaultValue)
    {
        bool first = true;
        T result = defaultValue;
        foreach (var v in attribute.NamedArguments)
        {
            if (v.Key == AttributeName)
            {
                if (!first)
                {
                    throw new ArgumentException("Duplicate Element!");
                }

                first = false;
                result = (T)v.Value!.Value!;
            }
        }
        return result;
    }
    public static IEnumerable<ISymbol> GetMemeberRecursiveBaseType(this ITypeSymbol? Type)
    {
        if (Type is null) yield break;

        foreach (var member in Type.GetMembers())
            yield return member;
        foreach (var member in Type.BaseType.GetMemeberRecursiveBaseType())
            yield return member;
    }
    public static bool IsImplementing(this ITypeSymbol? Type, ITypeSymbol? PotentialInterface)
    {
        if (Type.IsTheSameAs(PotentialInterface)) return true;
        return Type?.AllInterfaces.Any(x => x.IsTheSameAs(PotentialInterface)) ?? false;
    }
    public static bool IsImplementingNoGeneric(this INamedTypeSymbol? Type, INamedTypeSymbol? PotentialInterface)
    {
        if (Type.IsTheSameAsNoGeneric(PotentialInterface)) return true;
        return Type?.AllInterfaces.Any(x => x.IsTheSameAsNoGeneric(PotentialInterface)) ?? false;
    }
    public static bool IsSubclassFrom(this ITypeSymbol? Type, ITypeSymbol? PotentialBaseType)
    {
        if (Type is null) return false;
        if (Type.Equals(PotentialBaseType, SymbolEqualityComparer.Default)) return true;
        return Type.BaseType.IsSubclassFrom(PotentialBaseType);
    }
    public static bool IsTheSameAs(this ITypeSymbol? Type, ITypeSymbol? PotentialBaseType)
    {
        return Type?.Equals(PotentialBaseType, SymbolEqualityComparer.Default) ?? false;
    }
    public static bool IsTheSameAsNoGeneric(this INamedTypeSymbol? Type, INamedTypeSymbol? PotentialBaseType)
    {
        return Type.RemoveGeneric()?.Equals(PotentialBaseType.RemoveGeneric(), SymbolEqualityComparer.Default) ?? false;
    }
    public static INamedTypeSymbol? RemoveGeneric(this INamedTypeSymbol? Type)
    {
        if (Type is null) return null;
        if (Type.TypeArguments.Length > 0) return Type.ConstructUnboundGenericType();
        return Type;
    }
    //public static T GetConstructor<T>(this AttributeData attribute, int index, T? defaultValue = default)
    //{
    //var attr = attribute.
    //}
    public static IncrementalValueProvider<TOut> Select<TIn, TOut>(this IncrementalValueProvider<TIn> valueProvider, Func<TIn, TOut> func)
    {
        return valueProvider.Select((x, _) => func(x));
    }
    public static IncrementalValuesProvider<TOut> SelectMany<TIn, TOut>(this IncrementalValueProvider<TIn> valueProvider, Func<TIn, IEnumerable<TOut>> func)
    {
        return valueProvider.SelectMany((x, _) => func(x));
    }
    //public static IncrementalValueProvider<TIn> Where<TIn>(this IncrementalValueProvider<TIn> valueProvider, Func<TIn, bool> func)
    //{
    //    return valueProvider.Where(func);
    //}
    public static IEnumerable<T> SkipAtIndex<T>(this T[] Items, int index)
    {
        for (var i = 0; i < Items.Length; i++)
        {
            if (i == index) continue;
            yield return Items[i];
        }
    }
    public static IEnumerable<T[]> AllCombinations<T>(this T[] Items)
    {
        yield return Items;
        for (var i = 0; i < Items.Length; i++)
            foreach (var enus in Items.SkipAtIndex(i).ToArray().AllCombinations())
            {
                yield return enus;
            }
    }
    public static T FirstOrDefault<T>(this IEnumerable<T> Items, T defaultValue)
    {
        foreach (var item in Items)
            return item;
        return defaultValue;
    }
}
