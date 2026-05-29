using System.Net.Http.Headers;
using System.Net.Http.Json;
using MiniMaxSdk.Internal.Serialization;
using MiniMaxSdk.Images.Internal.Payloads;
using MiniMaxSdk.Images.Models;
using MiniMaxSdk.Utilities;

namespace MiniMaxSdk.Images.Clients;

/// <summary>
/// MiniMax 图片生成客户端，支持文生图与图生图。
/// </summary>
public sealed class MiniMaxImageGenerationClient : IDisposable
{
    /// <summary>
    /// 初始化 <see cref="MiniMaxImageGenerationClient"/> 实例。
    /// </summary>
    /// <param name="apiKey">用于验证账户信息的 MiniMax API Key。</param>
    /// <param name="httpClient">可选的 <see cref="HttpClient"/> 实例。</param>
    public MiniMaxImageGenerationClient(string apiKey, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API Key 不能为空。", nameof(apiKey));
        }

        _apiKey = apiKey.Trim();
        _httpClient = httpClient ?? new HttpClient();
        _disposeHttpClient = httpClient is null;
    }

    private static readonly Uri ImageGenerationEndpoint = new("https://api.minimaxi.com/v1/image_generation");

    private static MiniMaxJsonSerializerContext SerializerContext => MiniMaxJsonSerializerContext.Default;

    private readonly HttpClient _httpClient;

    private readonly bool _disposeHttpClient;

    private readonly string _apiKey;

    /// <summary>
    /// 调用 MiniMax 文生图接口生成图片。
    /// </summary>
    /// <param name="request">图片生成请求。</param>
    /// <param name="cancellationToken">用于取消异步操作的取消令牌。</param>
    /// <returns>包含任务标识、生成结果与统计信息的图片生成结果。</returns>
    public async Task<MiniMaxImageGenerationResult> GenerateAsync(MiniMaxImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        return await GenerateCoreAsync(ImageGenerationRequestPayload.FromRequest(request), "文生图", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 调用 MiniMax 图生图接口生成图片。
    /// </summary>
    /// <param name="request">图生图请求。</param>
    /// <param name="cancellationToken">用于取消异步操作的取消令牌。</param>
    /// <returns>包含任务标识、生成结果与统计信息的图片生成结果。</returns>
    public async Task<MiniMaxImageGenerationResult> GenerateFromImageAsync(MiniMaxImageToImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        return await GenerateCoreAsync(ImageGenerationRequestPayload.FromRequest(request), "图生图", cancellationToken).ConfigureAwait(false);
    }

    private async Task<MiniMaxImageGenerationResult> GenerateCoreAsync(ImageGenerationRequestPayload payload, string scenarioName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ImageGenerationEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = JsonContent.Create(payload, SerializerContext.ImageGenerationRequestPayload);

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"MiniMax {scenarioName}请求失败，HTTP {(int)response.StatusCode} {response.ReasonPhrase}。响应内容：{responseContent}");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync(SerializerContext.ImageGenerationResponsePayload, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"MiniMax {scenarioName}响应为空。");

        if (apiResponse.BaseResp?.StatusCode is int statusCode && statusCode != 0)
        {
            throw new InvalidOperationException($"MiniMax {scenarioName}失败，状态码：{statusCode}，错误信息：{apiResponse.BaseResp.StatusMessage}");
        }

        var images = new List<MiniMaxGeneratedImage>();

        if (apiResponse.Data?.ImageBase64 is { Count: > 0 } imageBase64List)
        {
            foreach (var imageBase64 in imageBase64List)
            {
                var imageBytes = Convert.FromBase64String(imageBase64);
                images.Add(new MiniMaxGeneratedImage(null, imageBytes, ImageFileFormatDetector.GetFileExtension(imageBytes)));
            }
        }

        if (apiResponse.Data?.ImageUrls is { Count: > 0 } imageUrlList)
        {
            foreach (var imageUrl in imageUrlList)
            {
                images.Add(new MiniMaxGeneratedImage(imageUrl, null, ImageFileFormatDetector.GetFileExtensionFromUrl(imageUrl)));
            }
        }

        return new MiniMaxImageGenerationResult(
            apiResponse.Id,
            images,
            apiResponse.Metadata?.SuccessCount ?? images.Count,
            apiResponse.Metadata?.FailedCount ?? 0);
    }

    /// <summary>
    /// 释放当前客户端持有的资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}