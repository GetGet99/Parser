#pragma warning disable IDE0240
#nullable enable
#pragma warning restore IDE0240
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Get.EasyCSharp.GeneratorTools;

class FieldAttributeSyntaxReceiver : ISyntaxContextReceiver
{
    readonly string Type;
    public FieldAttributeSyntaxReceiver(string Type)
    {
        this.Type = Type;
    }
    public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
            && fieldDeclarationSyntax.AttributeLists.Count > 0)
        {
            foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
            {
                // Get the symbol being declared by the field, and keep it if its annotated;
                if (
                        context.SemanticModel.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol &&
                        fieldSymbol.GetAttributes()
                        .Any(ad => ad.AttributeClass?.ToDisplayString() == Type)
                    )
                    Fields.Add(fieldSymbol);
            }
        }
    }
}
class PropertyAttributeSyntaxReceiver : ISyntaxContextReceiver
{
    readonly string Type;
    public PropertyAttributeSyntaxReceiver(string Type)
    {
        this.Type = Type;
    }
    public List<IPropertySymbol> Properties { get; } = new List<IPropertySymbol>();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax
            && propertyDeclarationSyntax.AttributeLists.Count > 0)
        {
            if (
                    context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax) is IPropertySymbol propSymbol &&
                    propSymbol.GetAttributes()
                    .Any(ad => ad.AttributeClass?.ToDisplayString() == Type)
                )
                Properties.Add(propSymbol);
        }
    }
}
class MethodAttributeSyntaxReceiver : ISyntaxContextReceiver
{
    readonly string Type;
    public MethodAttributeSyntaxReceiver(string Type)
    {
        this.Type = Type;
    }
    public List<IMethodSymbol> Methods { get; } = new List<IMethodSymbol>();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax
            && methodDeclarationSyntax.AttributeLists.Count > 0)
        {
            if (
                    context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax) is IMethodSymbol propSymbol &&
                    propSymbol.GetAttributes()
                    .Any(ad => ad.AttributeClass?.ToDisplayString() == Type)
                )
                Methods.Add(propSymbol);
        }
    }
}
public class ClassAttributeSyntaxReceiver : ISyntaxContextReceiver
{
    readonly string Type;
    public ClassAttributeSyntaxReceiver(string Type)
    {
        this.Type = Type;
    }
    public List<INamedTypeSymbol> Classes { get; } = new List<INamedTypeSymbol>();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax classDeclarationSyntax
            && classDeclarationSyntax.AttributeLists.Count > 0)
        {
            if (
                    context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is INamedTypeSymbol propSymbol &&
                    propSymbol.GetAttributes()
                    .Any(ad => ad.AttributeClass?.ToDisplayString() == Type)
                )
                Classes.Add(propSymbol);
        }
    }
}