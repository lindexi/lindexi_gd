namespace PptxGenerator.Rendering.Materials;

/// <summary>
/// 图片素材。
/// </summary>
/// <param name="FilePath">图片文件路径。</param>
public record ImageSlideMlMaterial(SlideMlFilePath FilePath) : ISlideMlMaterial
{
}
