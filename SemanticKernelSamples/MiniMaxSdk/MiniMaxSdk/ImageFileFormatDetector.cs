namespace MiniMaxSdk;

internal static class ImageFileFormatDetector
{
    public static string GetFileExtension(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);

        return imageBytes.AsSpan() switch
        {
            [0x89, 0x50, 0x4E, 0x47, ..] => ".png",
            [0xFF, 0xD8, 0xFF, ..] => ".jpg",
            [0x47, 0x49, 0x46, 0x38, ..] => ".gif",
            [0x52, 0x49, 0x46, 0x46, ..] when imageBytes.Length >= 12 && imageBytes[8] == 0x57 && imageBytes[9] == 0x45 && imageBytes[10] == 0x42 && imageBytes[11] == 0x50 => ".webp",
            _ => ".bin"
        };
    }

    public static string GetFileExtensionFromUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return ".bin";
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return ".bin";
        }

        var extension = Path.GetExtension(uri.AbsolutePath);
        return string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
    }
}