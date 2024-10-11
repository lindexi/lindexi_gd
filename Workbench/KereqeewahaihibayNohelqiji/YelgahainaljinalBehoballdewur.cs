using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace KereqeewahaihibayNohelqiji
{
    [Generator(LanguageNames.CSharp)]
    internal class YelgahainaljinalBehoballdewur : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 在这里编写代码
            context.RegisterImplementationSourceOutput(context.AdditionalTextsProvider.Collect().Combine(context.AnalyzerConfigOptionsProvider),
                (productionContext, provider) =>
                {
                    // 这里的代码只有当配置初始化或变更时才会被执行
                    var additionalTextArray = provider.Left;
                    var analyzerConfigOptionsProvider = provider.Right;

                    var stringBuilder = new StringBuilder();
                    for (int i = 0; i < 3; i++)
                    {
                        stringBuilder.Append('"');
                    }
                    stringBuilder.AppendLine();

                    foreach (var additionalText in additionalTextArray)
                    {
                        var analyzerConfigOptions = analyzerConfigOptionsProvider.GetOptions(additionalText);
                        if (analyzerConfigOptions.TryGetValue("build_metadata.AdditionalFiles.Link", out var link))
                        {
                            stringBuilder.AppendLine($"File={additionalText.Path}");
                            stringBuilder.AppendLine($"Link={link}");
                        }
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        stringBuilder.Append('"');
                    }

                    var code = @"using System;
namespace CujelcijallChearjawjuja
{
    public static class Foo
    {
        public static void F1()
        {
            Console.WriteLine(" + stringBuilder.ToString() + @");
        }
    }
}";
                    productionContext.AddSource("Foo.cs", code);
                });
        }
    }
}