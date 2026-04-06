// 实验大模型录音文件识别带上下文识别功能

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ChederehemculerlairLujurraqeldawjear.Contexts;

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

//var jsonObject = new JsonObject();
////JsonElement jsonElement = JsonSerializer.SerializeToElement(new Dictionary<string, string>());

//var audioJsonObject = new JsonObject();
//jsonObject.Add("audio", audioJsonObject);

//Add(audioJsonObject, "url", "https://cstore-ali-study-pub.xbstatic.com/running-wechat-mp/uwizqwnxhhwljhohhwvwuolhpmhhihhh.wav");
//Add(audioJsonObject, "format", "wav");

//var requestJsonObject = new JsonObject();
//jsonObject.Add("request", requestJsonObject);
//requestJsonObject["model_name"] = "bigmodel";


var asrRequest = new AsrRequest()
{
    User = new UserMeta()
    {
        Uid = "abc"
    }                      ,
    Audio = new AudioMeta()
    {
        Url = "https://cstore-ali-study-pub.xbstatic.com/running-wechat-mp/uwizqwnxhhwljhohhwvwuolhpmhhihhh.wav",
        Format = "wav",
    },
    Request = new RequestMeta()
    {
        ModelName = "bigmodel",
    }
};

var jsonString = JsonSerializer.Serialize(asrRequest);

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
