using System.IO;
using Avalonia.Media.Imaging;

namespace PptxGenerator;

/// <summary>
/// Avalonia 实现的 <see cref="IPreviewImage"/>，包装 <see cref="Bitmap"/>。
/// </summary>
public sealed class AvaloniaPreviewImage : IPreviewImage
{
    private readonly Bitmap _bitmap;

    /// <summary>
    /// 初始化 <see cref="AvaloniaPreviewImage"/> 的新实例。
    /// </summary>
    /// <param name="bitmap">Avalonia 位图。</param>
    public AvaloniaPreviewImage(Bitmap bitmap)
    {
        _bitmap = bitmap ?? throw new System.ArgumentNullException(nameof(bitmap));
    }

    /// <summary>
    /// 获取内部 Avalonia <see cref="Bitmap"/>。
    /// </summary>
    public Bitmap Source => _bitmap;

    /// <inheritdoc />
    public int Width => _bitmap.PixelSize.Width;

    /// <inheritdoc />
    public int Height => _bitmap.PixelSize.Height;

    /// <inheritdoc />
    public void Save(string filePath)
    {
        _bitmap.Save(filePath);
    }

    /// <inheritdoc />
    public void Save(Stream stream)
    {
        _bitmap.Save(stream);
    }
}