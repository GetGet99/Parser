#nullable enable
#pragma warning disable IDE0240
#nullable enable
#pragma warning restore IDE0240
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Get.EasyCSharp.GeneratorTools;

delegate TOutput? AttributeTransformer<TOutput>(AttributeData attributeData, Compilation compilation);
class AttributeHelper
{
    public static IEnumerable<(AttributeData RealAttributeData, TOutput Serialized)> GetAttributes<TAttribute, TOutput>(GeneratorSyntaxContext genContext, ISymbol symbol, AttributeTransformer<TOutput> attributeTransformer, bool allowSubclass = true)
    {
        // Get Attributes
        var Class = genContext.SemanticModel.Compilation.GetTypeByMetadataName(typeof(TAttribute).FullName);

        return (
            from x in symbol.GetAttributes()
            where allowSubclass ?
                x.AttributeClass?.IsSubclassFrom(Class) ?? false :
                x.AttributeClass?.IsTheSameAs(Class) ?? false
            select (RealAttr: x, WrapperAttr: attributeTransformer(x, genContext.SemanticModel.Compilation))
        ).Where(x => x.RealAttr is not null && x.WrapperAttr is not null);
    }
    public static (AttributeData RealAttributeData, TOutput Serialized)? TryGetAttribute<TAttribute, TOutput>(GeneratorSyntaxContext genContext, ISymbol symbol, AttributeTransformer<TOutput> attributeTransformer, bool allowSubclass = true)
    {
        foreach (var item in GetAttributes<TAttribute, TOutput>(genContext, symbol, attributeTransformer, allowSubclass))
        {
            return item;
        }
        return null;
    }
    public static IEnumerable<(AttributeData RealAttributeData, TOutput Serialized)> GetAttributesAnyGeneric<TAttribute, TOutput>(GeneratorSyntaxContext genContext, ISymbol symbol, AttributeTransformer<TOutput> attributeTransformer, bool allowSubclass = true)
    {
        // Get Attributes
        var attr = typeof(TAttribute);
        attr.
        var Class = genContext.SemanticModel.Compilation.GetTypeByMetadataName(typeof(TAttribute).FullName);

        return (
            from x in symbol.GetAttributes()
            where allowSubclass ?
                x.AttributeClass?.IsSubclassFrom(Class) ?? false :
                x.AttributeClass?.IsTheSameAs(Class) ?? false
            select (RealAttr: x, WrapperAttr: attributeTransformer(x, genContext.SemanticModel.Compilation))
        ).Where(x => x.RealAttr is not null && x.WrapperAttr is not null);
    }
    public static (AttributeData RealAttributeData, TOutput Serialized)? TryGetAttributeAnyGeneric<TAttribute, TOutput>(GeneratorSyntaxContext genContext, ISymbol symbol, AttributeTransformer<TOutput> attributeTransformer, bool allowSubclass = true)
    {
        foreach (var item in GetAttributesAnyGeneric<TAttribute, TOutput>(genContext, symbol, attributeTransformer, allowSubclass))
        {
            return item;
        }
        return null;
    }
}