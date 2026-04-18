using System.ClientModel;

using LeefayjehekijawlalWhichayfawcelhega.Models;

using OpenAI;
using OpenAI.Images;

namespace LeefayjehekijawlalWhichayfawcelhega.Services;

internal sealed class AiImageGenerationService
{
    private readonly AiServiceOptions _options;

    public AiImageGenerationService(AiServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public async Task<IReadOnlyList<GeneratedImageResult>> GenerateImagesAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("图片提示词不能为空。", nameof(prompt));
        }

        cancellationToken.ThrowIfCancellationRequested();

        OpenAIClient openAiClient = CreateClient();
        ImageClient imageClient = openAiClient.GetImageClient(_options.ImageModelId);
        ImageGenerationOptions imageGenerationOptions = new()
        {
            ResponseFormat = GeneratedImageFormat.Bytes,
        };

        ClientResult<GeneratedImageCollection> response = await imageClient.GenerateImagesAsync(
            prompt,
            _options.CandidateImageCount,
            imageGenerationOptions,
            cancellationToken);

        GeneratedImageCollection generatedImageCollection = response.Value;
        List<GeneratedImageResult> generatedImages = [];

        foreach (GeneratedImage generatedImage in generatedImageCollection)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BinaryData? imageBytes = generatedImage.ImageBytes;
            if (imageBytes is null)
            {
                continue;
            }

            generatedImages.Add(new GeneratedImageResult
            {
                Index = generatedImages.Count + 1,
                Content = imageBytes.ToArray(),
                FileExtension = ".png",
                SourceUrl = generatedImage.ImageUri?.ToString(),
            });
        }

        if (generatedImages.Count == 0)
        {
            throw new InvalidOperationException("图片生成接口未返回可用图片。请检查模型配置后重试。");
        }

        return generatedImages;
    }

    private OpenAIClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException("服务 Endpoint 不能为空。请先在界面中配置服务地址。");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("API Key 不能为空。请先在界面中输入 API Key。");
        }

        if (string.IsNullOrWhiteSpace(_options.ImageModelId))
        {
            throw new InvalidOperationException("图片模型不能为空。请先在界面中配置图片模型。");
        }

        return new OpenAIClient(
            new ApiKeyCredential(_options.ApiKey.Trim()),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_options.Endpoint.Trim()),
            });
    }
}
