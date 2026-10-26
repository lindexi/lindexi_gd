using System.Resources;

namespace MediaFoundation.Core;

internal static class SR
{
    private static readonly ResourceManager s_resources = new("MediaFoundation.Core.Strings", typeof(SR).Assembly);
    internal static string Get(string name) => s_resources.GetString(name) ?? name;
}