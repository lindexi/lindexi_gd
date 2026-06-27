using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    private static readonly char[] _invalidChars =
        [.. Path.GetInvalidPathChars(), .. Path.GetInvalidFileNameChars()];

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

        if (extension.StartsWith('.'))
        {
            extension = extension[1..];
        }

        // 先用占位渲染出模板路径，提取目录部分用于扫描已有文件
        string templatedPath = _template
            .Replace("{FileName}", cleanedFileName)
            .Replace("{Index}", "0")
            .Replace("{Extension}", extension);

        string relativeDirectory = Path.GetDirectoryName(templatedPath) ?? string.Empty;
        string absoluteDirectory = Path.Join(documentDirectory, relativeDirectory);

        int index = GetNextIndex(absoluteDirectory, cleanedFileName);

        string path = _template
            .Replace("{FileName}", cleanedFileName)
            .Replace("{Index}", index.ToString())
            .Replace("{Extension}", extension);

        return path.Replace('\\', '/');
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

        if (!Directory.Exists(directoryPath))
        {
            return 0;
        }

        int maxIndex = -1;

        foreach (string file in Directory.EnumerateFiles(directoryPath))
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(file);

            // 文件名格式为 {fileName}{Index}，提取 Index 部分
            if (nameWithoutExtension.Length <= cleanedFileName.Length ||
                !nameWithoutExtension.StartsWith(cleanedFileName, StringComparison.Ordinal))
            {
                continue;
            }

            string indexPart = nameWithoutExtension[cleanedFileName.Length..];

            if (int.TryParse(indexPart, out int currentIndex) && currentIndex > maxIndex)
            {
                maxIndex = currentIndex;
            }
        }

        return maxIndex + 1;
    }

    private static string CleanFileName(string fileName)
    {
        // 替换非法字符为下划线
        string cleaned = fileName;
        foreach (char c in _invalidChars)
        {
            cleaned = cleaned.Replace(c, '_');
        }

        // 截断保留前 50 个字符
        if (cleaned.Length > 50)
        {
            cleaned = cleaned[..50];
        }

        return cleaned;
    }
}
