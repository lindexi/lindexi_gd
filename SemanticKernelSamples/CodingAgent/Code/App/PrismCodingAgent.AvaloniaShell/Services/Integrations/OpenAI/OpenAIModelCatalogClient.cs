using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Models;

namespace PrismCodingAgent.AvaloniaShell.Services.Integrations.OpenAI;

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
            return OpenAIModelCatalogResult.Failure(Localizations.Lang.Current.CatalogEndpointRequired);
        }

        if (!Uri.TryCreate(endPoint, UriKind.Absolute, out Uri? endpointUri))
        {
            return OpenAIModelCatalogResult.Failure(Localizations.Lang.Current.CatalogEndpointInvalid);
        }

        var options = new OpenAIClientOptions
        {
            Endpoint = endpointUri,
        };
        var client = new OpenAIClient(new ApiKeyCredential(string.IsNullOrEmpty(apiKey) ? "unused" : apiKey), options);
        OpenAIModelClient modelClient = client.GetOpenAIModelClient();
        ClientResult<OpenAIModelCollection> result;
        try
        {
            result = await modelClient.GetModelsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ClientResultException exception)
        {
            return Failure(Localizations.Lang.Current.CatalogResponseError, exception);
        }
        catch (JsonException exception)
        {
            return Failure(Localizations.Lang.Current.CatalogJsonInvalid, exception);
        }
        catch (HttpRequestException exception)
        {
            return Failure(Localizations.Lang.Current.CatalogConnectionFailed, exception);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return OpenAIModelCatalogResult.Failure(Localizations.Lang.Current.CatalogTimeout);
        }
        catch (IOException exception)
        {
            return Failure(Localizations.Lang.Current.CatalogReadFailed, exception);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(Localizations.Lang.Current.CatalogProtocolInvalid, exception);
        }

        var modelIds = new List<string>(result.Value.Count);
        var uniqueModelIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < result.Value.Count; index++)
        {
            string modelId = result.Value[index].Id;
            if (string.IsNullOrWhiteSpace(modelId))
            {
                return OpenAIModelCatalogResult.Failure(Localizations.Lang.Format(Localizations.Lang.Current.CatalogBlankId, index + 1));
            }

            if (!uniqueModelIds.Add(modelId))
            {
                return OpenAIModelCatalogResult.Failure(Localizations.Lang.Format(Localizations.Lang.Current.CatalogDuplicateId, modelId));
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