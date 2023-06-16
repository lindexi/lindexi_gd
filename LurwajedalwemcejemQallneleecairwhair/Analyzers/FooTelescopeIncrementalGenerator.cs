using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Analyzers;

[Generator(LanguageNames.CSharp)]
public class FooTelescopeIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeNameIncrementalValueProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            var typeNameList = new List<string>();

            // 获取到所有引用程序集
            var referencedAssemblySymbols = compilation.SourceModule.ReferencedAssemblySymbols;

            // 为了方便代码理解，这里只取名为 Lib 程序集的内容…
            foreach (var referencedAssemblySymbol in referencedAssemblySymbols)
            {
                var name = referencedAssemblySymbol.Name;

                if (name.Contains("Lib"))
                {
                    // 获取所有的类型
                    // 这里 ToList 只是为了方便调试
                    var allTypeSymbol = GetAllTypeSymbol(referencedAssemblySymbol.GlobalNamespace).ToList();

                    foreach (var typeSymbol in allTypeSymbol)
                    {
                        typeNameList.Add(typeSymbol.ToDisplayString());
                    }
                }
                else
                {
                    // 其他的引用程序集，在这里就忽略
                }
            }

            return typeNameList;
        });

        context.RegisterSourceOutput(typeNameIncrementalValueProvider, (productionContext, list) =>
        {
            var code = $@"
    public static class FooHelper
    {{
        public static IEnumerable<string> GetAllTypeName()
        {{
            {(string.Join("\r\n", list.Select(t => $@"yield return ""{t}"";")))}
        }}
    }}";
            productionContext.AddSource("FooHelper", code);
        });
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypeSymbol(INamespaceSymbol namespaceSymbol)
    {
        var typeMemberList = namespaceSymbol.GetTypeMembers();

        foreach (var typeSymbol in typeMemberList)
        {
            yield return typeSymbol;
        }

        foreach (var namespaceMember in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var typeSymbol in GetAllTypeSymbol(namespaceMember))
            {
                yield return typeSymbol;
            }
        }
    }
}
