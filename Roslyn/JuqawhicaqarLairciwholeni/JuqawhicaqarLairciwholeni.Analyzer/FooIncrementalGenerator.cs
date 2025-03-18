using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JuqawhicaqarLairciwholeni.Analyzer;

[Generator(LanguageNames.CSharp)]
public class FooIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<GeneratedCodeInfo> sourceProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) =>
            {
                if (node is InvocationExpressionSyntax invocationExpressionSyntax)
                {
                    if (invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                    {
                        // 这是一个调用名为 WriteLine 的方法代码，但就不知道具体是谁的 WriteLine 了。语法过程中是无法知道具体的类型是哪个的
                        // 比如 Foo a = ...; a.WriteLine(...);
                        // 或 Foo b = ...; b.WriteLine(...);
                        // 此时最多在语法层面只判断出是 WriteLine 方法，进一步判断就交给语义过程了
                        return memberAccessExpressionSyntax.Name.Identifier.Text == "WriteLine";
                    }
                }

                return false;
            },
            (syntaxContext, _) =>
            {
                var symbolInfo = syntaxContext.SemanticModel.GetSymbolInfo(syntaxContext.Node);

                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol
                    // 以下这句判断纯属多余，因为语法过程中已经判断了是 WriteLine 方法
                    || methodSymbol.Name != "WriteLine")
                {
                    return default(GeneratedCodeInfo);
                }

                // 语义过程继续判断具体是否 Foo 类型的 WriteLine 方法
                var className = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (className != "global::JuqawhicaqarLairciwholeni.Foo")
                {
                    return default(GeneratedCodeInfo);
                }

                /*
                   class Foo
                   {
                       public void WriteLine(int message)
                       {
                           Console.WriteLine($"Foo: {message}");
                       }
                   }
                 */

                var invocationExpressionSyntax = (InvocationExpressionSyntax) syntaxContext.Node;
                ArgumentSyntax argumentSyntax = invocationExpressionSyntax.ArgumentList.Arguments.First();
                var argument = (int)syntaxContext.SemanticModel.GetConstantValue(argumentSyntax.Expression).Value!;

#pragma warning disable RSEXPERIMENTAL002 // 实验性警告，忽略即可
                var interceptableLocation = syntaxContext.SemanticModel.GetInterceptableLocation(invocationExpressionSyntax)!;

                var displayLocation = interceptableLocation.GetDisplayLocation();

                var generatedCode =
                    $$"""
                      using System.Runtime.CompilerServices;
                      
                      namespace Foo_JuqawhicaqarLairciwholeni
                      {
                          static partial class FooInterceptor
                          {
                              // {{displayLocation}}
                              [InterceptsLocation(version: {{interceptableLocation.Version}}, data: "{{interceptableLocation.Data}}")]
                              public static void InterceptorMethod{{argument}}(this {{className}} foo, int param)
                              {
                                  Console.WriteLine($"Interceptor{{argument}}: lindexi is doubi");
                              }
                          }
                      }

                      namespace System.Runtime.CompilerServices
                      {
                          [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                          file sealed class InterceptsLocationAttribute : Attribute
                          {
                              public InterceptsLocationAttribute(int version, string data)
                              {
                                  _ = version;
                                  _ = data;
                              }
                          }
                      }
                      """;

                return new GeneratedCodeInfo(generatedCode, $"FooInterceptor{argument}.cs");
            })
            .Where(t => t != default);

        context.RegisterImplementationSourceOutput(sourceProvider,
           (productionContext, provider) =>
           {
               productionContext.AddSource(provider.Name, provider.GeneratedCode);
           });
    }
}

readonly record struct GeneratedCodeInfo(string GeneratedCode, string Name);
