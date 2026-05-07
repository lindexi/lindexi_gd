namespace JawjeleceeYairlubelhearrene.Models;

internal sealed record PowerPointReadResult(System.IO.FileInfo SourceFile, IReadOnlyList<PowerPointSlideInfo> Slides);