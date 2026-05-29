namespace MiniMaxSdk;

public sealed record MiniMaxImageGenerationResult(string? TaskId, IReadOnlyList<MiniMaxGeneratedImage> Images, int SuccessCount, int FailedCount);