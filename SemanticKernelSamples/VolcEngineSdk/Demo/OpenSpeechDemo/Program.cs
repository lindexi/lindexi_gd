using VolcEngineSdk.OpenSpeech;
using VolcEngineSdk.OpenSpeech.Contexts;

var accessTokenFile = @"C:\lindexi\Work\Key\OpenSpeech TTS Access Token.txt";
var appId = "5866932789";

// speaker 发音人： voiceType
var voiceType = "zh_female_vv_uranus_bigtts";

// 使用 context_texts 为语音合成提供辅助信息，实现不同语气说话。
// 当前字符串列表只第一个值有效，因此通过多次请求分别输出不同语气的音频文件。

// 模型版本： seed-tts-2.0-expressive
const string resourceId = "seed-tts-2.0";
const string model = "seed-tts-2.0-expressive";
const string text = "工作占据了生活的绝大部分，只有去做自己认为伟大的工作，才能获得满足感。不管生活再苦再累，都绝不放弃寻找。";

var speechStyles = new[]
{
    new SpeechStyle("default", null),
    new SpeechStyle("sad", "你可以用特别特别痛心的语气说话吗?"),
    new SpeechStyle("slow", "你可以说慢一点吗？")
};

Console.WriteLine("开始调用 OpenSpeech 语音合成...");

var authentication = CreateAuthentication(appId, accessTokenFile, resourceId);

using var httpClient = new HttpClient();
var client = new OpenSpeechClient(httpClient);

foreach (var speechStyle in speechStyles)
{
    var outputFilePath = Path.Combine(AppContext.BaseDirectory, $"context-texts-{speechStyle.Name}.mp3");
    var request = CreateRequest(voiceType, model, text, speechStyle.ContextText);
    var options = new SpeechSynthesisRequestOptions(authentication)
    {
        Protocol = SpeechSynthesisProtocol.HttpChunked,
        RequestId = Guid.NewGuid().ToString(),
        UsageTokensToReturn = "text_words"
    };

    var result = await client.SynthesizeAsync(request, options);
    await File.WriteAllBytesAsync(outputFilePath, result.AudioData);

    Console.WriteLine($"语气版本: {speechStyle.Name}");
    Console.WriteLine($"音频文件已生成: {outputFilePath}");
    Console.WriteLine($"音频字节数: {result.AudioData.Length}");
    Console.WriteLine($"返回句子数: {result.Sentences.Count}");
    Console.WriteLine($"计费字符数: {result.Usage?.TextWords?.ToString() ?? "未返回"}");
    Console.WriteLine($"服务端 LogId: {result.LogId ?? "未返回"}");
}

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

static SpeechSynthesisRequest CreateRequest(string voiceType, string model, string text, string? contextText)
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
                ContextTexts = string.IsNullOrWhiteSpace(contextText) ? null : [contextText]
            }
        }
    };
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
