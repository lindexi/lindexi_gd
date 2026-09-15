using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Models;
using System.ClientModel;

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

internal sealed record OpenAIModelCatalogResult(IReadOnlyList<string> ModelIds, string? ErrorMessage)
{
    public bool IsSuccessful => ErrorMessage is null;

    public static OpenAIModelCatalogResult Success(IReadOnlyList<string> modelIds) => new(modelIds, null);

    public static OpenAIModelCatalogResult Failure(string message) => new([], message);
}

internal sealed class OpenAIModelCatalogClient : IOpenAIModelCatalogClient
{
    public async Task<OpenAIModelCatalogResult> GetModelIdsAsync
    (
        string endPoint,
        string apiKey,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(endPoint))
        {
            return OpenAIModelCatalogResult.Failure("请先填写模型服务地址。");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return OpenAIModelCatalogResult.Failure("请先填写 API 密钥。");
        }

        if (!Uri.TryCreate(endPoint, UriKind.Absolute, out Uri? endpointUri))
        {
            return OpenAIModelCatalogResult.Failure("模型服务地址格式无效。");
        }

        var options = new OpenAIClientOptions
        {
            Endpoint = endpointUri,
        };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        OpenAIModelClient modelClient = client.GetOpenAIModelClient();
        ClientResult<OpenAIModelCollection> result;
        try
        {
            result = await modelClient.GetModelsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ClientResultException exception)
        {
            return OpenAIModelCatalogResult.Failure($"获取模型列表失败：{exception.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return OpenAIModelCatalogResult.Failure("获取模型列表超时。");
        }

        var modelIds = new List<string>(result.Value.Count);
        var uniqueModelIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < result.Value.Count; index++)
        {
            string modelId = result.Value[index].Id;
            if (string.IsNullOrWhiteSpace(modelId))
            {
                return OpenAIModelCatalogResult.Failure($"模型列表中的第 {index + 1} 项包含空白模型 ID。");
            }

            if (!uniqueModelIds.Add(modelId))
            {
                return OpenAIModelCatalogResult.Failure($"模型列表包含重复的模型 ID：{modelId}");
            }

            modelIds.Add(modelId);
        }

        return OpenAIModelCatalogResult.Success(modelIds);
    }
}