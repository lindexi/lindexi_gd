namespace PptxGenerator.Rendering.Materials;

/// <summary>
/// 图片素材
/// </summary>
/// <param name="File"></param>
public record ImageSlideMlMaterial(FileInfo File) : ISlideMlMaterial
{
}