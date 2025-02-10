using Microsoft.CodeAnalysis;

namespace DercelgefarKarhelchaye.Analyzer;

[Generator(LanguageNames.CSharp)]
public class IncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(initializationContext =>
        {
            initializationContext.AddSource("GeneratedCode.cs",
                """
                using System;
                namespace DercelgefarKarhelchaye
                {
                    public static class GeneratedCode
                    {
                        public static void Print()
                        {
                            Console.WriteLine("Hello from generated code!");
                        }
                    }
                }
                """);
        });
    }
}

