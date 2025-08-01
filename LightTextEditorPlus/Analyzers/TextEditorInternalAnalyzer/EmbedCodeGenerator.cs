using System.Reflection;

using Microsoft.CodeAnalysis;

namespace TextEditorInternalAnalyzer;

[Generator(LanguageNames.CSharp)]
public class EmbedCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Assembly assembly = GetType().Assembly;
        using Stream embedSource = assembly.GetManifestResourceStream("TextEditorInternalAnalyzer.Attributes.APIConstraintAttribute.cs")!;
        var text = new StreamReader(embedSource).ReadToEnd();

        context.RegisterPostInitializationOutput(initializationContext =>
        {
            initializationContext.AddSource("APIConstraintAttribute.cs", text);
        });
    }
}