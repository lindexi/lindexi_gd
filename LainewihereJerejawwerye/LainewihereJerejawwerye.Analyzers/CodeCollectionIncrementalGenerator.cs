using System;
using Microsoft.CodeAnalysis;

namespace LainewihereJerejawwerye.Analyzers
{
    [Generator(LanguageNames.CSharp)]
    public class CodeCollectionIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterImplementationSourceOutput(context.AnalyzerConfigOptionsProvider,
                (productionContext, provider) =>
                {
                    var text = string.Empty;

                    // 通过 csproj 等 PropertyGroup 里面获取
                    // 需要将可见的，放入到 CompilerVisibleProperty 里面
                    // 需要加上 `build_property.` 前缀
                    if (provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var myCustomProperty))
                    {
                        text += " " + myCustomProperty;
                    }

                    var code = @"using System;
namespace LainewihereJerejawwerye
{
    public static class Foo
    {
        public static void F1()
        {
            Console.WriteLine(""" + text + @""");
        }
    }
}";
                    productionContext.AddSource("Demo", code);
                });
        }
    }
}