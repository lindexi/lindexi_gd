// 实验大模型录音文件识别带上下文识别功能

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

var keyFile = @"C:\lindexi\Work\Speech.txt";
var key = File.ReadAllText(keyFile).Trim();
/*
   豆包录音文件识别模型1.0
   - volc.bigasr.auc
   豆包录音文件识别模型2.0
   - volc.seedasr.auc
 */

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", key);
var resourceId = "volc.seedasr.auc";
httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Resource-Id", resourceId);

var requestId = Guid.NewGuid().ToString();
httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Request-Id", requestId);
// 发包序号，固定值，-1
httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Sequence", "-1");

var url = "https://openspeech.bytedance.com/api/v3/auc/bigmodel/submit";

var jsonObject = new JsonObject();
//JsonElement jsonElement = JsonSerializer.SerializeToElement(new Dictionary<string, string>());

var audioJsonObject = new JsonObject();
jsonObject.Add("audio", audioJsonObject);

Add(audioJsonObject, "url", "https://cstore-ali-study-pub.xbstatic.com/running-wechat-mp/uwizqwnxhhwljhohhwvwuolhpmhhihhh.wav");
Add(audioJsonObject, "format", "wav");

var requestJsonObject = new JsonObject();
jsonObject.Add("request", requestJsonObject);
requestJsonObject["model_name"] = "bigmodel";

var jsonString = jsonObject.ToJsonString();
using var httpResponseMessage = await httpClient.PostAsync(url, new StringContent(jsonString, Encoding.UTF8, "application/json"));

var responseText = await httpResponseMessage.Content.ReadAsStringAsync();

var queryUrl = "https://openspeech.bytedance.com/api/v3/auc/bigmodel/query";

while (true)
{
    // {"header":{"reqid":"","code":45000000,"message":"unexpected end of JSON input"}}
    using var queryHttpResponseMessage = await httpClient.PostAsync(queryUrl, new StringContent("{}", Encoding.UTF8, "application/json"));
    var queryHttpResponseText = await queryHttpResponseMessage.Content.ReadAsStringAsync();

    // {"audio_info":{"duration":4223},"result":{"additions":{"duration":"4223"},"text":"打开西瓜白榜。","utterances":[{"end_time":2890,"start_time":1450,"text":"打开西瓜白榜。","words":[{"confidence":0,"end_time":1690,"start_time":1450,"text":"打"},{"confidence":0,"end_time":1890,"start_time":1690,"text":"开"},{"confidence":0,"end_time":2170,"start_time":1930,"text":"西"},{"confidence":0,"end_time":2410,"start_time":2170,"text":"瓜"},{"confidence":0,"end_time":2610,"start_time":2570,"text":"白"},{"confidence":0,"end_time":2890,"start_time":2850,"text":"榜"}]}]}}
    var speechRecognitionResponse = JsonSerializer.Deserialize<SpeechRecognitionResponse>(queryHttpResponseText);
    if (!string.IsNullOrEmpty(speechRecognitionResponse?.Result?.Text))
    {
        break;
    }
}

Console.WriteLine("Hello, World!");

static void Add(JsonObject jsonObject, string propertyName, object obj)
{
    if (obj is string str)
    {
        jsonObject[propertyName] = str;
    }
    else
    {
        JsonElement jsonElement = JsonSerializer.SerializeToElement(obj);
        jsonObject.Add(propertyName, JsonObject.Create(jsonElement));
    }
}


/// <summary>
/// 语音识别响应根对象
/// </summary>
public class SpeechRecognitionResponse
{
    /// <summary>
    /// 音频信息
    /// </summary>
    [JsonPropertyName("audio_info")]
    public AudioInfo AudioInfo { get; set; }

    /// <summary>
    /// 识别结果
    /// </summary>
    [JsonPropertyName("result")]
    public RecognitionResult Result { get; set; }
}

/// <summary>
/// 音频基础信息
/// </summary>
public class AudioInfo
{
    /// <summary>
    /// 音频总时长（毫秒）
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
}

/// <summary>
/// 核心识别结果
/// </summary>
public class RecognitionResult
{
    /// <summary>
    /// 完整识别文本
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }

    /// <summary>
    /// 分句识别结果列表
    /// </summary>
    [JsonPropertyName("utterances")]
    public List<Utterance> Utterances { get; set; }
}

/// <summary>
/// 分句识别单元
/// </summary>
public class Utterance
{
    /// <summary>
    /// 是否为确定性识别结果
    /// </summary>
    [JsonPropertyName("definite")]
    public bool Definite { get; set; }

    /// <summary>
    /// 分句结束时间（毫秒）
    /// </summary>
    [JsonPropertyName("end_time")]
    public int EndTime { get; set; }

    /// <summary>
    /// 分句开始时间（毫秒）
    /// </summary>
    [JsonPropertyName("start_time")]
    public int StartTime { get; set; }

    /// <summary>
    /// 分句文本
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }

    /// <summary>
    /// 单字识别结果列表
    /// </summary>
    [JsonPropertyName("words")]
    public List<Word> Words { get; set; }
}

/// <summary>
/// 单字识别单元
/// </summary>
public class Word
{
    /// <summary>
    /// 字间空白时长（毫秒）
    /// </summary>
    [JsonPropertyName("blank_duration")]
    public int BlankDuration { get; set; }

    /// <summary>
    /// 单字结束时间（毫秒）
    /// </summary>
    [JsonPropertyName("end_time")]
    public int EndTime { get; set; }

    /// <summary>
    /// 单字开始时间（毫秒）
    /// </summary>
    [JsonPropertyName("start_time")]
    public int StartTime { get; set; }

    /// <summary>
    /// 单字文本
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }
}