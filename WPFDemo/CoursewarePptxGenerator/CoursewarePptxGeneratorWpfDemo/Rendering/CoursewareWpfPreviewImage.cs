using System.IO;
using System.Windows.Media.Imaging;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Rendering;

/// <summary>
/// WPF 实现的 <see cref="IPreviewImage" />，包装 <see cref="BitmapSource" />。
/// </summary>
internal sealed class CoursewareWpfPreviewImage : IPreviewImage
{
    private readonly BitmapSource _bitmapSource;

    /// <summary>
    /// 初始化 <see cref="CoursewareWpfPreviewImage" /> 的新实例。
    /// </summary>
    /// <param name="bitmapSource">WPF 位图源。</param>
    public CoursewareWpfPreviewImage(BitmapSource bitmapSource)
    {
        ArgumentNullException.ThrowIfNull(bitmapSource);
        _bitmapSource = bitmapSource;
    }

    /// <summary>
    /// 获取内部 WPF <see cref="BitmapSource" />。
    /// </summary>
    public BitmapSource Source => _bitmapSource;

    /// <inheritdoc />
    public void Save(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        Save(fileStream);
    }

    /// <inheritdoc />
    public void Save(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_bitmapSource));
        encoder.Save(stream);
    }
}
