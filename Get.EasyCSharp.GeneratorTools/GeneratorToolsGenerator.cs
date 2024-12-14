//using CopySourceGenerator;
//using Microsoft.CodeAnalysis;

//namespace Get.EasyCSharp.GeneratorTools;
//[CopySource("sExtension", typeof(Extension))]
//[CopySource("sGeneratorBase", typeof(GeneratorBase))]
//[CopySource("sSyntaxReceiver", typeof(ClassAttributeSyntaxReceiver))]
//[CopySource("sAttributeBaseGenerator", typeof(AttributeBaseGenerator<,,,>))]
//[Generator]
//partial class GeneratorToolsGenerator : GeneratorBase
//{
//    protected override void OnInitialize(GeneratorInitializationContext context)
//    {
//        context.RegisterForPostInitialization(x => {
//            x.AddSource($"{typeof(Extension).FullName}.g.cs", sExtension);
//            x.AddSource($"{typeof(GeneratorBase).FullName}.g.cs", sGeneratorBase);
//            x.AddSource($"EasyCSharp.GeneratorTools.SyntaxReceiver.g.cs", sSyntaxReceiver);
//            x.AddSource($"EasyCSharp.GeneratorTools.AttributeBaseGenerator.g.cs", sAttributeBaseGenerator);
//        });
//    }
//}
