using System.Globalization;
using VideoComposerLib;

var ffmpegFile = new FileInfo(@"C:\lindexi\Application\ffmpeg.exe");
var testFolder = new DirectoryInfo(Path.Join(AppContext.BaseDirectory, "TestFile"));
var outputFile = new FileInfo(Path.Join(AppContext.BaseDirectory, $"video-composer-test-{DateTime.Now:yyyyMMdd-HHmmss}.mp4"));

ArgumentNullException.ThrowIfNull(ffmpegFile);
ArgumentNullException.ThrowIfNull(testFolder);

if (!ffmpegFile.Exists)
{
    throw new FileNotFoundException("FFmpeg 可执行文件不存在。", ffmpegFile.FullName);
}

if (!testFolder.Exists)
{
    throw new DirectoryNotFoundException($"测试目录不存在：{testFolder.FullName}");
}

var segments = BuildVideoSegments(testFolder);
Console.WriteLine($"已加载 {segments.Count} 个视频分段，开始合成。");

await using var videoComposer = new FFmpegVideoComposer(
    ffmpegFile,
    logHandler: static (level, message) => Console.WriteLine($"[{level}] {message}"));

var success = await videoComposer.ComposeAsync(segments, outputFile);
if (!success || !outputFile.Exists)
{
    throw new InvalidOperationException("视频合成失败。outputFile 未生成。");
}

Console.WriteLine($"视频生成成功：{outputFile.FullName}");

static IReadOnlyList<VideoSegment> BuildVideoSegments(DirectoryInfo testFolder)
{
    var imageFiles = testFolder.GetFiles("*.png", SearchOption.TopDirectoryOnly)
        .OrderBy(static file => file.Name, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(GetFileNumber, static file => file);

    var slideDirectories = testFolder.GetDirectories("slide_*")
        .OrderBy(static directory => directory.Name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (slideDirectories.Length == 0)
    {
        throw new InvalidOperationException($"未找到任何分段目录：{testFolder.FullName}");
    }

    var segments = new List<VideoSegment>(slideDirectories.Length);
    foreach (var slideDirectory in slideDirectories)
    {
        var slideNumber = GetDirectoryNumber(slideDirectory);
        if (!imageFiles.TryGetValue(slideNumber, out var imageFile))
        {
            throw new InvalidOperationException($"分段目录 {slideDirectory.Name} 缺少对应图片，期望编号：{slideNumber:000}");
        }

        var audioFiles = slideDirectory.GetFiles("audio_*.mp3", SearchOption.TopDirectoryOnly)
            .OrderBy(static file => file.Name, StringComparer.OrdinalIgnoreCase)
            .Cast<FileInfo>()
            .ToArray();

        if (audioFiles.Length == 0)
        {
            throw new InvalidOperationException($"分段目录 {slideDirectory.Name} 不包含任何音频文件。");
        }

        segments.Add(new VideoSegment(imageFile, audioFiles));
    }

    return segments;
}

static int GetDirectoryNumber(DirectoryInfo directory)
{
    var parts = directory.Name.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length == 0 || !int.TryParse(parts[^1], NumberStyles.None, CultureInfo.InvariantCulture, out var number))
    {
        throw new InvalidOperationException($"无法从目录名解析编号：{directory.Name}");
    }

    return number;
}

static int GetFileNumber(FileInfo file)
{
    if (!int.TryParse(Path.GetFileNameWithoutExtension(file.Name), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
    {
        throw new InvalidOperationException($"无法从图片文件名解析编号：{file.Name}");
    }

    return number;
}
