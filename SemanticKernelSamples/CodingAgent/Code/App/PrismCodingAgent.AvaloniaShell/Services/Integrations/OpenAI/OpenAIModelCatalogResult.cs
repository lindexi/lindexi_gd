using System.Collections.Generic;

namespace PrismCodingAgent.AvaloniaShell.Services.Integrations.OpenAI;

internal sealed record OpenAIModelCatalogResult(IReadOnlyList<string> ModelIds, string? ErrorMessage)
{
    public bool IsSuccessful => ErrorMessage is null;

    public static OpenAIModelCatalogResult Success(IReadOnlyList<string> modelIds) => new(modelIds, null);

    public static OpenAIModelCatalogResult Failure(string message) => new([], message);
}