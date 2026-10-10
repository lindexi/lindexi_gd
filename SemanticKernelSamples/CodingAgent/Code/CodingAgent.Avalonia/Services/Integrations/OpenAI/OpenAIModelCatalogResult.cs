using System.Collections.Generic;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed record OpenAIModelCatalogResult(IReadOnlyList<string> ModelIds, string? ErrorMessage)
{
    public bool IsSuccessful => ErrorMessage is null;

    public static OpenAIModelCatalogResult Success(IReadOnlyList<string> modelIds) => new(modelIds, null);

    public static OpenAIModelCatalogResult Failure(string message) => new([], message);
}