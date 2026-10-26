#:property TargetFramework=net10.0

using System.Runtime.CompilerServices;

var root = Path.GetDirectoryName(Path.GetDirectoryName(GetSourceFile()))!;

(string Source, string Destination)[] moves =
[
    ("MediaFoundation.Core/Playback/EvrPrototypePresenter.cs", "MediaFoundation.Core/Playback/EVRPrototypePresenter.cs"),
    ("MediaFoundation.Wpf/Rendering/MfPlayD3DImagePresenter.cs", "MediaFoundation.Wpf/Rendering/MFPlayD3DImagePresenter.cs"),
    ("MediaFoundation.PlayerDemo/Diagnostics/MfPlayPrototype.cs", "MediaFoundation.PlayerDemo/Diagnostics/MFPlayPrototype.cs")
];

foreach (var (source, destination) in moves)
{
    File.Move(Path.Combine(root, source), Path.Combine(root, destination));
    Console.WriteLine($"{source} -> {destination}");
}

foreach (var document in Directory.EnumerateFiles(Path.Combine(root, "Docs"), "*.md"))
{
    var text = File.ReadAllText(document)
        .Replace("EvrPrototypePresenter", "EVRPrototypePresenter", StringComparison.Ordinal)
        .Replace("MfPlayD3DImagePresenter", "MFPlayD3DImagePresenter", StringComparison.Ordinal)
        .Replace("MfPlayPrototype", "MFPlayPrototype", StringComparison.Ordinal);
    File.WriteAllText(document, text);
}

static string GetSourceFile([CallerFilePath] string sourceFile = "") => sourceFile;
