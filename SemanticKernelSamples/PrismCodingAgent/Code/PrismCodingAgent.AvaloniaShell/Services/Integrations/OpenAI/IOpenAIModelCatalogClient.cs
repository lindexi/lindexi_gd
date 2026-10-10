using System.Threading;
using System.Threading.Tasks;

namespace PrismCodingAgent.AvaloniaShell.Services.Integrations.OpenAI;

internal interface IOpenAIModelCatalogClient
{
    Task<OpenAIModelCatalogResult> GetModelIdsAsync
    (
        string endPoint,
        string apiKey,
        CancellationToken cancellationToken = default
    );
}