namespace MiniMaxSdk;

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