// 实验大模型录音文件识别带上下文识别功能

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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
httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Resource-Id", "volc.seedasr.auc");

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
Add(audioJsonObject, "format","wav");

var requestJsonObject = new JsonObject();
jsonObject.Add("request", requestJsonObject);
requestJsonObject["model_name"] = "bigmodel";

var jsonString = jsonObject.ToJsonString();
using var httpResponseMessage = await httpClient.PostAsync(url, new StringContent(jsonString,Encoding.UTF8, "application/json"));

var responseText = await httpResponseMessage.Content.ReadAsStringAsync();

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