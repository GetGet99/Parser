using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Get.EasyCSharp.GeneratorTools;
[Generator]
partial class AtributeConverterGenerator : GeneratorBase<ClassAttributeSyntaxReceiver>
{
    readonly static AddAttributeConverterAttribute DefaultAddAttributeConverterAttribute = new(typeof(AddAttributeConverterAttribute));

    protected override ClassAttributeSyntaxReceiver ConstructSyntaxReceiver()
        => new(typeof(AddAttributeConverterAttribute).FullName);

    protected override void OnExecute(GeneratorExecutionContext context, ClassAttributeSyntaxReceiver SyntaxReceiver)
    {
        var AddAttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(AddAttributeConverterAttribute).FullName);
        var TypeSymbol = context.Compilation.GetTypeByMetadataName(typeof(Type).FullName);
        var AttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(Attribute).FullName);
        var INamedTypeSymbolSymbol = context.Compilation.GetTypeByMetadataName(typeof(INamedTypeSymbol).FullName);
        var ITypeSymbolSymbol = context.Compilation.GetTypeByMetadataName(typeof(ITypeSymbol).FullName);
        var IExtensionSymbol = context.Compilation.GetTypeByMetadataName(typeof(Extension).FullName);
        if (AddAttributeSymbol is null) return;
        if (TypeSymbol is null) return;
        if (AttributeSymbol is null) return;
        if (INamedTypeSymbolSymbol is null) return;
        if (ITypeSymbolSymbol is null) return;
        foreach (var @class in SyntaxReceiver.Classes)
        {
            context.AddSource($"{@class.ContainingNamespace}.{@class.Name}_GeneratedAttributeConverter.g.cs", $$"""
                #nullable enable
                #pragma warning disable CS8618
                #pragma warning disable CS8625
                #pragma warning disable CS8601
                using System.Runtime.CompilerServices;
                namespace {{@class.ContainingNamespace}}
                {
                    partial class {{@class.Name}}{{(
                            @class.TypeParameters.Length == 0 ? "" :
                            $"<{string.Join(", ", from x in @class.TypeParameters select x.Name)}>"
                        )}}
                    {
                        {{
                            ProcessAttributes(
                                @class.GetAttributes(),
                                AddAttributeSymbol,
                                TypeSymbol,
                                INamedTypeSymbolSymbol,
                                AttributeSymbol,
                                ITypeSymbolSymbol
                            ).JoinDoubleNewLine().IndentWOF(2)
                        }}
                    }
                }
                """);
        }
    }
    static IEnumerable<string> ProcessAttributes(ImmutableArray<AttributeData> AttributeData, ISymbol AddAttributeSymbol, ISymbol TypeSymbol, INamedTypeSymbol INamedTypeSymbolSymbol, ISymbol AttributeSymbol, ITypeSymbol ITypeSymbolSymbol)
    {
        foreach (var attribute in AttributeData)
        {
            if (!(attribute.AttributeClass?.Equals(AddAttributeSymbol, SymbolEqualityComparer.Default) ?? false))
                continue;
            /// Constructor Arguments
            var args = attribute.ConstructorArguments;
            if (args.Length != 1) continue;

            /// Constructor #1
            var arg = args[0];
            if (!(arg.Type?.Equals(TypeSymbol, SymbolEqualityComparer.Default) ?? false))
                continue;

            if (arg.Value is not INamedTypeSymbol Type) continue;

            /// <see cref="AddAttributeConverterAttribute.MethodName"/>
            var FuncName = attribute.NamedArguments.SingleOrDefault(
                x => x.Key == nameof(AddAttributeConverterAttribute.MethodName))
                .Value.Value.CastOrDefault(DefaultAddAttributeConverterAttribute.MethodName);

            /// <see cref="AddAttributeConverterAttribute.StructName"/>
            var StructName = attribute.NamedArguments.SingleOrDefault(
                x => x.Key == nameof(AddAttributeConverterAttribute.StructName))
                .Value.Value.CastOrDefault(DefaultAddAttributeConverterAttribute.ParametersAsString);

            /// <see cref="AddAttributeConverterAttribute.ParametersAsString"/>
            var Parameter = attribute.NamedArguments.SingleOrDefault(
                x => x.Key == nameof(AddAttributeConverterAttribute.ParametersAsString))
                .Value.Value.CastOrDefault(DefaultAddAttributeConverterAttribute.ParametersAsString) ?? "";


            /// <see cref="AddAttributeConverterAttribute.SampleObjectType"/>
            var _SampleObjectType = attribute.NamedArguments.SingleOrDefault(
                x => x.Key == nameof(AddAttributeConverterAttribute.SampleObjectType))
                .Value;
            
            if (_SampleObjectType.Value is not INamedTypeSymbol SampleObjectType)
                SampleObjectType = Type;
            //Debugger.Launch();

            var TypeConstructors = Type.Constructors;
            var TypeParameters = Type.TypeParameters;

            ITypeSymbol ProcessTypeSymbol(ITypeSymbol sym)
            {
                if (sym.Equals(TypeSymbol, SymbolEqualityComparer.Default))
                    return INamedTypeSymbolSymbol.WithNullableAnnotation(sym.NullableAnnotation);
                else
                    return sym;

            }
            var members = (from x in Type.GetMemeberRecursiveBaseType()
                           where !(x.Name is nameof(Attribute.TypeId) && x.ContainingType.Equals(AttributeSymbol, SymbolEqualityComparer.Default))
                           where !x.IsImplicitlyDeclared && x.DeclaredAccessibility == Accessibility.Public && x.Kind is SymbolKind.Field or SymbolKind.Property select x).ToArray();
            var TypeWrapper = StructName ?? $"{Type.Name}Warpper";
            yield return $$"""
            /// AutoGenerated For {{Type}}
            public partial class {{TypeWrapper}} {
                // Craeted For Reference with Default Value
                static {{Type}} _Default = new {{SampleObjectType}}({{Parameter}});

                public {{TypeWrapper}}({{typeof(Compilation).FullName}} compilation) {
                    {{
                        (
                            from member in members
                            let type =
                                    member switch
                                    {
                                        IPropertySymbol p => ProcessTypeSymbol(p.Type),
                                        IFieldSymbol f => ProcessTypeSymbol(f.Type),
                                        _ => throw new ArgumentException()
                                    }
                            select type.Equals(INamedTypeSymbolSymbol, SymbolEqualityComparer.Default)
                                ? $"this.{member.Name} = _Default.{member.Name} == null ? null : compilation.GetTypeByMetadataName(_Default.{member.Name}.FullName);"
                                : $"this.{member.Name} = _Default.{member.Name};"
                        ).JoinNewLine().IndentWOF(2)
                    }}
                }

                // {{Type.Name}}'s Type Parameters
                {{
                    (
                        from typeparameters in TypeParameters
                        select $$"""
                        /// <summary>
                        /// <inheritdoc cref="{{"" /* TODO */}}"/>
                        /// </summary>
                        public {{ITypeSymbolSymbol}} {{typeparameters.Name}} { get; set; }
                        """
                    ).JoinDoubleNewLine().IndentWOF(1)
                }}

                // {{Type.Name}}'s Constructor
                {{TypeConstructors.Length switch {
                    0 => "// No Constructor",
                    1 =>
                        TypeConstructors[0].Parameters.Length == 0 ? "// No Parameters" :
                        (
                            from param in TypeConstructors[0].Parameters
                            select
                                $$"""
                                /// <summary>
                                /// <see cref="{{TypeConstructors[0]}}"/>
                                /// </summary>
                                public {{ProcessTypeSymbol(param.Type)}} {{param.Name}} { get; set; } {{(param.HasExplicitDefaultValue ? $"= {(
                                                param.ExplicitDefaultValue is null ? $"default({ProcessTypeSymbol(param.Type)})" :
                                                $"({ProcessTypeSymbol(param.Type)}){param.ExplicitDefaultValue.ToSyntaxString()}"
                                            )};" : "")}}
                                """
                        ).JoinDoubleNewLine().IndentWOF(1),
                    >1 => "// Warning: There are more than 1 constructors, and it is not yet supported",
                    _ => "// Warning: There is less than 0 constructors. Wait what?"
                }}}

                // {{Type.Name}}'s Members
                {{
                    (
                        from member in members
                        select $$"""
                        /// <summary>
                        /// <inheritdoc cref="{{member}}"/>
                        /// </summary>
                        public {{
                                    // Type
                                    member switch
                                    {
                                        IPropertySymbol p => ProcessTypeSymbol(p.Type),
                                        IFieldSymbol f => ProcessTypeSymbol(f.Type),
                                        _ => throw new ArgumentException()
                                    }
                                }} {{member.Name}} { get; set; }
                        """
                    ).JoinDoubleNewLine().IndentWOF(1)
                }}
            }

            private static {{TypeWrapper}} {{FuncName ?? $"AttributeDataTo{Type.Name}"}}({{typeof(AttributeData).FullName}} attributeData, {{typeof(Compilation)}} compilation) {
                var result = new {{TypeWrapper}}(compilation);
                
                // Type Parameters
                {{(
                    TypeParameters.Length <= 0 ? "// No Type Parameters" :
                    (
                        from i in Enumerable.Range(0, TypeParameters.Length)
                        select
                            $"""
                            // {TypeParameters[i].Name}
                            result.{TypeParameters[i].Name} = attributeData.AttributeClass.TypeArguments[{i}];
                            """
                    ).JoinDoubleNewLine().IndentWOF(1)
                )}}

                // Parameters
                {{(
                    TypeConstructors.Length != 1 ? "// Amount of Constructor != 0, not generated" :
                    (
                        TypeConstructors[0].Parameters.Length <= 0 ? "// No Parameters" :
                        (
                           from i in Enumerable.Range(0, TypeConstructors[0].Parameters.Length)
                           let param = TypeConstructors[0].Parameters[i]
                           let type = ProcessTypeSymbol(param.Type)
                           select
                               $"""
                                // {param.Name}
                                result.{param.Name} = {(type.TypeKind == TypeKind.Enum ? $"({type})" : "")}{typeof(Extension).FullName}.CastOrDefault(attributeData.ConstructorArguments[{i}].Value, {
                                    (type.TypeKind == TypeKind.Enum ? $"({((INamedTypeSymbol)type).EnumUnderlyingType})default({type})" : $"default({type})")
                                });
                                """
                        ).JoinDoubleNewLine().IndentWOF(1)
                    )
                )}}

                // Members
                {{(
                    members.Length <= 0 ? "// No Members" :
                   $$"""
                    foreach (var v in attributeData.NamedArguments)
                    {
                        switch (v.Key)
                        {
                            {{
                                (
                                    from member in members
                                    let type = // Type
                                        member switch
                                        {
                                            IPropertySymbol p => ProcessTypeSymbol(p.Type),
                                            IFieldSymbol f => ProcessTypeSymbol(f.Type),
                                            _ => throw new ArgumentException()
                                        }
                                    select
                                        $"""
                                        // {member.Name}
                                        case "{member.Name}":
                                            result.{member.Name} = {(type.TypeKind == TypeKind.Enum ? $"({type})" : "")}{typeof(Extension).FullName}.CastOrDefault(v.Value.Value, {
                                                (type.TypeKind == TypeKind.Enum ? $"({((INamedTypeSymbol)type).EnumUnderlyingType})default({type})" : $"default({type})")
                                            });
                                            break;
                                        """
                                ).JoinDoubleNewLine().IndentWOF(2)
                            }}
                        }
                    }
                    """.IndentWOF(1)
                )}}

                // Return Result
                return result;
            }
            /// Ended AutoGenerated For {{Type}}
            """;
        }
    }

    static string ChooseName(string fieldName, TypedConstant overridenNameOpt)
    {
        if (!overridenNameOpt.IsNull)
        {
            return overridenNameOpt.Value?.ToString() ?? "";
        }

        fieldName = fieldName.TrimStart('_');
        if (fieldName.Length == 0)
            return string.Empty;

        if (fieldName.Length == 1)
            return fieldName.ToUpper();

        return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
    }
    static string? GetVisiblityPrefix(string DefaultPrefix, PropertyVisibility propertyVisibility)
        => propertyVisibility switch
        {
            PropertyVisibility.Default => DefaultPrefix,
            PropertyVisibility.DoNotGenerate => null,
            PropertyVisibility.Public => $"public",
            PropertyVisibility.Private => $"private",
            PropertyVisibility.Protected => $"protected",
            _ => throw new ArgumentOutOfRangeException()
        };
    static string? GetVisibilityPrefix(string DefaultPrefix, TypedConstant Value, PropertyVisibility defaultVisibility)
    {
        try
        {
            PropertyVisibility propertyVisibility = (PropertyVisibility)(byte)Value.Value!;
            return GetVisiblityPrefix(DefaultPrefix, propertyVisibility);
        }
        catch
        {
            return GetVisiblityPrefix(DefaultPrefix, defaultVisibility);
        }
    }
}