using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;

namespace ImageViewer.Services;

internal sealed class ImageDirectoryService
{
    private static readonly HashSet<string> SupportedExtensions = Configuration.Default.ImageFormatsManager.ImageFormats
        .SelectMany(format => format.FileExtensions)
        .Select(extension => $".{extension}")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly NaturalFileComparer _fileComparer = new();

    public IReadOnlyList<string> SupportedFilePatterns { get; } = SupportedExtensions
        .Select(extension => $"*{extension}")
        .OrderBy(pattern => pattern, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public IReadOnlyList<string> GetImagesInSameDirectory(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Array.Empty<string>();
        }

        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return File.Exists(filePath) && IsSupportedImagePath(filePath)
                ? [filePath]
                : Array.Empty<string>();
        }

        try
        {
            return Directory.EnumerateFiles(directory)
                .Where(IsSupportedImagePath)
                .OrderBy(Path.GetFileName, _fileComparer)
                .ToArray();
        }
        catch (UnauthorizedAccessException)
        {
            return File.Exists(filePath) && IsSupportedImagePath(filePath)
                ? [filePath]
                : Array.Empty<string>();
        }
        catch (IOException)
        {
            return File.Exists(filePath) && IsSupportedImagePath(filePath)
                ? [filePath]
                : Array.Empty<string>();
        }
    }

    public bool IsSupportedImagePath(string filePath)
    {
        return SupportedExtensions.Contains(Path.GetExtension(filePath));
    }

    public string GetFormatName(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "JPEG",
            ".png" => "PNG",
            ".bmp" => "BMP",
            ".gif" => "GIF",
            ".webp" => "WebP",
            ".tif" or ".tiff" => "TIFF",
            var extension when SupportedExtensions.Contains(extension) => extension.TrimStart('.').ToUpperInvariant(),
            _ => "未知格式"
        };
    }
}
