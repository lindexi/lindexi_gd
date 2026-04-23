using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;

namespace SnapkeboyearheNarjairfiru;

internal sealed record SnapshotRecord(
    string Key,
    string DisplayKey,
    string DisplayName,
    DateTimeOffset CapturedAt,
    string ImagePath,
    string TextPath,
    string AnalysisText)
{
    private const string DisplayPrefix = "屏幕：";
    private const string TimePrefix = "时间：";
    private const string AnalysisPrefix = "解读：";

    public SnapshotListItem ToListItem()
    {
        return new SnapshotListItem(
            Key,
            DisplayName,
            CapturedAt,
            ImagePath,
            TextPath,
            AnalysisText,
            LoadPreviewImage(ImagePath));
    }

    public static string CreateBaseFileName(DateTimeOffset capturedAt, string displayKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayKey);
        return $"{capturedAt:yyyyMMdd_HHmmss_fff}_{displayKey}";
    }

    public static string CreateTextContent(string displayName, DateTimeOffset capturedAt, string analysisText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisText);

        return $"{DisplayPrefix}{displayName}{Environment.NewLine}{TimePrefix}{capturedAt:O}{Environment.NewLine}{AnalysisPrefix}{Environment.NewLine}{analysisText}";
    }

    public static bool TryLoad(string textPath, out SnapshotRecord? record)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(textPath);

        record = null;
        var textFile = new FileInfo(textPath);
        var imagePath = Path.ChangeExtension(textPath, ".png");

        if (!textFile.Exists || !File.Exists(imagePath))
        {
            return false;
        }

        var lines = File.ReadAllLines(textPath);
        var key = Path.GetFileNameWithoutExtension(textPath);
        var displayKey = ExtractDisplayKey(key);
        var displayName = key;
        DateTimeOffset capturedAt = textFile.CreationTimeUtc;
        var analysisText = File.ReadAllText(textPath).Trim();

        if (lines.Length >= 4
            && lines[0].StartsWith(DisplayPrefix, StringComparison.Ordinal)
            && lines[1].StartsWith(TimePrefix, StringComparison.Ordinal)
            && string.Equals(lines[2], AnalysisPrefix, StringComparison.Ordinal)
            && DateTimeOffset.TryParse(lines[1][TimePrefix.Length..], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedTime))
        {
            displayName = lines[0][DisplayPrefix.Length..].Trim();
            capturedAt = parsedTime;
            analysisText = string.Join(Environment.NewLine, lines[3..]).Trim();
        }

        record = new SnapshotRecord(
            key,
            displayKey,
            displayName,
            capturedAt,
            imagePath,
            textPath,
            analysisText);
        return true;
    }

    private static string ExtractDisplayKey(string key)
    {
        var separatorIndex = key.LastIndexOf('_');
        return separatorIndex >= 0 && separatorIndex < key.Length - 1
            ? key[(separatorIndex + 1)..]
            : key;
    }

    private static BitmapImage LoadPreviewImage(string imagePath)
    {
        var bitmap = new BitmapImage();
        using var stream = File.OpenRead(imagePath);
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}

public sealed class SnapshotListItem
(
    string key,
    string displayName,
    DateTimeOffset capturedAt,
    string imagePath,
    string textPath,
    string analysisText,
    BitmapImage image
)
{
    public string Key { get; } = key;
    public string DisplayName { get; } = displayName;
    public DateTimeOffset CapturedAt { get; } = capturedAt;
    public string CapturedAtText { get; } = capturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
    public string ImagePath { get; } = imagePath;
    public string TextPath { get; } = textPath;
    public string AnalysisText { get; } = analysisText;
    public BitmapImage Image { get; } = image;
}