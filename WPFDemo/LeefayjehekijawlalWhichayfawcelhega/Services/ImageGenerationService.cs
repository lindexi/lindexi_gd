using System.ClientModel;

using LeefayjehekijawlalWhichayfawcelhega.Models;

using OpenAI;
using OpenAI.Images;

namespace LeefayjehekijawlalWhichayfawcelhega.Services;

internal sealed class ImageGenerationService
{
    private readonly AiProviderOptions _options;

    public ImageGenerationService(AiProviderOptions options)
    {
        _options = options;
    }

    public async Task<IReadOnlyList<GeneratedImageResult>> GenerateImagesAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("图片提示词不能为空。", nameof(prompt));
        }

        if (string.IsNullOrWhiteSpace(_options.ImageModelId))
        {
            throw new InvalidOperationException("请先在界面配置图片模型。");
        }

        ImageClient imageClient = new(
            _options.ImageModelId.Trim(),
            new ApiKeyCredential(GetApiKey()),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(GetEndpoint()),
            });

        ClientResult<GeneratedImageCollection> result = await imageClient.GenerateImagesAsync(
            prompt,
            _options.CandidateImageCount,
            new ImageGenerationOptions
            {
                ResponseFormat = GeneratedImageFormat.Bytes,
            },
            cancellationToken);

        List<GeneratedImageResult> generatedImages = [];
        foreach (GeneratedImage image in result.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BinaryData? imageBytes = image.ImageBytes;
            if (imageBytes is null)
            {
                continue;
            }

            generatedImages.Add(new GeneratedImageResult
            {
                Index = generatedImages.Count + 1,
                Content = imageBytes.ToArray(),
                FileExtension = ".png",
                SourceUrl = image.ImageUri?.ToString(),
            });
        }

        if (generatedImages.Count == 0)
        {
            throw new InvalidOperationException("图片模型没有返回可用图片。");
        }

        return generatedImages;
    }

    private string GetApiKey()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("请先在界面配置 API Key。");
        }

        return _options.ApiKey.Trim();
    }

    private string GetEndpoint()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException("请先在界面配置模型服务地址。");
        }

        return _options.Endpoint.Trim();
    }
}
