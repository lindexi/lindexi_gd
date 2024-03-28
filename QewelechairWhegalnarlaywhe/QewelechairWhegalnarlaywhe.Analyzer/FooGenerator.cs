using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;

namespace QewelechairWhegalnarlaywhe.Analyzer
{
    [Generator(LanguageNames.CSharp)]
    public class FooGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
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
