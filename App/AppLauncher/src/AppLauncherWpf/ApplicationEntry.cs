namespace AppLauncherWpf;

internal sealed record ApplicationEntry(
    ApplicationType Type,
    string Name,
    string EntryPath,
    string Source,
    string? ExecutablePath = null,
    IReadOnlyList<string>? Aliases = null)
{
    public string Initial => Name.Length > 0 ? Name[..1].ToUpperInvariant() : "?";

    public IEnumerable<string> SearchNames
    {
        get
        {
            yield return Name;

            if (Aliases is not null)
            {
                foreach (string alias in Aliases)
                {
                    yield return alias;
                }
            }
        }
    }
}

internal enum ApplicationType
{
    Win32Desktop,
    PackagedApplication,
    Shell
}
