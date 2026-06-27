using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace SimpleWrite.Business.TextEditors.PasteStrategies;

/// <summary>
/// 剪贴板图片数据。
/// </summary>
/// <param name="Bitmap">来自剪贴板的位图，如果来自文件引用则为 null</param>
/// <param name="SourceFilePath">源文件路径，如果来自位图数据则为 null</param>
/// <param name="Extension">文件后缀含点，如 .png</param>
internal sealed record ClipboardImageData(Bitmap? Bitmap, string? SourceFilePath, string Extension) : IDisposable
{
    /// <summary>
    /// 释放位图资源（如果有）。
    /// </summary>
    public void Dispose()
    {
        Bitmap?.Dispose();
    }
}

/// <summary>
/// 剪贴板图片读取辅助类。封装位图数据和文件引用两种来源的检测与读取。
/// </summary>
internal static class ClipboardImageReader
{
    /// <summary>
    /// 尝试从剪贴板读取位图数据格式的图片。
    /// </summary>
    /// <param name="clipboard">剪贴板实例</param>
    /// <returns>图片数据列表，如果剪贴板无位图数据则返回 null</returns>
    public static async Task<IReadOnlyList<ClipboardImageData>?> TryGetBitmapImagesAsync(IClipboard clipboard)
    {
        Bitmap? bitmap = await clipboard.TryGetBitmapAsync().ConfigureAwait(false);

        if (bitmap is null)
        {
            return null;
        }

        // Bitmap.Save 默认输出 PNG 格式
        return [new ClipboardImageData(bitmap, null, ".png")];
    }

    /// <summary>
    /// 尝试从剪贴板读取文件引用格式的图片。
    /// </summary>
    /// <param name="clipboard">剪贴板实例</param>
    /// <returns>图片数据列表，如果剪贴板无文件引用或无图片文件则返回 null</returns>
    public static async Task<IReadOnlyList<ClipboardImageData>?> TryGetFileImagesAsync(IClipboard clipboard)
    {
        IStorageItem[]? items = await clipboard.TryGetFilesAsync().ConfigureAwait(false);

        if (items is null || items.Length == 0)
        {
            return null;
        }

        List<ClipboardImageData>? result = null;
        ReadOnlySpan<string> imageFileExtensions =
                        [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico", ".tiff"];

        foreach (IStorageItem item in items)
        {
            using (item)
            {
                string? localPath = item.TryGetLocalPath();
                if (localPath is null)
                {
                    continue;
                }

                string extension;
                try
                {
                    extension = Path.GetExtension(localPath);
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrEmpty(extension))
                {
                    continue;
                }



                bool isImage = false;
                foreach (string ext in imageFileExtensions)
                {
                    if (string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase))
                    {
                        isImage = true;
                        break;
                    }
                }

                if (!isImage)
                {
                    continue;
                }

                result ??= [];
                result.Add(new ClipboardImageData(null, localPath, extension));
            }
        }

        return result;
    }
}
