using System.Threading;
using System.Threading.Tasks;

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface IOpenAIModelCatalogClient
{
    Task<OpenAIModelCatalogResult> GetModelIdsAsync
    (
        string endPoint,
        string apiKey,
        CancellationToken cancellationToken = default
    );
}