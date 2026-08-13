using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using AgentLib.Model;

namespace CodingChatRoom.AvaloniaShell.Services;

internal static class TemporaryImageViewer
{
    private static readonly IReadOnlyDictionary<string, string> ImageFileExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/gif"] = ".gif",
            ["image/webp"] = ".webp",
            ["image/bmp"] = ".bmp",
        };

    internal static void Open(CopilotChatImageItem image)
    {
        ArgumentNullException.ThrowIfNull(image);

        string filePath = SaveToTemporaryFile(image);
        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true,
        });
    }

    internal static string SaveToTemporaryFile(CopilotChatImageItem image)
    {
        ArgumentNullException.ThrowIfNull(image);

        string directoryPath = Path.Join(Path.GetTempPath(), "CodingChatRoom", "ImagePreviews");
        Directory.CreateDirectory(directoryPath);

        string extension = ImageFileExtensions.GetValueOrDefault(image.MimeType, ".img");
        string filePath = Path.Join(directoryPath, $"{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(filePath, image.Data.ToArray());
        return filePath;
    }
}
