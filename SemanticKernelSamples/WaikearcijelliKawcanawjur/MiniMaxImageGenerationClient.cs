using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WaikearcijelliKawcanawjur;

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

public sealed record MiniMaxImageGenerationRequest(
    string Prompt,
    string Model = MiniMaxImageGenerationModels.Image01,
    string? AspectRatio = null,
    int? Width = null,
    int? Height = null,
    string ResponseFormat = MiniMaxImageResponseFormats.Base64,
    long? Seed = null,
    int Count = 1,
    bool PromptOptimizer = false,
    bool? AigcWatermark = null,
    MiniMaxImageStyle? Style = null)
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Prompt))
        {
            throw new ArgumentException("Prompt 不能为空。", nameof(Prompt));
        }

        if (Prompt.Length > 1500)
        {
            throw new ArgumentException("Prompt 长度不能超过 1500 个字符。", nameof(Prompt));
        }

        if (!MiniMaxImageGenerationModels.IsSupported(Model))
        {
            throw new ArgumentException($"不支持的模型：{Model}", nameof(Model));
        }

        if (AspectRatio is not null && !MiniMaxImageAspectRatios.IsSupported(AspectRatio))
        {
            throw new ArgumentException($"不支持的宽高比：{AspectRatio}", nameof(AspectRatio));
        }

        if (!MiniMaxImageResponseFormats.IsSupported(ResponseFormat))
        {
            throw new ArgumentException($"不支持的返回格式：{ResponseFormat}", nameof(ResponseFormat));
        }

        if (Count is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(Count), Count, "生成图片数量需在 1 到 9 之间。");
        }

        if (Width.HasValue ^ Height.HasValue)
        {
            throw new ArgumentException("Width 和 Height 需要同时设置。");
        }

        if (Width.HasValue && Height.HasValue)
        {
            ValidateSize(nameof(Width), Width.Value);
            ValidateSize(nameof(Height), Height.Value);
        }

        Style?.Validate();
    }

    private static void ValidateSize(string argumentName, int value)
    {
        if (value is < 512 or > 2048)
        {
            throw new ArgumentOutOfRangeException(argumentName, value, "图片尺寸需在 512 到 2048 之间。");
        }

        if (value % 8 != 0)
        {
            throw new ArgumentException("图片尺寸必须为 8 的倍数。", argumentName);
        }
    }
}

public sealed record MiniMaxImageStyle(string StyleType, float? StyleWeight = null)
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(StyleType))
        {
            throw new ArgumentException("画风类型不能为空。", nameof(StyleType));
        }

        if (StyleWeight.HasValue && (StyleWeight.Value <= 0 || StyleWeight.Value > 1))
        {
            throw new ArgumentOutOfRangeException(nameof(StyleWeight), StyleWeight, "画风权重需在 (0, 1] 区间内。");
        }
    }
}

public sealed record MiniMaxGeneratedImage(string? Url, byte[]? Bytes, string SuggestedFileExtension)
{
    public bool HasBinaryContent => Bytes is { Length: > 0 };
}

public sealed record MiniMaxImageGenerationResult(string? TaskId, IReadOnlyList<MiniMaxGeneratedImage> Images, int SuccessCount, int FailedCount);

public static class MiniMaxImageGenerationModels
{
    public const string Image01 = "image-01";
    public const string Image01Live = "image-01-live";

    public static bool IsSupported(string model) => model is Image01 or Image01Live;
}

public static class MiniMaxImageAspectRatios
{
    public const string Square = "1:1";
    public const string Landscape16By9 = "16:9";
    public const string Standard4By3 = "4:3";
    public const string Standard3By2 = "3:2";
    public const string Portrait2By3 = "2:3";
    public const string Portrait3By4 = "3:4";
    public const string Portrait9By16 = "9:16";
    public const string Ultrawide21By9 = "21:9";

    public static bool IsSupported(string aspectRatio) => aspectRatio is Square or Landscape16By9 or Standard4By3 or Standard3By2 or Portrait2By3 or Portrait3By4 or Portrait9By16 or Ultrawide21By9;
}

public static class MiniMaxImageResponseFormats
{
    public const string Url = "url";
    public const string Base64 = "base64";

    public static bool IsSupported(string responseFormat) => responseFormat is Url or Base64;
}

internal sealed record ImageGenerationRequestPayload(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("style")] StylePayload? Style,
    [property: JsonPropertyName("aspect_ratio")] string? AspectRatio,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("response_format")] string ResponseFormat,
    [property: JsonPropertyName("seed")] long? Seed,
    [property: JsonPropertyName("n")] int Count,
    [property: JsonPropertyName("prompt_optimizer")] bool PromptOptimizer,
    [property: JsonPropertyName("aigc_watermark")] bool? AigcWatermark)
{
    public static ImageGenerationRequestPayload FromRequest(MiniMaxImageGenerationRequest request)
    {
        return new ImageGenerationRequestPayload(
            request.Model,
            request.Prompt,
            request.Style is null ? null : new StylePayload(request.Style.StyleType, request.Style.StyleWeight),
            request.AspectRatio,
            request.Width,
            request.Height,
            request.ResponseFormat,
            request.Seed,
            request.Count,
            request.PromptOptimizer,
            request.AigcWatermark);
    }
}

internal sealed record StylePayload(
    [property: JsonPropertyName("style_type")] string StyleType,
    [property: JsonPropertyName("style_weight")] float? StyleWeight);

internal sealed record ImageGenerationResponsePayload(
    [property: JsonPropertyName("data")] ImageDataPayload? Data,
    [property: JsonPropertyName("metadata")] MetadataPayload? Metadata,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("base_resp")] BaseResponsePayload? BaseResp);

internal sealed record ImageDataPayload(
    [property: JsonPropertyName("image_urls")] IReadOnlyList<string>? ImageUrls,
    [property: JsonPropertyName("image_base64")] IReadOnlyList<string>? ImageBase64);

internal sealed record MetadataPayload(
    [property: JsonPropertyName("success_count")] int SuccessCount,
    [property: JsonPropertyName("failed_count")] int FailedCount);

internal sealed record BaseResponsePayload(
    [property: JsonPropertyName("status_code")] int StatusCode,
    [property: JsonPropertyName("status_msg")] string? StatusMessage);

internal static class ImageFileFormatDetector
{
    public static string GetFileExtension(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);

        return imageBytes.AsSpan() switch
        {
            [0x89, 0x50, 0x4E, 0x47, ..] => ".png",
            [0xFF, 0xD8, 0xFF, ..] => ".jpg",
            [0x47, 0x49, 0x46, 0x38, ..] => ".gif",
            [0x52, 0x49, 0x46, 0x46, ..] when imageBytes.Length >= 12 && imageBytes[8] == 0x57 && imageBytes[9] == 0x45 && imageBytes[10] == 0x42 && imageBytes[11] == 0x50 => ".webp",
            _ => ".bin"
        };
    }

    public static string GetFileExtensionFromUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return ".bin";
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return ".bin";
        }

        var extension = Path.GetExtension(uri.AbsolutePath);
        return string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
    }
}
