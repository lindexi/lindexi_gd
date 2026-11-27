using OpenAI.Responses;

namespace PptxGenerator.Chat.Services;

internal readonly record struct ChatUsage(long Total, long Input, long Output, long? Reasoning, long? Cached)
{
    internal ChatUsage Add(ResponseTokenUsage usage) => new(
        Total + usage.TotalTokenCount,
        Input + usage.InputTokenCount,
        Output + usage.OutputTokenCount,
        Sum(Reasoning, usage.OutputTokenDetails?.ReasoningTokenCount),
        Sum(Cached, usage.InputTokenDetails?.CachedTokenCount));

    private static long? Sum(long? accumulated, int? reported) =>
        reported is { } count ? accumulated.GetValueOrDefault() + count : accumulated;
}
