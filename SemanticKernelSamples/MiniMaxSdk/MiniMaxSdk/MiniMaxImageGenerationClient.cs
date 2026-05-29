using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniMaxSdk;

public sealed class MiniMaxImageGenerationClient : IDisposable
{
    private static readonly Uri ImageGenerationEndpoint = new("https://api.minimaxi.com/v1/image_generation");
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly string _apiKey;

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

    /// <summary>
    /// 调用 MiniMax 文生图接口生成图片。
    /// </summary>
    public async Task<MiniMaxImageGenerationResult> GenerateAsync(MiniMaxImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ImageGenerationEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = JsonContent.Create(ImageGenerationRequestPayload.FromRequest(request), options: SerializerOptions);

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"MiniMax 文生图请求失败，HTTP {(int)response.StatusCode} {response.ReasonPhrase}。响应内容：{responseContent}");
        }

        var apiResponse = JsonSerializer.Deserialize<ImageGenerationResponsePayload>(responseContent, SerializerOptions)
            ?? throw new InvalidOperationException("MiniMax 文生图响应为空。");

        if (apiResponse.BaseResp?.StatusCode is int statusCode && statusCode != 0)
        {
            throw new InvalidOperationException($"MiniMax 文生图失败，状态码：{statusCode}，错误信息：{apiResponse.BaseResp.StatusMessage}");
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

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}