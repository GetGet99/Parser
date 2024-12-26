using Get.EasyCSharp.GeneratorTools.SyntaxCreator.Attributes;
using Get.EasyCSharp.GeneratorTools.SyntaxCreator.Lines;
using System.Collections.Generic;
using System.Linq;

using Get.EasyCSharp.GeneratorTools.SyntaxCreator.Expression;
namespace Get.EasyCSharp.GeneratorTools.SyntaxCreator.Members;

abstract class MethodBase
{
    protected MethodBase(SyntaxVisibility Visibility)
    {
        this.Visibility = Visibility;
    }
    public SyntaxVisibility Visibility { get; set; }
    public IDocumentation? Documentation { get; set; }

    public LinkedList<IAttribute> Attributes { get; } = new();
    public LinkedList<ILine> Code { get; } = new();

    public string StringRepresentaion => ToString();

    protected virtual IEnumerable<string> GetKeywords()
    {
        if (Visibility.GetString() is string s)
            yield return s;
    }
    protected abstract string MiddleText { get; }
    public override string ToString()
    {
        return $$"""
                {{Documentation?.StringRepresentaion ?? "// No Documentation was provided"}}
                {{GetKeywords().JoinWith(" ")}} {{MiddleText}} {
                    {{Code.Select(x => x.StringRepresentaion).JoinNewLine().IndentWOF(1)}}
                }
                """;
    }
}
abstract class MethodBase2 : MethodBase, IMember
{
    protected MethodBase2(SyntaxVisibility Visibility, FullType ReturnType, string Name, IEnumerable<ParameterDefinition>? Parameters = null)
    : base(Visibility) {
        this.ReturnType = ReturnType;
        this.Name = Name;
        if (Parameters is not null)
            foreach (var param in Parameters) this.Parameters.AddLast(param);
    }
    public string Name { get; set; }
    public FullType ReturnType { get; set; }
    public LinkedList<ParameterDefinition> Parameters { get; set; } = new();
    protected override string MiddleText => $"{ReturnType} {Name}({Parameters.Select(x => $"{x.Type} {x.Name}").JoinWith(", ")})";
}
class Property : IMember
{
    public Property(SyntaxVisibility Visibility, FullType PropertyType, string Name)
    {
        this.Name = Name;
        this.Visibility = Visibility;
        this.PropertyType = PropertyType;
    }
    public string Name { get; }
    public FullType PropertyType { get; }
    public SyntaxVisibility Visibility { get; }
    public IDocumentation? Documentation { get; set; }
    public bool Override { get; set; } = false;
    public bool Static { get; set; } = false;
    public Parts Get { get; } = new("get");
    public Parts Set { get; } = new("set");
    public IExpression? CustomEqualExpression { get; set; } = null;

    public string StringRepresentaion => ToString();

    protected virtual IEnumerable<string> GetKeywords()
    {
        if (Visibility.GetString() is string s)
            yield return s;
        if (Static)
            yield return "static";
        if (Override)
            yield return "override";
    }

    protected virtual IEnumerable<string> GetDeclaration()
    {
        foreach (var kw in GetKeywords())
            yield return kw;
        yield return PropertyType.StringRepresentaion;
        yield return Name;
    }
    public override string ToString()
    {
        return $$"""
                {{Documentation?.StringRepresentaion ?? "// No Documentation was provided"}}
                {{GetDeclaration().JoinWith(" ")}} {
                    {{
                        Array(Get, Set)
                        .Where(static x => x.IsGenerating)
                        .Select(static x => x.StringRepresentaion)
                        .JoinDoubleNewLine()
                        .IndentWOF(1)
                    }}
                }{{(CustomEqualExpression is not null ? $" = {CustomEqualExpression};" : "")}}
                """;
    }
    T[] Array<T>(params T[] values) => values;
    public class Parts : MethodBase
    {
        public bool Shortened { get; set; } = false;
        public bool IsGenerating => Visibility is not SyntaxVisibility.DoNotGenerate;
        string PartType;
        public Parts(string PartType)
            : base(SyntaxVisibility.Default)
        { this.PartType = PartType; }
        protected override string MiddleText => PartType;
        public override string ToString()
        {
            if (Shortened) return $"{PartType};";
            return base.ToString();
        }
    }
}