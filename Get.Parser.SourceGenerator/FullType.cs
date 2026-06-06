using Microsoft.CodeAnalysis;

namespace Get.EasyCSharp.GeneratorTools;

record struct FullType(string TypeWithNamespace, bool Nullable = false)
{
    public FullType(ITypeSymbol typeSymbol) : this(typeSymbol.FullName()) { }

    public readonly override string ToString() => TypeWithNamespace;

    public static FullType Of<T>(bool Nullable = false)
    {
        return new FullType(Type2String(typeof(T)), Nullable);
    }

    static string Type2String(Type type)
    {
        if (type.IsGenericType)
        {
            var name = $"global::{type.FullName}";
            int typeIndex = name.IndexOf('`');
            string baseType = name[..typeIndex];
            var typeArguments = type.GetGenericArguments();
            string arguments = string.Join(", ", typeArguments.Select(Type2String));
            return $"{baseType}<{arguments}>";
        }
        return $"global::{type.FullName}";
    }
}
