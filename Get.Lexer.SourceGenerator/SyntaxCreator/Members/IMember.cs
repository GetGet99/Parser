using Get.EasyCSharp.GeneratorTools.SyntaxCreator.Lines;
namespace Get.EasyCSharp.GeneratorTools.SyntaxCreator.Members;

interface IMember : ISyntax
{
    string Name { get; }
    SyntaxVisibility Visibility { get; }
    IDocumentation? Documentation { get; }
}
