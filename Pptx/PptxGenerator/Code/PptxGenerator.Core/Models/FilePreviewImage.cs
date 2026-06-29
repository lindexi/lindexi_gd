namespace PptxGenerator.Models;

/// <summary>
/// 基于文件路径的 <see cref="IPreviewImage"/> 实现，包装本地图片文件。
/// 适用于无需重新渲染、直接复用已有图片的场景（如文件导入、模板缩略图等）。
/// </summary>
public sealed class FilePreviewImage : IPreviewImage
{
    private readonly FileInfo _imageFile;

    /// <summary>
    /// 获取源图片文件的完整路径。
    /// </summary>
    public string SourcePath => _imageFile.FullName;

    /// <summary>
    /// 初始化 <see cref="FilePreviewImage"/> 的新实例。
    /// </summary>
    /// <param name="imageFile">图片文件信息。</param>
    /// <exception cref="ArgumentNullException"><paramref name="imageFile"/> 为 <see langword="null"/>。</exception>
    public FilePreviewImage(FileInfo imageFile)
    {
        ArgumentNullException.ThrowIfNull(imageFile);
        _imageFile = imageFile;
    }

    /// <inheritdoc />
    public void Save(string filePath)
    {
        _imageFile.CopyTo(filePath, overwrite: true);
    }

    /// <inheritdoc />
    public void Save(Stream stream)
    {
        using var fileStream = _imageFile.OpenRead();
        fileStream.CopyTo(stream);
    }
}
