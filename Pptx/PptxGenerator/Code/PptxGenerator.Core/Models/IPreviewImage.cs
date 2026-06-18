using System.IO;

namespace PptxGenerator;

/// <summary>
/// 预览图片抽象接口，解耦 UI 框架特定的 Bitmap 类型。
/// 各 UI 框架（WPF、Avalonia）通过实现此接口来封装各自的图片类型。
/// </summary>
public interface IPreviewImage
{
    /// <summary>
    /// 将图片保存到指定文件路径。
    /// </summary>
    /// <param name="filePath">目标文件路径。</param>
    void Save(string filePath);

    /// <summary>
    /// 将图片以 PNG 格式写入指定流。
    /// </summary>
    /// <param name="stream">目标流。</param>
    void Save(Stream stream);

    /// <summary>
    /// 获取图片的像素宽度。
    /// </summary>
    int Width { get; }

    /// <summary>
    /// 获取图片的像素高度。
    /// </summary>
    int Height { get; }
}