using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WhacadenaKewarfellaja.Analyzers
{
    [Generator(LanguageNames.CSharp)]
    public class CodeCollectionIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 找到对什么文件感兴趣
            // 例如对全体的 cs 代码感兴趣
            IncrementalValueProvider<Compilation> compilations =
                context.CompilationProvider
                        // 这里的 Select 是仿照 Linq 写的，可不是真的 Linq 哦，只是一个叫 Select 的方法
                        // public static IncrementalValueProvider<TResult> Select<TSource,TResult>(this IncrementalValueProvider<TSource> source, Func<TSource,CancellationToken,TResult> selector)
                    .Select((compilation, cancellationToken) => compilation);

            context.RegisterSourceOutput(compilations, (sourceProductionContext, compilation) =>
            {
                var syntaxTree = compilation.SyntaxTrees.FirstOrDefault();
                if (syntaxTree == null)
                {
                    return;
                }

                var root = syntaxTree.GetRoot(sourceProductionContext.CancellationToken);

                // 选择给 Program 的附加上
                var classDeclarationSyntax = root
                    .DescendantNodes(descendIntoTrivia: true)
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault();
                if (classDeclarationSyntax?.Identifier.Text!="Program")
                {
                    // 如果变更的非预期类型，那就不加上代码，否则代码将会重复加入
                    return;
                }

                // 这是一个很强的技术，在代码没有变更的情况下，多次构建，是可以看到不会重复进入此逻辑，也就是 Count 属性没有加一
                // 可以试试对一个大的项目，修改部分代码，看看 Count 属性

                string source = $@"
using System;

namespace WhacadenaKewarfellaja
{{
    public static partial class Program
    {{
        static partial void HelloFrom(string name)
        {{
            Console.WriteLine($""构建 {Count} 次 says: Hi from '{{name}}'"");
        }}
    }}
}}
";
                sourceProductionContext.AddSource("GeneratedSourceTest", source);

                Count++;
            });
        }

        private static int Count { set; get; } = 0;
    }
}
