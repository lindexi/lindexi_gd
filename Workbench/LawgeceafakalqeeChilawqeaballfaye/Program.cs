// See https://aka.ms/new-console-template for more information
using System.Text.RegularExpressions;

Span<string> text = ["package/Abroad", "package/Abroad_win-x64"];
foreach (var t in text)
{
    Regex regex = new(@"package/([\w-]+)");
    var match = regex.Match(t);

    var customAndRuntimeIdentifier = match.Groups[1].Value;

    var index = customAndRuntimeIdentifier.IndexOf('_');

    var custom = customAndRuntimeIdentifier;
    string? runtimeIdentifier = null;
    if (index > 0)
    {
        custom = customAndRuntimeIdentifier.Substring(0, index);
        runtimeIdentifier = customAndRuntimeIdentifier.Substring(index + 1);
    }

    Console.WriteLine($"Text={t} custom: {custom}, runtimeIdentifier: {runtimeIdentifier}");
}
