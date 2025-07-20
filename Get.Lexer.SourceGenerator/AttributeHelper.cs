#nullable enable
#pragma warning disable IDE0240
#nullable enable
#pragma warning restore IDE0240
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Get.EasyCSharp.GeneratorTools;

delegate TOutput? AttributeTransformer<TOutput>(AttributeData attributeData, Compilation compilation);
static class AttributeHelper
{
    public static IEnumerable<(AttributeData RealAttributeData, TOutput Serialized)> GetAttributes<TAttribute, TOutput>(SemanticModel semanticModel, ISymbol symbol, AttributeTransformer<TOutput> attributeTransformer, bool allowSubclass = true)
    {
        // Get Attributes
        var Class = semanticModel.Compilation.GetTypeByMetadataName(typeof(TAttribute).FullName);

        return (
            from x in symbol.GetAttributes()
            where allowSubclass ?
                x.AttributeClass?.IsSubclassFrom(Class) ?? false :
                x.AttributeClass?.IsTheSameAs(Class) ?? false
            select (RealAttr: x, WrapperAttr: attributeTransformer(x, semanticModel.Compilation))
        ).Where(x => x.RealAttr is not null && x.WrapperAttr is not null);
    }
    public static (AttributeData RealAttributeData, TOutput Serialized)? TryGetAttribute<TAttribute, TOutput>(SemanticModel semanticModel, ISymbol symbol, AttributeTransformer<TOutput> attributeTransformer, bool allowSubclass = true)
    {
        foreach (var item in GetAttributes<TAttribute, TOutput>(semanticModel, symbol, attributeTransformer, allowSubclass))
        {
            return item;
        }
        return null;
    }
    public static IEnumerable<(AttributeData RealAttributeData, TOutput Serialized)> GetAttributesAnyGeneric<TAttribute, TOutput>(SemanticModel semanticModel, ISymbol symbol, AttributeTransformer<TOutput> attributeTransformer, bool allowSubclass = true)
    {
        // Get Attributes
        var attr = typeof(TAttribute);
        string name = attr.ContainsGenericParameters || attr.GenericTypeArguments.Length > 0 ? attr.Name[..attr.Name.IndexOf('`')] : attr.Name;

        return (
            from x in symbol.GetAttributes()
            where allowSubclass ?
                IsSubclassFromAnyGeneric(x.AttributeClass, attr.Namespace, name) :
                IsTheSameAsAnyGeneric(x.AttributeClass, attr.Namespace, name)
            select (RealAttr: x, WrapperAttr: attributeTransformer(x, semanticModel.Compilation))
        ).Where(x => x.RealAttr is not null && x.WrapperAttr is not null);

        static bool IsSubclassFromAnyGeneric(INamedTypeSymbol? Type, string ns, string PotentialBaseType)
        {
            if (Type is null) return false;
            if (Type.ContainingNamespace.ToString() == ns &&
               Type.Name == PotentialBaseType
            )
                return true;
            return IsSubclassFromAnyGeneric(Type.BaseType, ns, PotentialBaseType);
        }
        static bool IsTheSameAsAnyGeneric(INamedTypeSymbol? Type, string ns, string PotentialBaseType)
        {
            if (Type is null) return false;
            if (Type.ContainingNamespace.ToString() == ns &&
                (Type.IsGenericType ? Type.Name[..Type.Name.IndexOf("`")] : Type.Name) == PotentialBaseType
            )
                return true;
            return false;
        }
    }
    public static (AttributeData RealAttributeData, TOutput Serialized)? TryGetAttributeAnyGeneric<TAttribute, TOutput>(SemanticModel semanticModel, ISymbol symbol, AttributeTransformer<TOutput> attributeTransformer, bool allowSubclass = true)
    {
        foreach (var item in GetAttributesAnyGeneric<TAttribute, TOutput>(semanticModel, symbol, attributeTransformer, allowSubclass))
        {
            return item;
        }
        return null;
    }
    public static string ToReadableString(this Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.Name;
            int typeIndex = name.IndexOf('`');
            string baseType = name[..typeIndex];
            Type[] typeArguments = type.GetGenericArguments();

            string arguments = string.Join(", ", typeArguments.Select(ToReadableString));
            return $"{baseType}<{arguments}>";
        }
        else
        {
            return SimplifyName(type.Name);
        }
    }
    private static string SimplifyName(string typeName)
    {
        return typeName switch
        {
            "Boolean" => "bool",
            "Byte" => "byte",
            "SByte" => "sbyte",
            "Char" => "char",
            "Decimal" => "decimal",
            "Double" => "double",
            "Single" => "float",
            "Int32" => "int",
            "UInt32" => "uint",
            "Int64" => "long",
            "UInt64" => "ulong",
            "Int16" => "short",
            "UInt16" => "ushort",
            "String" => "string",
            _ => typeName,
        };
    }
}