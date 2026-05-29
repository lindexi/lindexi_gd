namespace MiniMaxSdk.Images.Models;

/// <summary>
/// 表示一次 MiniMax 图生图请求。
/// </summary>
/// <param name="Prompt">图像的文本描述，最长 1500 字符。</param>
/// <param name="SubjectReferences">主体参考列表，用于图生图。</param>
/// <param name="Model">模型名称，可选值为 <c>image-01</c> 和 <c>image-01-live</c>。</param>
/// <param name="AspectRatio">图像宽高比，默认由服务端处理，可选值参考 <see cref="MiniMaxImageAspectRatios"/>。</param>
/// <param name="Width">生成图片的宽度（像素），仅当模型为 <c>image-01</c> 时生效。</param>
/// <param name="Height">生成图片的高度（像素），仅当模型为 <c>image-01</c> 时生效。</param>
/// <param name="ResponseFormat">返回图片的形式，可选值为 <c>url</c> 或 <c>base64</c>。</param>
/// <param name="Seed">随机种子，使用相同参数与相同种子可生成内容相近的图片。</param>
/// <param name="Count">单次请求生成的图片数量，取值范围为 1 到 9。</param>
/// <param name="PromptOptimizer">是否开启 Prompt 自动优化。</param>
/// <param name="AigcWatermark">是否在生成的图片中添加水印。</param>
/// <param name="Style">画风设置，仅当模型为 <c>image-01-live</c> 时生效。</param>
/// <remarks>
/// <para>当同时设置 <paramref name="AspectRatio"/> 与 <paramref name="Width"/>、<paramref name="Height"/> 时，服务端优先使用宽高比参数。</para>
/// <para><paramref name="Width"/> 与 <paramref name="Height"/> 需要同时设置，取值范围为 512 到 2048，且必须是 8 的倍数。</para>
/// <para>当 <paramref name="ResponseFormat"/> 为 <c>url</c> 时，返回链接有效期为 24 小时。</para>
/// </remarks>
public sealed record MiniMaxImageToImageGenerationRequest(
    string Prompt,
    IReadOnlyList<MiniMaxImageSubjectReference> SubjectReferences,
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

        ArgumentNullException.ThrowIfNull(SubjectReferences);

        if (SubjectReferences.Count == 0)
        {
            throw new ArgumentException("图生图至少需要提供一个主体参考。", nameof(SubjectReferences));
        }

        foreach (var subjectReference in SubjectReferences)
        {
            ArgumentNullException.ThrowIfNull(subjectReference);
            subjectReference.Validate();
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