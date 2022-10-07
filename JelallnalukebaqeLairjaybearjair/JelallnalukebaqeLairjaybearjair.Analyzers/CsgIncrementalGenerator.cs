using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis;

namespace JelallnalukebaqeLairjaybearjair.Analyzers
{
    [Generator(LanguageNames.CSharp)]
    public class CsgIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var csgFileIncrementalValuesProvider =
            context.AdditionalTextsProvider.Where(t =>
                string.Equals(Path.GetExtension(t.Path), ".csg", StringComparison.OrdinalIgnoreCase));

            context.RegisterSourceOutput(csgFileIncrementalValuesProvider, (sourceProductionContext, csg) =>
            {
                AddFrameworkCode(sourceProductionContext);

                var csgSource = csg.GetText();
                if (csgSource == null) return;

                var keyDictionary = new Dictionary<string, string>()
                {
                    {"引用命名空间 ","using "},
                    {"定义命名空间 ","namespace "},
                    {"类型 ","class "},
                    {"公开的 ","public "},
                    {"静态的 ","static "},
                    {"无返回值类型的 ","void "},
                };

                var stringBuilder = new StringBuilder();
                foreach (var textLine in csgSource.Lines)
                {
                    var text = textLine.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        foreach (var keyValuePair in keyDictionary)
                        {
                            text = text.Replace(keyValuePair.Key, keyValuePair.Value);
                        }
                    }

                    stringBuilder.AppendLine(text);
                }

                sourceProductionContext.AddSource(Path.GetFileNameWithoutExtension(csg.Path) + ".g.cs", stringBuilder.ToString());
            });
        }

        /// <summary>
        /// 添加框架代码
        /// </summary>
        /// <param name="sourceProductionContext"></param>
        private static void AddFrameworkCode(SourceProductionContext sourceProductionContext)
        {
            string consoleText = @"
using System;

namespace 系统;

static class 控制台
{
    public static void 输出一行文本(string 文本)
    {
        Console.WriteLine(文本);
    }
}";
            sourceProductionContext.AddSource("DefaultConsole", consoleText);
        }
    }
}
