#pragma warning disable IDE0240
#nullable enable
#pragma warning restore IDE0240
using Microsoft.CodeAnalysis;

namespace Get.EasyCSharp.GeneratorTools;

abstract class GeneratorBase<T> : ISourceGenerator where T : ISyntaxContextReceiver
{
    protected abstract T ConstructSyntaxReceiver();
    public void Initialize(GeneratorInitializationContext context)
    {
        OnInitialize(context);
        context.RegisterForSyntaxNotifications(() => ConstructSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is T receiver)
            OnExecute(context, receiver);
    }
    protected virtual void OnInitialize(GeneratorInitializationContext context) { }
    protected virtual void OnExecute(GeneratorExecutionContext context, T SyntaxReceiver) { }
}
public abstract class GeneratorBase : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        OnInitialize(context);
    }

    public void Execute(GeneratorExecutionContext context)
    {
        OnExecute(context);
    }
    protected virtual void OnInitialize(GeneratorInitializationContext context) { }
    protected virtual void OnExecute(GeneratorExecutionContext context) { }
}