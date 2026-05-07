using VolcEngineSdk.OpenSpeech;
using VolcEngineSdk.OpenSpeech.Contexts;

var accessTokenFile = @"C:\lindexi\Work\Key\OpenSpeech TTS Access Token.txt";
var appId = "5866932789";

// speaker 发音人： voiceType
var voiceType = "zh_female_vv_uranus_bigtts";

// 使用 context_texts 为语音合成提供辅助信息，实现不同语气说话。
// 当前字符串列表只第一个值有效，因此通过拆分脚本并多次请求来实现带上下文与停顿的整段音频。

// 模型版本： seed-tts-2.0-expressive
const string resourceId = "seed-tts-2.0";
const string model = "seed-tts-2.0-expressive";
const string outputFileName = "context-texts-script.mp3";
const string script = """
[上下文: 吐字清晰，语速适中，用引导学生认读的温和语气]
刚才大家已经完成了课文的初读，现在我们就正式进入字词学习环节，这些词语都选自页面右下角标注的“课前预学单”第1题，大家之前预习的时候有没有都读准呀？[停顿: 1秒]
首先看页面上方的要求：读一读下面的词语，注意读准字音，不会认读的生字词可以圈出来多读几遍。[停顿: 1秒]
现在老师先带着大家逐行认读，大家可以跟着小声读哦：
第一行：腊八粥[停顿: 0.5秒]、吞咽[停顿: 0.5秒]、汤匙[停顿: 0.5秒]、碗盏[停顿: 0.5秒]、搅和[停顿: 1秒]。
这里要提醒大家几个重点读音：首先是翘舌音的字，“粥”“匙”“盏”还有后面“肿胀”的“胀”都是翘舌音，读的时候要把舌尖翘起来，不要读成平舌音哦。[停顿: 1秒]
另外还有几个轻声和多音字要特别记牢：“搅和”的“和”在这里读轻声huo，“钥匙”的“匙”读轻声shi，只有表示舀东西的工具时才读chí，大家可不要搞混啦。[停顿: 1.5秒]
我们接着读第二行：肿胀[停顿: 0.5秒]、熬粥[停顿: 0.5秒]、褐色[停顿: 0.5秒]、染缸[停顿: 0.5秒]、脏水[停顿: 1秒]。这里的“脏”是平舌音，大家读的时候要注意舌尖放平。[停顿: 0.5秒]
第三行：筷子[停顿: 0.5秒]、陈旧[停顿: 0.5秒]、感觉[停顿: 0.5秒]、沸腾[停顿: 0.5秒]、何况[停顿: 1秒]。
第四行：资格[停顿: 0.5秒]、可靠[停顿: 0.5秒]、罢了[停顿: 0.5秒]、要不然[停顿: 0.5秒]、猜想[停顿: 1秒]。
第五行：惊异[停顿: 0.5秒]、总之[停顿: 0.5秒]、解释[停顿: 0.5秒]、浪漫[停顿: 0.5秒]、奈何[停顿: 1.5秒]。
大家都读准了吗？要是还有拿不准的字音，可以再多读几遍巩固一下。[停顿: 1秒]掌握了这些字词，接下来我们就一起来梳理课文围绕腊八粥讲了什么故事吧。
""";

Console.WriteLine("开始调用 OpenSpeech 语音合成...");

var authentication = CreateAuthentication(appId, accessTokenFile, resourceId);
var scriptInput = ParseScript(script);
var outputFilePath = Path.Combine(AppContext.BaseDirectory, outputFileName);

using var httpClient = new HttpClient();
var client = new OpenSpeechClient(httpClient);
using var combinedAudioStream = new MemoryStream();
var totalTextWords = 0;
var totalSentenceCount = 0;

foreach (var (index, segment) in scriptInput.Segments.Select((segment, index) => (index, segment)))
{
    var request = CreateRequest(voiceType, model, segment.Text, scriptInput.ContextText, segment.PauseAfterMilliseconds);
    var options = new SpeechSynthesisRequestOptions(authentication)
    {
        Protocol = SpeechSynthesisProtocol.HttpChunked,
        RequestId = Guid.NewGuid().ToString(),
        UsageTokensToReturn = "text_words"
    };

    var result = await client.SynthesizeAsync(request, options);
    await combinedAudioStream.WriteAsync(result.AudioData);
    totalTextWords += result.Usage?.TextWords ?? 0;
    totalSentenceCount += result.Sentences.Count;

    Console.WriteLine($"分段 {index + 1}: {segment.Text}");
    Console.WriteLine($"分段音频字节数: {result.AudioData.Length}");
    Console.WriteLine($"分段返回句子数: {result.Sentences.Count}");
    Console.WriteLine($"分段计费字符数: {result.Usage?.TextWords?.ToString() ?? "未返回"}");
    Console.WriteLine($"服务端 LogId: {result.LogId ?? "未返回"}");
}

await File.WriteAllBytesAsync(outputFilePath, combinedAudioStream.ToArray());

Console.WriteLine($"音频文件已生成: {outputFilePath}");
Console.WriteLine($"合并音频字节数: {combinedAudioStream.Length}");
Console.WriteLine($"合并返回句子数: {totalSentenceCount}");
Console.WriteLine($"合并计费字符数: {totalTextWords}");

static OpenSpeechAuthentication CreateAuthentication(string appId, string accessTokenFile, string resourceId)
{
    var apiKey = Environment.GetEnvironmentVariable("OPENSPEECH_API_KEY");
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        return OpenSpeechAuthentication.CreateWithApiKey(apiKey.Trim(), resourceId);
    }

    ArgumentException.ThrowIfNullOrWhiteSpace(appId);

    var accessKey = ReadRequiredText(accessTokenFile);
    return OpenSpeechAuthentication.CreateWithLegacyCredentials(appId, accessKey, resourceId);
}

static SpeechSynthesisRequest CreateRequest(string voiceType, string model, string text, string? contextText, int? silenceDuration)
{
    return new SpeechSynthesisRequest
    {
        User = new UserMeta
        {
            Uid = Environment.UserName
        },
        RequestParameters = new SpeechSynthesisRequestParameters
        {
            Text = text,
            Model = model,
            Speaker = voiceType,
            AudioParameters = new SpeechSynthesisAudioParameters
            {
                Format = "mp3",
                SampleRate = 24000
            },
            Additions = new SpeechSynthesisAdditions
            {
                ContextTexts = string.IsNullOrWhiteSpace(contextText) ? null : [contextText],
                SilenceDuration = silenceDuration
            }
        }
    };
}

static ScriptInput ParseScript(string script)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(script);

    const string contextPrefix = "[上下文:";
    const string contextSuffix = "]";
    const string pausePrefix = "[停顿:";
    const string pauseSuffix = "秒]";

    string? contextText = null;
    var workingText = script.Trim();

    if (workingText.StartsWith(contextPrefix, StringComparison.Ordinal))
    {
        var contextEndIndex = workingText.IndexOf(contextSuffix, StringComparison.Ordinal);
        if (contextEndIndex > 0)
        {
            contextText = workingText[contextPrefix.Length..contextEndIndex].Trim();
            workingText = workingText[(contextEndIndex + contextSuffix.Length)..].Trim();
        }
    }

    workingText = workingText.Replace(Environment.NewLine, string.Empty, StringComparison.Ordinal)
                             .Replace("\n", string.Empty, StringComparison.Ordinal);

    var segments = new List<SpeechSegment>();
    var currentIndex = 0;

    while (currentIndex < workingText.Length)
    {
        var pauseStartIndex = workingText.IndexOf(pausePrefix, currentIndex, StringComparison.Ordinal);
        if (pauseStartIndex < 0)
        {
            var remainingText = workingText[currentIndex..].Trim();
            if (!string.IsNullOrWhiteSpace(remainingText))
            {
                segments.Add(new SpeechSegment(remainingText, null));
            }

            break;
        }

        var segmentText = workingText[currentIndex..pauseStartIndex].Trim();
        var pauseValueStartIndex = pauseStartIndex + pausePrefix.Length;
        var pauseEndIndex = workingText.IndexOf(pauseSuffix, pauseValueStartIndex, StringComparison.Ordinal);
        if (pauseEndIndex < 0)
        {
            throw new InvalidOperationException("脚本中的停顿标记格式无效。");
        }

        var pauseValueText = workingText[pauseValueStartIndex..pauseEndIndex].Trim();
        if (!double.TryParse(pauseValueText, out var pauseSeconds))
        {
            throw new InvalidOperationException($"无法解析停顿时长：{pauseValueText}");
        }

        if (!string.IsNullOrWhiteSpace(segmentText))
        {
            segments.Add(new SpeechSegment(segmentText, (int) Math.Round(pauseSeconds * 1000)));
        }

        currentIndex = pauseEndIndex + pauseSuffix.Length;
    }

    return new ScriptInput(contextText, segments);
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

internal sealed record SpeechStyle(string Name, string? ContextText);
internal sealed record ScriptInput(string? ContextText, IReadOnlyList<SpeechSegment> Segments);
internal sealed record SpeechSegment(string Text, int? PauseAfterMilliseconds);
