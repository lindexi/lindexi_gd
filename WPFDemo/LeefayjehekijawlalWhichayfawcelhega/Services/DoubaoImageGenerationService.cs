using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using LeefayjehekijawlalWhichayfawcelhega.Models;

namespace LeefayjehekijawlalWhichayfawcelhega.Services;

internal sealed class DoubaoImageGenerationService
{
    private readonly HttpClient _httpClient = new();
    private readonly DoubaoOptions _options;

    public DoubaoImageGenerationService(DoubaoOptions options)
    {
        _options = options;
    }

    public async Task<IReadOnlyList<GeneratedImageResult>> GenerateImagesAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("图片提示词不能为空。", nameof(prompt));
        }

        string apiKey = GetApiKey();
        var request = new ArkImageGenerationRequest
        {
            Model = _options.ImageModelId,
            Prompt = prompt,
            Size = _options.ImageSize,
            Count = _options.CandidateImageCount,
        };

        using var httpRequestMessage = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"{_options.Endpoint.TrimEnd('/')}/images/generations"))
        {
            Content = JsonContent.Create(request),
        };

        httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage, cancellationToken);
        response.EnsureSuccessStatusCode();

        ArkImageGenerationResponse imageGenerationResponse = await response.Content.ReadFromJsonAsync<ArkImageGenerationResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("图片生成接口没有返回有效数据。");

        if (imageGenerationResponse.Data.Count == 0)
        {
            throw new InvalidOperationException("图片生成接口未返回候选图片。");
        }

        List<GeneratedImageResult> generatedImages = [];
        foreach (ArkImageGenerationResponseItem item in imageGenerationResponse.Data)
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[] imageContent;
            string fileExtension;

            if (!string.IsNullOrWhiteSpace(item.Base64Content))
            {
                imageContent = Convert.FromBase64String(item.Base64Content);
                fileExtension = ".png";
            }
            else if (!string.IsNullOrWhiteSpace(item.Url))
            {
                using HttpResponseMessage imageResponse = await _httpClient.GetAsync(item.Url, cancellationToken);
                imageResponse.EnsureSuccessStatusCode();
                imageContent = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
                fileExtension = GetFileExtension(imageResponse.Content.Headers.ContentType?.MediaType, item.Url);
            }
            else
            {
                continue;
            }

            generatedImages.Add(new GeneratedImageResult
            {
                Index = generatedImages.Count + 1,
                Content = imageContent,
                FileExtension = fileExtension,
                SourceUrl = item.Url,
            });
        }

        if (generatedImages.Count == 0)
        {
            throw new InvalidOperationException("图片生成接口返回的数据中不包含可下载的图片。");
        }

        return generatedImages;
    }

    private string GetApiKey()
    {
        string? apiKey = Environment.GetEnvironmentVariable(_options.ApiKeyEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException($"环境变量 {_options.ApiKeyEnvironmentVariableName} 未设置。请先配置豆包 API Key。");
        }

        return apiKey.Trim();
    }

    private static string GetFileExtension(string? mediaType, string? imageUrl)
    {
        if (!string.IsNullOrWhiteSpace(mediaType))
        {
            return mediaType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".png",
            };
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri))
        {
            string extension = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                return extension;
            }
        }

        return ".png";
    }
}
