using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QewelechairWhegalnarlaywhe.Analyzer
{
    [Generator(LanguageNames.CSharp)]
    public class FooGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<Compilation> t1 = context.CompilationProvider;
            IncrementalValueProvider<IAssemblySymbol> incrementalValueProvider = t1.Select((t,_)=>t.Assembly);

           context.RegisterSourceOutput(t1, (productionContext, compilation) =>
           {

           });

           IncrementalValuesProvider<AdditionalText> t2 = context.AdditionalTextsProvider;
           context.RegisterSourceOutput(t2, (productionContext, text) =>
           {

           });

           IncrementalValueProvider<ImmutableArray<AdditionalText>> t3 = t2.Collect();

           IncrementalValueProvider<(Compilation Left, ImmutableArray<AdditionalText> Right)> ta = t1.Combine(t3);

           IncrementalValuesProvider<AdditionalText> tb = t2;

           IncrementalValueProvider<(ImmutableArray<AdditionalText> Left, ImmutableArray<AdditionalText> Right)> tc = t2.Collect().Combine(tb.Collect());

           IncrementalValuesProvider<AdditionalText> td = tc.SelectMany((tuple, _) => { return tuple.Left.Concat(tuple.Right); } );

           var compilerOptions = context.CompilationProvider.Select((s, _) => s.Options);
            context.RegisterSourceOutput(compilerOptions, static (productionContext, options) =>
            {
                var code = $@"
using System;
using System.Globalization;

public static class BuildInformation
{{
    /// <summary>
    /// Returns the build date (UTC).
    /// </summary>
    public static readonly DateTime BuildAt = DateTime.ParseExact(""{DateTime.UtcNow:O}"", ""O"", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    /// <summary>
    /// Returns the platform.
    /// </summary>
    public const string Platform = ""{options.Platform}"";
    /// <summary>
    /// Returns the configuration.
    /// </summary>
    public const string Configuration = ""{options.OptimizationLevel}"";
}}
";

                productionContext.AddSource("LinkDotNet.BuildInformation.g", code);
            });
        }
    }
}
