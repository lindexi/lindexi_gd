using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNetCampus.Storage.Analyzer;

static class SaveInfoNodeParserGeneratorConfigHelper
{
    public static IncrementalValueProvider<SaveInfoNodeParserGeneratorConfigOption> GetConfigOption(this IncrementalValueProvider<AnalyzerConfigOptionsProvider> analyzerProvider)
    {
        return analyzerProvider.Select((t, _) =>
        {
            bool shouldGenerateSaveInfoNodeParser = false;
            if (t.GlobalOptions.TryGetValue("build_property.GenerateSaveInfoNodeParser",
                    out var generateSaveInfoNodeParser))
            {
                bool.TryParse(generateSaveInfoNodeParser, out shouldGenerateSaveInfoNodeParser);
            }

            if (!t.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace))
            {
                rootNamespace = null;
            }

            return new SaveInfoNodeParserGeneratorConfigOption()
            {
                ShouldGenerateSaveInfoNodeParser = shouldGenerateSaveInfoNodeParser,
                RootNamespace = rootNamespace,
            };
        });
    }

    public static IncrementalValuesProvider<SaveInfoClassInfo> FilterAndUpdateNamespace(this IncrementalValuesProvider<SaveInfoClassInfo> classInfoProvider, IncrementalValueProvider<SaveInfoNodeParserGeneratorConfigOption> configurationProvider)
    {
        var provider = classInfoProvider
            .Combine(configurationProvider)
            .Select((t, _) =>
            {
                if (!t.Right.ShouldGenerateSaveInfoNodeParser)
                {
                    return null;
                }

                if (t.Right.RootNamespace != null)
                {
                    return t.Left with
                    {
                        Namespace = t.Right.RootNamespace
                    };
                }
                return t.Left;
            })
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);
        return provider;
    }
}