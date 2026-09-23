using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Models;
using System.ClientModel;

namespace CodingChatRoom.AvaloniaShell.Services;

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
            return Failure("模型服务返回了错误响应。请检查服务地址、API 密钥和服务状态。", exception);
        }
        catch (JsonException exception)
        {
            return Failure("模型服务返回的内容不是有效的 OpenAI 模型列表。请检查服务地址是否指向 OpenAI 兼容 API。", exception);
        }
        catch (HttpRequestException exception)
        {
            return Failure("无法连接模型服务。请检查服务地址、网络和代理设置。", exception);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return OpenAIModelCatalogResult.Failure("获取模型列表超时。");
        }
        catch (IOException exception)
        {
            return Failure("读取模型服务响应失败。请稍后重试。", exception);
        }
        catch (InvalidOperationException exception)
        {
            return Failure("模型服务响应不符合 OpenAI 模型列表协议。", exception);
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

    private static OpenAIModelCatalogResult Failure(string message, Exception exception)
    {
        Trace.TraceError($"获取 OpenAI 模型列表失败：{exception}");
        return OpenAIModelCatalogResult.Failure(message);
    }
}