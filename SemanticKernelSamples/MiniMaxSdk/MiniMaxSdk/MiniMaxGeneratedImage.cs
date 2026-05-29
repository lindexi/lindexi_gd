namespace MiniMaxSdk;

public sealed record MiniMaxGeneratedImage(string? Url, byte[]? Bytes, string SuggestedFileExtension)
{
    public bool HasBinaryContent => Bytes is { Length: > 0 };
}