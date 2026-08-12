using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 表示聊天输入区中一张待发送的图片附件。
/// </summary>
public sealed class ImageAttachmentViewModel
{
    private static readonly IReadOnlyDictionary<string, string> SupportedMimeTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".bmp"] = "image/bmp",
        };

    private ImageAttachmentViewModel(string fileName, string mimeType, BinaryData data)
    {
        FileName = fileName;
        MimeType = mimeType;
        Data = data;
    }

    /// <summary>
    /// 获取图片文件名。
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// 获取图片 MIME 类型。
    /// </summary>
    public string MimeType { get; }

    /// <summary>
    /// 获取图片二进制数据。
    /// </summary>
    public BinaryData Data { get; }

    /// <summary>
    /// 尝试从文件名和图片数据创建附件。
    /// </summary>
    /// <param name="fileName">图片文件名。</param>
    /// <param name="data">图片二进制数据。</param>
    /// <param name="attachment">创建成功的图片附件。</param>
    /// <returns>文件扩展名受支持且数据非空时返回 <see langword="true"/>。</returns>
    public static bool TryCreate(
        string fileName,
        ReadOnlyMemory<byte> data,
        [NotNullWhen(true)] out ImageAttachmentViewModel? attachment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        string extension = Path.GetExtension(fileName);
        if (data.IsEmpty || !SupportedMimeTypes.TryGetValue(extension, out string? mimeType))
        {
            attachment = null;
            return false;
        }

        attachment = new ImageAttachmentViewModel(
            Path.GetFileName(fileName),
            mimeType,
            BinaryData.FromBytes(data));
        return true;
    }
}
