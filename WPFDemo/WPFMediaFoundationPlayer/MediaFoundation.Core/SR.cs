using System.Resources;

namespace MediaFoundation.Core;

internal static class SR
{
    private static readonly ResourceManager Resources = new("MediaFoundation.Core.Strings", typeof(SR).Assembly);
    internal static string Get(string name) => Resources.GetString(name) ?? name;
}
