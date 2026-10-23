#:property TargetFramework=net10.0

using System.Runtime.CompilerServices;

var root = Path.GetDirectoryName(Path.GetDirectoryName(GetSourceFile()))!;

(string Source, string Destination)[] moves =
[
    ("MediaFoundation.Core/MediaPlaybackSession.cs", "MediaFoundation.Core/Playback/MediaPlaybackSession.cs"),
    ("MediaFoundation.Core/NativeCallback.cs", "MediaFoundation.Core/Playback/NativeCallback.cs"),
    ("MediaFoundation.Core/EvrPrototypePresenter.cs", "MediaFoundation.Core/Playback/EvrPrototypePresenter.cs"),
    ("MediaFoundation.Core/MediaEngineSession.cs", "MediaFoundation.Core/Playback/MediaEngineSession.cs"),
    ("MediaFoundation.Core/EngineCallback.cs", "MediaFoundation.Core/Playback/EngineCallback.cs"),
    ("MediaFoundation.Wpf/VideoHost.cs", "MediaFoundation.Wpf/Rendering/VideoHost.cs"),
    ("MediaFoundation.Wpf/MfPlayD3DImagePresenter.cs", "MediaFoundation.Wpf/Rendering/MfPlayD3DImagePresenter.cs"),
    ("MediaFoundation.Wpf/D3DImagePresenter.cs", "MediaFoundation.Wpf/Rendering/D3DImagePresenter.cs"),
    ("MediaFoundation.Wpf/SharedVideoSurface.cs", "MediaFoundation.Wpf/Rendering/SharedVideoSurface.cs"),
    ("MediaFoundation.PlayerDemo/PlaybackDiagnostics.cs", "MediaFoundation.PlayerDemo/Diagnostics/PlaybackDiagnostics.cs"),
    ("MediaFoundation.PlayerDemo/MediaProbe.cs", "MediaFoundation.PlayerDemo/Diagnostics/MediaProbe.cs"),
    ("MediaFoundation.PlayerDemo/MatroskaTracks.cs", "MediaFoundation.PlayerDemo/Diagnostics/MatroskaTracks.cs"),
    ("MediaFoundation.PlayerDemo/MfPlayPrototype.cs", "MediaFoundation.PlayerDemo/Diagnostics/MfPlayPrototype.cs"),
    ("MediaFoundation.PlayerDemo/SelfTest.cs", "MediaFoundation.PlayerDemo/Testing/SelfTest.cs"),
    ("MediaFoundation.PlayerDemo/ScalingTest.cs", "MediaFoundation.PlayerDemo/Testing/ScalingTest.cs"),
    ("MediaFoundation.PlayerDemo/SourcePathTest.cs", "MediaFoundation.PlayerDemo/Testing/SourcePathTest.cs"),
    ("MediaFoundation.PlayerDemo/VisualTest.cs", "MediaFoundation.PlayerDemo/Testing/VisualTest.cs"),
    ("MediaFoundation.PlayerDemo/MediaProbeTest.cs", "MediaFoundation.PlayerDemo/Testing/MediaProbeTest.cs"),
    ("MediaFoundation.PlayerDemo/LoopbackProbe.cs", "MediaFoundation.PlayerDemo/Testing/LoopbackProbe.cs"),
    ("MediaFoundation.PlayerDemo/MEDIA-DIAGNOSTICS.md", "Docs/MEDIA-DIAGNOSTICS.md")
];

foreach (var (source, destination) in moves)
{
    var target = Path.Combine(root, destination);
    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
    File.Move(Path.Combine(root, source), target);
    Console.WriteLine($"{source} -> {destination}");
}

var documentation = Path.Combine(root, "Docs", "ToolbarProgressValidation.md");
var text = File.ReadAllText(documentation)
    .Replace("MediaFoundation.Core/EvrPrototypePresenter.cs", "MediaFoundation.Core/Playback/EvrPrototypePresenter.cs", StringComparison.Ordinal)
    .Replace("MediaFoundation.Wpf/MfPlayD3DImagePresenter.cs", "MediaFoundation.Wpf/Rendering/MfPlayD3DImagePresenter.cs", StringComparison.Ordinal);
File.WriteAllText(documentation, text);
Console.WriteLine("Docs/ToolbarProgressValidation.md updated");

static string GetSourceFile([CallerFilePath] string sourceFile = "") => sourceFile;
