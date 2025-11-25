using Microsoft.CodeAnalysis;

namespace LurlelnarkallChijurjeaqelba;

[Generator(LanguageNames.CSharp)]
public class IncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<string> configurationProvider = context.AnalyzerConfigOptionsProvider.Select((t, _) =>
        {
            var globalOptions = t.GlobalOptions;
            if (globalOptions.TryGetValue("build_property.FooProperty", out var property))
            {
                return property;
            }

            return null;
        });

        context.RegisterSourceOutput(configurationProvider, (productionContext, configurationProperty) =>
        {
            productionContext.AddSource("GeneratedCode.cs",
                $$"""
                  using System;
                  
                  namespace LurlelnarkallChijurjeaqelba
                  {
                      public static class GeneratedCode
                      {
                          public static void Print()
                          {
                              Console.WriteLine("配置的属性 {{configurationProperty}}");
                          }
                      }
                  }
                  """);
        });
    }
}