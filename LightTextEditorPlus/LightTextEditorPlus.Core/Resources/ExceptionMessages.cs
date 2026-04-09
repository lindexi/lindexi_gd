using System.Globalization;
using System.Resources;

namespace LightTextEditorPlus.Core.Resources;

internal static class ExceptionMessages
{
    private static readonly ResourceManager ResourceManager =
        new("LightTextEditorPlus.Core.Resources.ExceptionMessages", typeof(ExceptionMessages).Assembly);

    internal static string Get(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture)
               ?? throw new MissingManifestResourceException($"Missing exception message resource '{name}'.");
    }

    internal static string Format(string name, params object?[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(name), args);
    }
}
