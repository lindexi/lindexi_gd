using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyzers;

[Generator(LanguageNames.CSharp)]
public class FooTelescopeIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var internalsVisibleFromAssemblyNameListIncrementalValueProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            var internalsVisibleFromAssemblyNameList = new List<string>();

            // 获取到所有引用程序集
            var referencedAssemblySymbols = compilation.SourceModule.ReferencedAssemblySymbols;

            foreach (IAssemblySymbol? referencedAssemblySymbol in referencedAssemblySymbols)
            {
                var name = referencedAssemblySymbol.Name;

                if (referencedAssemblySymbol.GivesAccessTo(compilation.Assembly))
                {
                    internalsVisibleFromAssemblyNameList.Add(name);
                }
            }

            return internalsVisibleFromAssemblyNameList;
        });

        context.RegisterSourceOutput(internalsVisibleFromAssemblyNameListIncrementalValueProvider, (productionContext, list) =>
        {
            var code = $@"
    public static class InternalsVisibleToHelper
    {{
        public static IEnumerable<string> GetAllInternalsVisibleFromAssemblyName()
        {{
            {(string.Join("\r\n", list.Select(t => $@"yield return ""{t}"";")))}
        }}
    }}";
            productionContext.AddSource("InternalsVisibleToHelper", code);
        });
    }
}
