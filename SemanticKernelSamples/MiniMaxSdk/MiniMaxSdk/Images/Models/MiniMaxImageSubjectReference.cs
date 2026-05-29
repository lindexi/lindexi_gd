namespace MiniMaxSdk.Images.Models;

/// <summary>
/// 表示 MiniMax 图生图请求中的主体参考图片。
/// </summary>
/// <param name="Type">主体类型，当前仅支持 <c>character</c>（人像）。</param>
/// <param name="ImageFile">参考图文件，支持公网 URL 或 Base64 编码的 Data URL。</param>
/// <remarks>
/// <para>为获得最佳效果，建议上传单人正面照片。</para>
/// <para>图片要求：格式为 JPG、JPEG、PNG，大小小于 10MB。</para>
/// </remarks>
public sealed record MiniMaxImageSubjectReference(string Type, string ImageFile)
{
    /// <summary>
    /// 创建人物主体参考。
    /// </summary>
    /// <param name="imageFile">参考图文件，支持公网 URL 或 Base64 编码的 Data URL。</param>
    /// <returns>人物主体参考对象。</returns>
    public static MiniMaxImageSubjectReference CreateCharacter(string imageFile)
    {
        return new MiniMaxImageSubjectReference(MiniMaxImageSubjectReferenceTypes.Character, imageFile);
    }

    internal void Validate()
    {
        if (!MiniMaxImageSubjectReferenceTypes.IsSupported(Type))
        {
            throw new ArgumentException($"不支持的主体参考类型：{Type}", nameof(Type));
        }

        if (string.IsNullOrWhiteSpace(ImageFile))
        {
            throw new ArgumentException("参考图文件不能为空。", nameof(ImageFile));
        }
    }
}