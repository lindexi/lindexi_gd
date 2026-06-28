using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace SimpleWrite.Business.TextEditors.PasteStrategies;

/// <summary>
/// 图片路径模板渲染辅助类。负责占位符替换和编号计算。
/// </summary>
internal sealed class ImagePathTemplate
{
    /// <summary>
    /// 默认路径模板。
    /// </summary>
    public const string DefaultPathTemplate = "image/{FileName}/{FileName}{Index}.{Extension}";

    private static readonly SearchValues<char> _invalidCharSet =
        SearchValues.Create([.. Path.GetInvalidPathChars(), .. Path.GetInvalidFileNameChars()]);

    private readonly string _template;

    /// <summary>
    /// 初始化 <see cref="ImagePathTemplate"/> 的新实例。
    /// </summary>
    /// <param name="template">路径模板，默认使用 <see cref="DefaultPathTemplate"/></param>
    public ImagePathTemplate(string? template = null)
    {
        _template = string.IsNullOrWhiteSpace(template) ? DefaultPathTemplate : template;
    }

    /// <summary>
    /// 根据文件名和后缀渲染相对路径。序号由内部扫描目标目录已有文件自动计算。
    /// </summary>
    /// <param name="documentDirectoryInfo">文档所在目录的绝对路径</param>
    /// <param name="fileName">文件名（不含后缀），将进行非法字符替换和长度截断</param>
    /// <param name="extension">后缀（可含点，如 .png，也可不含点，如 png）</param>
    /// <returns>渲染后的相对路径，使用正斜杠分隔</returns>
    public string RenderRelativePath(DirectoryInfo documentDirectoryInfo, string fileName, string extension)
    {
        var documentDirectory = documentDirectoryInfo.FullName;
        string cleanedFileName = CleanFileName(fileName);

        ReadOnlySpan<char> extSpan = extension.AsSpan();
        if (!extSpan.IsEmpty && extSpan[0] == '.')
        {
            extSpan = extSpan[1..];
        }

        string extensionWithoutDot = extSpan.ToString();

        // 先用占位渲染出模板路径，提取目录部分用于扫描已有文件
        string templatedPath = RenderTemplate(_template, cleanedFileName, "0", extensionWithoutDot);

        string relativeDirectory = Path.GetDirectoryName(templatedPath) ?? string.Empty;
        string absoluteDirectory = Path.Join(documentDirectory, relativeDirectory);

        int index = GetNextIndex(absoluteDirectory, cleanedFileName);

        string path = RenderTemplate(_template, cleanedFileName, index.ToString(), extensionWithoutDot);

        return NormalizeSlashes(path);
    }

    /// <summary>
    /// 扫描目录中已有的同名图片文件，返回下一个可用序号（最大编号 + 1）。
    /// </summary>
    /// <param name="directoryPath">要扫描的目录路径</param>
    /// <param name="fileName">文件名（不含后缀），需与渲染时一致</param>
    /// <returns>下一个可用序号；如果目录不存在或无匹配文件则返回 0</returns>
    public int GetNextIndex(string directoryPath, string fileName)
    {
        string cleanedFileName = CleanFileName(fileName);
        ReadOnlySpan<char> cleanedFileNameSpan = cleanedFileName.AsSpan();

        if (!Directory.Exists(directoryPath))
        {
            return 0;
        }

        int maxIndex = -1;

        foreach (string file in Directory.EnumerateFiles(directoryPath))
        {
            ReadOnlySpan<char> nameWithoutExtension = Path.GetFileNameWithoutExtension(file.AsSpan());

            // 文件名格式为 {fileName}{Index}，提取 Index 部分
            if (nameWithoutExtension.Length <= cleanedFileNameSpan.Length ||
                !nameWithoutExtension.StartsWith(cleanedFileNameSpan, StringComparison.Ordinal))
            {
                continue;
            }

            ReadOnlySpan<char> indexPart = nameWithoutExtension[cleanedFileNameSpan.Length..];

            if (int.TryParse(indexPart, out int currentIndex) && currentIndex > maxIndex)
            {
                maxIndex = currentIndex;
            }
        }

        return maxIndex + 1;
    }

    private static string CleanFileName(string fileName)
    {
        // 原地替换非法字符为下划线，避免多次 string.Replace 分配
        Span<char> buffer = fileName.Length <= 256
            ? stackalloc char[fileName.Length]
            : new char[fileName.Length];

        fileName.AsSpan().CopyTo(buffer);

        for (int i = 0; i < buffer.Length; i++)
        {
            if (_invalidCharSet.Contains(buffer[i]))
            {
                buffer[i] = '_';
            }
        }

        // 截断保留前 50 个字符
        return buffer.Length > 50
            ? new string(buffer[..50])
            : new string(buffer);
    }

    /// <summary>
    /// 使用 Span 单次遍历模板进行占位符替换，避免多次 string.Replace 的中间分配。
    /// </summary>
    private static string RenderTemplate(string template, string fileName, string index, string extension)
    {
        ReadOnlySpan<char> templateSpan = template.AsSpan();
        ReadOnlySpan<char> fileNameSpan = fileName.AsSpan();
        ReadOnlySpan<char> indexSpan = index.AsSpan();
        ReadOnlySpan<char> extensionSpan = extension.AsSpan();

        // 第一次遍历：计算结果总长度
        int totalLength = ComputeRenderedLength(templateSpan, fileNameSpan, indexSpan, extensionSpan);

        // 第二次遍历：使用 string.Create 填充
        return string.Create(totalLength, (template, fileName, index, extension), static (span, state) =>
        {
            ReadOnlySpan<char> tpl = state.template.AsSpan();
            ReadOnlySpan<char> fn = state.fileName.AsSpan();
            ReadOnlySpan<char> idx = state.index.AsSpan();
            ReadOnlySpan<char> ext = state.extension.AsSpan();

            int written = 0;

            while (!tpl.IsEmpty)
            {
                if (tpl.StartsWith("{FileName}", StringComparison.Ordinal))
                {
                    fn.CopyTo(span[written..]);
                    written += fn.Length;
                    tpl = tpl["{FileName}".Length..];
                }
                else if (tpl.StartsWith("{Index}", StringComparison.Ordinal))
                {
                    idx.CopyTo(span[written..]);
                    written += idx.Length;
                    tpl = tpl["{Index}".Length..];
                }
                else if (tpl.StartsWith("{Extension}", StringComparison.Ordinal))
                {
                    ext.CopyTo(span[written..]);
                    written += ext.Length;
                    tpl = tpl["{Extension}".Length..];
                }
                else
                {
                    span[written++] = tpl[0];
                    tpl = tpl[1..];
                }
            }
        });
    }

    private static int ComputeRenderedLength(
        ReadOnlySpan<char> template, ReadOnlySpan<char> fileName, ReadOnlySpan<char> index, ReadOnlySpan<char> extension)
    {
        int length = 0;

        while (!template.IsEmpty)
        {
            if (template.StartsWith("{FileName}", StringComparison.Ordinal))
            {
                length += fileName.Length;
                template = template["{FileName}".Length..];
            }
            else if (template.StartsWith("{Index}", StringComparison.Ordinal))
            {
                length += index.Length;
                template = template["{Index}".Length..];
            }
            else if (template.StartsWith("{Extension}", StringComparison.Ordinal))
            {
                length += extension.Length;
                template = template["{Extension}".Length..];
            }
            else
            {
                length++;
                template = template[1..];
            }
        }

        return length;
    }

    /// <summary>
    /// 使用 string.Create 原地替换反斜杠为正斜杠，避免 string.Replace 分配。
    /// </summary>
    private static string NormalizeSlashes(string path)
    {
        if (!path.Contains('\\'))
        {
            return path;
        }

        return string.Create(path.Length, path, static (span, source) =>
        {
            ReadOnlySpan<char> src = source.AsSpan();
            for (int i = 0; i < src.Length; i++)
            {
                span[i] = src[i] == '\\' ? '/' : src[i];
            }
        });
    }
}
