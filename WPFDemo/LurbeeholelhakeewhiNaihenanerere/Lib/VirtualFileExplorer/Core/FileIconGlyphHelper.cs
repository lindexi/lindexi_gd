namespace VirtualFileExplorer.Core;

internal static class FileIconGlyphHelper
{
    internal const string FolderGlyph = "\uE8B7";
    internal const string FileGlyph = "\uE8A5";
    internal const string ImageGlyph = "\uEB9F";
    internal const string ArchiveGlyph = "\uF012";
    internal const string CodeGlyph = "\uE943";
    internal const string DocumentGlyph = "\uE8A5";

    internal static string GetFileGlyph(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return FileGlyph;
        }

        return extension.ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".svg" or ".webp" => ImageGlyph,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => ArchiveGlyph,
            ".cs" or ".xaml" or ".json" or ".xml" or ".md" or ".txt" or ".log" => CodeGlyph,
            ".doc" or ".docx" or ".pdf" => DocumentGlyph,
            _ => FileGlyph
        };
    }
}
