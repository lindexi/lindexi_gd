// See https://aka.ms/new-console-template for more information

using KadefihalldokaiChairwedone;
using KadefihalldokaiChairwedone.CoursewareSpeechGenerators;

using OpenAI;

using System.ClientModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using VideoComposerLib;

using VolcEngineSdk.OpenSpeech;

using CoursewareSpeechGenerator = KadefihalldokaiChairwedone.CoursewareSpeechGenerators.CoursewareSpeechGenerator;

var coursewareJsonFile = @"C:\lindexi\Work\CoursewareMaterialInfo.json";

var ffmpegFile = @"C:\lindexi\Application\ffmpeg.exe";

// 豆包语音-新版控制台-API Key管理，创建或获取 API Key 内容
var openSpeechApiKeyFile = @"C:\lindexi\Work\Key\OpenSpeech API Key.txt";
var openSpeechApiKey = File.ReadAllText(openSpeechApiKeyFile);

// 根据 https://www.volcengine.com/docs/6561/1329505 文档：X-Api-Resource-Id
// 表示调用服务的资源信息 ID，可以用来选择不同的模型版本效果，也决定了计费方式。可选内容：
// seed-tts-2.0：对应计费商品为 “语音合成2.0字符版“
// seed-tts-1.0：对应计费商品为“语音合成1.0字符版”
// seed-tts-1.0-concurr：对应计费商品为“语音合成1.0并发版“
const string resourceId = "seed-tts-2.0";
const string speaker = "zh_female_vv_uranus_bigtts";

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);
var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
    NetworkTimeout = TimeSpan.FromHours(1),
});

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg").AsIChatClient();

var coursewareMaterialInfo = LoadCoursewareMaterialInfo(coursewareJsonFile);

var outputDirectory = new DirectoryInfo(Path.Join(AppContext.BaseDirectory, $"GeneratedCoursewareSpeech_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}"));
outputDirectory.Create();

var authentication = OpenSpeechAuthentication.CreateWithApiKey(openSpeechApiKey, resourceId);
using var httpClient = new HttpClient();
var openSpeechClient = new OpenSpeechClient(httpClient);
await using var ffmpegVideoComposer = new FFmpegVideoComposer(
    new FileInfo(ffmpegFile),
    workingDirectory: outputDirectory,
    logHandler: (level, message) => Console.WriteLine($"[{level}] {message}"));

var generator = new CoursewareSpeechGenerator
{
    WorkingDirectory = new DirectoryInfo(Path.Join(outputDirectory.FullName, "Working")),
    FFmpegVideoComposer = ffmpegVideoComposer,
    OpenSpeechClient = openSpeechClient,
    SpeechSynthesisOptions = new CoursewareSpeechSynthesisOptions(authentication, speaker),
    Logger = new ConsoleLogger(),
};

var outputVideoFile = await generator.GeneratorCoursewareSpeechVideoAsync(coursewareMaterialInfo, chatClient);

Console.WriteLine($"视频文件已生成：{outputVideoFile.FullName}");

static CoursewareMaterialInfo LoadCoursewareMaterialInfo(string coursewareJsonFile)
{
    var savableCoursewareMaterialInfo = JsonSerializer.Deserialize<SavableCoursewareMaterialInfo>(File.ReadAllText(coursewareJsonFile), new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
    }) ?? throw new InvalidOperationException("课件 JSON 反序列化失败，未能读取到课件页面信息。");

    if (savableCoursewareMaterialInfo.SlideMaterialInfoList.Count == 0)
    {
        throw new InvalidOperationException("课件 JSON 中没有任何页面信息。");
    }

    var coursewareSlideMaterialInfoList = savableCoursewareMaterialInfo.SlideMaterialInfoList.Select(t => new CoursewareSlideMaterialInfo(new FileInfo(t.SlideThumbnailFilePath), t.ContentText)).ToList();
    return new CoursewareMaterialInfo(coursewareSlideMaterialInfoList);
}

static string ReadRequiredText(string filePath)
{
    if (!File.Exists(filePath))
    {
        throw new FileNotFoundException($"找不到密钥文件：{filePath}", filePath);
    }

    var text = File.ReadAllText(filePath).Trim();
    if (string.IsNullOrWhiteSpace(text))
    {
        throw new InvalidOperationException($"密钥文件内容为空：{filePath}");
    }

    return text;
}

public record SavableCoursewareSlideMaterialInfo(string SlideThumbnailFilePath, string ContentText);

public record SavableCoursewareMaterialInfo(List<SavableCoursewareSlideMaterialInfo> SlideMaterialInfoList);

class ConsoleLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        Console.WriteLine($"[{logLevel}] {message}");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}