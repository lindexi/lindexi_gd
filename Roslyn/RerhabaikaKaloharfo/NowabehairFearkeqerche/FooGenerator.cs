using Microsoft.CodeAnalysis;

namespace NowabehairFearkeqerche
{
    [Generator(LanguageNames.CSharp)]
    public class FooGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var typeNameIncrementalValueProvider = context.CompilationProvider.Select((compilation, token) =>
            {
                var referencedAssemblySymbols = compilation.SourceModule.ReferencedAssemblySymbols;

                return referencedAssemblySymbols.Length;
            });

            context.RegisterSourceOutput(typeNameIncrementalValueProvider, (productionContext, provider) =>
            {

            });
        }
    }
}