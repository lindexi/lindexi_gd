using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LainewihereJerejawwerye.Analyzers
{
    [Generator(LanguageNames.CSharp)]
    public class CodeCollectionIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var incrementalValuesProvider = context.SyntaxProvider.CreateSyntaxProvider((node, _) =>
            {
                // 只读取方法定义。这里例子是读取一个方法的返回值
                return node.IsKind(SyntaxKind.MethodDeclaration);
            }, (syntaxContext, _) =>
            {
                // 转换为语义
                IMethodSymbol methodSymbol =
                    syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) as IMethodSymbol;
                if (methodSymbol is null)
                {
                    // 理论上不会进入这个代码，前面判断方法定义
                    return string.Empty;
                }

                // 获取方法返回类型
                ITypeSymbol methodSymbolReturnType = methodSymbol.ReturnType;
                if (methodSymbolReturnType is INamedTypeSymbol namedTypeSymbol)
                {
                    // 由于 ValueTuple 是值类型，因此可以快速判断是否值类型
                    if (namedTypeSymbol.IsValueType && namedTypeSymbol.TupleElements.Length > 0)
                    {
                        foreach (var tupleElement in namedTypeSymbol.TupleElements)
                        {
                            // 如此可以获取每一个类型
                            var tupleElementType = tupleElement.Type;
                        }

                        // 也可以获取原始代码的定义
                        var code = namedTypeSymbol.DeclaringSyntaxReferences[0].GetSyntax().ToString();
                        return code;
                    }
                }

                return "";
            }).Where(t => !string.IsNullOrEmpty(t));

            context.RegisterImplementationSourceOutput(incrementalValuesProvider,
                (productionContext, provider) =>
                {
                    var text = provider;

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