using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace SnapkeboyearheNarjairfiru;

internal sealed record SnapshotRecord(
    string Key,
    string DisplayKey,
    string DisplayName,
    DateTimeOffset CapturedAt,
    string ImagePath,
    string AnalysisPath,
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
            AnalysisPath,
            AnalysisText,
            LoadPreviewImage(ImagePath));
    }

    public SnapshotAnalysisContext ToAnalysisContext()
    {
        return new SnapshotAnalysisContext(CapturedAt, DisplayName, AnalysisText);
    }

    public static string CreateBaseFileName(DateTimeOffset capturedAt, string displayKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayKey);
        return $"{capturedAt:yyyyMMdd_HHmmss_fff}_{displayKey}";
    }

    public string CreateXmlContent()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(DisplayKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(DisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(AnalysisText);

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("snapshot",
                new XAttribute("key", Key),
                new XAttribute("capturedAt", CapturedAt.ToString("O", CultureInfo.InvariantCulture)),
                new XElement("display",
                    new XAttribute("key", DisplayKey),
                    new XAttribute("name", DisplayName)),
                new XElement("image",
                    new XAttribute("fileName", Path.GetFileName(ImagePath))),
                new XElement("analysis", AnalysisText)));

        return document.ToString();
    }

    public static bool TryLoad(string analysisPath, out SnapshotRecord? record)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisPath);

        return Path.GetExtension(analysisPath).ToLowerInvariant() switch
        {
            ".xml" => TryLoadFromXml(analysisPath, out record),
            ".txt" => TryLoadFromLegacyText(analysisPath, out record),
            _ => TrySetNull(out record)
        };
    }

    private static bool TryLoadFromXml(string analysisPath, out SnapshotRecord? record)
    {
        record = null;
        var analysisFile = new FileInfo(analysisPath);

        if (!analysisFile.Exists)
        {
            return false;
        }

        var document = XDocument.Load(analysisPath, LoadOptions.None);
        var root = document.Element("snapshot");
        if (root is null)
        {
            return false;
        }

        var key = root.Attribute("key")?.Value;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = Path.GetFileNameWithoutExtension(analysisPath);
        }

        var displayElement = root.Element("display");
        var displayKey = displayElement?.Attribute("key")?.Value;
        if (string.IsNullOrWhiteSpace(displayKey))
        {
            displayKey = ExtractDisplayKey(key);
        }

        var displayName = displayElement?.Attribute("name")?.Value;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = key;
        }

        DateTimeOffset capturedAt = analysisFile.CreationTimeUtc;
        var capturedAtAttribute = root.Attribute("capturedAt")?.Value;
        if (!string.IsNullOrWhiteSpace(capturedAtAttribute)
            && DateTimeOffset.TryParse(capturedAtAttribute, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedCapturedAt))
        {
            capturedAt = parsedCapturedAt;
        }

        var imageFileName = root.Element("image")?.Attribute("fileName")?.Value;
        var imagePath = string.IsNullOrWhiteSpace(imageFileName)
            ? Path.ChangeExtension(analysisPath, ".png")
            : Path.Combine(analysisFile.DirectoryName ?? string.Empty, imageFileName);

        var analysisText = root.Element("analysis")?.Value.Trim() ?? string.Empty;

        record = new SnapshotRecord(
            key,
            displayKey,
            displayName,
            capturedAt,
            imagePath,
            analysisPath,
            analysisText);
        return true;
    }

    private static bool TryLoadFromLegacyText(string analysisPath, out SnapshotRecord? record)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisPath);

        record = null;
        var textFile = new FileInfo(analysisPath);
        var imagePath = Path.ChangeExtension(analysisPath, ".png");

        if (!textFile.Exists)
        {
            return false;
        }

        var lines = File.ReadAllLines(analysisPath);
        var key = Path.GetFileNameWithoutExtension(analysisPath);
        var displayKey = ExtractDisplayKey(key);
        var displayName = key;
        DateTimeOffset capturedAt = textFile.CreationTimeUtc;
        var analysisText = File.ReadAllText(analysisPath).Trim();

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
            analysisPath,
            analysisText);
        return true;
    }

    private static bool TrySetNull(out SnapshotRecord? record)
    {
        record = null;
        return false;
    }

    private static string ExtractDisplayKey(string key)
    {
        var separatorIndex = key.LastIndexOf('_');
        return separatorIndex >= 0 && separatorIndex < key.Length - 1
            ? key[(separatorIndex + 1)..]
            : key;
    }

    private static BitmapImage? LoadPreviewImage(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            return null;
        }

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

internal sealed record SnapshotAnalysisContext(
    DateTimeOffset CapturedAt,
    string DisplayName,
    string AnalysisText);

public sealed class SnapshotListItem
(
    string key,
    string displayName,
    DateTimeOffset capturedAt,
    string imagePath,
    string analysisPath,
    string analysisText,
    BitmapImage? image
)
{
    public string Key { get; } = key;
    public string DisplayName { get; } = displayName;
    public DateTimeOffset CapturedAt { get; } = capturedAt;
    public string CapturedAtText { get; } = capturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
    public string ImagePath { get; } = imagePath;
    public string AnalysisPath { get; } = analysisPath;
    public string AnalysisText { get; } = analysisText;
    public BitmapImage? Image { get; } = image;
}