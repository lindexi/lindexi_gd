namespace ImageViewer.Models;

internal sealed record ImageFileInfo(
    string FilePath,
    string FileName,
    long FileSizeBytes,
    int PixelWidth,
    int PixelHeight,
    string Format);
