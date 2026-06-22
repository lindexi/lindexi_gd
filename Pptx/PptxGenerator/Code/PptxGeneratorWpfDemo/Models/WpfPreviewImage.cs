using System.IO;
using System.Windows.Media.Imaging;
using PptxGenerator.Models;

namespace PptxGenerator;

/// <summary>
/// WPF 实现的 <see cref="IPreviewImage"/>，包装 <see cref="BitmapSource"/>。
/// </summary>
public sealed class WpfPreviewImage : IPreviewImage
{
    private readonly BitmapSource _bitmapSource;

    /// <summary>
    /// 初始化 <see cref="WpfPreviewImage"/> 的新实例。
    /// </summary>
    /// <param name="bitmapSource">WPF 位图源。</param>
    public WpfPreviewImage(BitmapSource bitmapSource)
    {
        _bitmapSource = bitmapSource ?? throw new System.ArgumentNullException(nameof(bitmapSource));
    }

    /// <summary>
    /// 获取内部 WPF <see cref="BitmapSource"/>。
    /// </summary>
    public BitmapSource Source => _bitmapSource;

    /// <inheritdoc />
    public void Save(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_bitmapSource));
        encoder.Save(fileStream);
    }

    /// <inheritdoc />
    public void Save(Stream stream)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_bitmapSource));
        encoder.Save(stream);
    }
}
