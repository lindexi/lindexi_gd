using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Input.Platform;

namespace SimpleWrite.Business.TextEditors.PasteStrategies;

/// <summary>
/// Markdown 文档的粘贴策略。拥有完整粘贴能力，支持图片粘贴（保存文件并插入引用）和纯文本粘贴。
/// </summary>
internal sealed class MarkdownPasteStrategy : IPasteStrategy
{
    private readonly ImagePathTemplate _imagePathTemplate;

    /// <summary>
    /// 初始化 <see cref="MarkdownPasteStrategy"/> 的新实例。
    /// </summary>
    /// <param name="pathTemplate">图片路径模板，默认使用 <see cref="ImagePathTemplate.DefaultPathTemplate"/></param>
    public MarkdownPasteStrategy(string? pathTemplate = null)
    {
        _imagePathTemplate = new ImagePathTemplate(pathTemplate);
    }

    /// <summary>
    /// 执行 Markdown 粘贴操作。优先处理图片粘贴，无图片时回退到纯文本粘贴。
    /// 文档未保存到本地时返回 false，由调用方回退到默认粘贴行为。
    /// </summary>
    /// <param name="context">粘贴上下文</param>
    /// <returns>true 表示已处理；false 表示未处理，调用方应回退到默认行为</returns>
    public async Task<bool> PasteAsync(PasteContext context)
    {
        if (context.DocumentFile is null)
        {
            return false;
        }
        IReadOnlyList<ClipboardImageData>? images =
            await ClipboardImageReader.TryGetBitmapImagesAsync(context.Clipboard).ConfigureAwait(false);

        if (images is null || images.Count == 0)
        {
            images = await ClipboardImageReader.TryGetFileImagesAsync(context.Clipboard).ConfigureAwait(false);
        }

        if (images is not null && images.Count > 0)
        {
            await PasteImagesAsync(context, context.DocumentFile, images).ConfigureAwait(false);
            return true;
        }

        // 无图片：读取纯文本
        string? text = await context.Clipboard.TryGetTextAsync().ConfigureAwait(false);
        if (!string.IsNullOrEmpty(text))
        {
            context.InsertText(text);
        }

        // 即使文本为空也返回 true，表示策略已处理，无需回退
        return true;
    }

    private async Task PasteImagesAsync(PasteContext context, FileInfo documentFile,
        IReadOnlyList<ClipboardImageData> images)
    {
        string documentName = Path.GetFileNameWithoutExtension(documentFile.Name);
        var documentDirectory = documentFile.Directory;

        var markdownLines = new List<string>(images.Count);

        ArgumentNullException.ThrowIfNull(documentDirectory);

        for (int i = 0; i < images.Count; i++)
        {
            ClipboardImageData image = images[i];

            string relativePath = _imagePathTemplate.RenderRelativePath(documentDirectory, documentName, image.Extension);

            string relativeDirectory = Path.GetDirectoryName(relativePath) ?? string.Empty;
            string absoluteDirectory = Path.Join(documentDirectory.FullName, relativeDirectory);

            Directory.CreateDirectory(absoluteDirectory);

            string absolutePath = Path.Join(documentDirectory.FullName, relativePath);

            if (image.Bitmap is not null)
            {
                await Task.Run(() => image.Bitmap.Save(absolutePath)).ConfigureAwait(true);
            }
            else if (image.SourceFilePath is not null)
            {
                File.Copy(image.SourceFilePath, absolutePath, overwrite: true);
            }

            string normalizedRelativePath = relativePath.Replace('\\', '/');
            markdownLines.Add($"![]({normalizedRelativePath})");
        }

        // 释放位图资源
        foreach (ClipboardImageData image in images)
        {
            image.Dispose();
        }

        string markdownText = string.Join("\n", markdownLines);
        context.InsertText(markdownText);
    }
}