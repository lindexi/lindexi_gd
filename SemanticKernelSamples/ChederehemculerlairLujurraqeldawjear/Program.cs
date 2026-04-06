// 实验大模型录音文件识别带上下文识别功能

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using VolcEngineSdk.OpenSpeech.Contexts;

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

var installApplication =
      """
        7-Zip 19.00 (x64)                                            19.00
        Anaconda3 2022.10 (Python 3.9.13 64-bit)                     2022.10
        B站录播姬                                                    1.2.1
        Docker Desktop                                               2.3.0.3
        EasiAgent                                                    0.0.1.140
        EasiUpdate                                                   1.1.3.433
        EasiUpdate3                                                  3.5.0.911
        Everything 1.4.1.1026 (x64)                                  1.4.1.1026
        EverythingToolbar 2.0.4.0                                    2.0.4.0
        FileAlyzer 2                                                 2.0.5.57
        FormatFactory                                                5.10.0.0
        FreeFileSync                                                 13.9
        Git version 2.31.1                                           2.31.1
        GTK3-Runtime Win64                                           3.24.31-2022-01-04-ts-win64
        HuyaClient
        Intel Processor Diagnostic Tool 64bit                        4.1.4.36
        JetBrains dotCover 2025.3.3                                  2025.3.3
        JetBrains dotMemory 2025.3.3                                 2025.3.3
        JetBrains dotTrace 2025.3.3                                  2025.3.3
        JetBrains ReSharper in Visual Studio Community 2019          2021.1.5
        JetBrains ReSharper in Visual Studio Community 2022          2024.3.7
        JetBrains ReSharper in Visual Studio Community 2022 Preview  2023.2.1
        JetBrains ReSharper in Visual Studio Community 2026          2025.3.3
        KingstonSSDUpdate                                            1.0.1.14
        Listary version 6.3                                          6.3
        Microsoft .NET SDK 6.0.100-rc.2.21505.57 (x64)               6.1.21.50557
        Microsoft .NET SDK 8.0.300 (x64)                             8.3.24.22415
        Microsoft .NET SDK 9.0.100-preview.5.24307.3 (x64)           9.1.24.30703
        Microsoft 365 - zh-cn                                        16.0.19822.20142
        Microsoft Azure Compute Emulator - v2.9.7                    2.9.8999.43
        Microsoft Azure Storage Emulator - v5.10                     5.10.19227.2113
        Microsoft Edge                                               146.0.3856.97
        Microsoft OneDrive                                           26.051.0316.0004
        Microsoft Teams                                              1.5.00.33312
        Microsoft Visual C++ 2013 Redistributable (x64) - 12.0.30501 12.0.30501.0
        Microsoft Visual C++ 2013 Redistributable (x86) - 12.0.30501 12.0.30501.0
        Microsoft Visual C++ v14 Redistributable (x64) - 14.50.35719 14.50.35719.0
        Microsoft Visual C++ v14 Redistributable (x86) - 14.50.35719 14.50.35719.0
        Microsoft Visual Studio Installer                            4.4.38.63497
        Mozilla Firefox (x64 zh-CN)                                  149.0
        Mozilla Maintenance Service                                  78.0.2
        OBS Studio                                                   27.0.1
        PotPlayer-64 bit                                             26.01.14.0
        QQ影音                                                       4.6.3.1104
        Realtek Audio Driver                                         6.0.8971.1
        RunswUpgrade                                                 1.0.1.24
        Steam                                                        2.10.91.91
        Sublime Text 3
        TeamViewer                                                   15.10.5
        Total Commander 64-bit (Remove or Repair)                    9.51
        Unity 2022.1.2f1c1                                           2022.1.2f1c1
        Unity Hub 3.1.2-c1                                           3.1.2-c1
        Update for  (KB2504637)                                      1
        Visual Studio Community 2019                                 16.11.3
        Visual Studio Community 2026                                 18.4.2
        VRChat
        Windows Software Development Kit - Windows 10.0.19041.5609   10.1.19041.5609
        Windows Software Development Kit - Windows 10.0.22621.5040   10.1.22621.5040
        Windows Software Development Kit - Windows 10.0.26100.6584   10.1.26100.6584
        Windows Software Development Kit - Windows 10.0.26100.7705   10.1.26100.7705
        哔哩哔哩直播姬                                               3.24.0.1906
        软媒魔方                                                     6.2.5.0
        腾讯课堂                                                     1.0
        微软OfficePLUS                                               3.15.0.34264
        希沃白板 5                                                   5.2.4.9612
        向日葵                                                       12.6.0.49095
        小狼毫输入法                                                 0.16.3.0
        """;



var corpusContext = new CorpusContext()
{
    ContextData =
    {
        new CorpusContextData()
        {
            Text = "你是一个应用软件使用助手，你可以调用工具帮助用户实现电脑的操作",
        }, 
        new CorpusContextData()
        {
            Text = "现在电脑上安装的程序有：哔哩哔哩直播姬、软媒魔方、腾讯课堂、微软OfficePLUS、希沃白板、向日葵、小狼毫输入法、QQ影音。请选择将打开的应用程序",
        },
    }
};

var corpusContextJson = JsonSerializer.Serialize(corpusContext, new JsonSerializerOptions()
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
});
var asrRequest = new AsrRequest()
{
    User = new UserMeta()
    {
        Uid = "abc"
    },
    Audio = new AudioMeta()
    {
        Url = "https://cstore-ali-study-pub.xbstatic.com/running-wechat-mp/uwizqwnxhhwljhohhwvwuolhpmhhihhh.wav",
        Format = "wav",
    },
    Request = new RequestMeta()
    {
        ModelName = "bigmodel",
        Corpus = new CorpusMeta()
        {
            Context = corpusContextJson,
        }
    }
};

var jsonString = JsonSerializer.Serialize(asrRequest, new JsonSerializerOptions()
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
});

using var httpResponseMessage =
    await httpClient.PostAsync(url, new StringContent(jsonString, Encoding.UTF8, "application/json"));

var responseText = await httpResponseMessage.Content.ReadAsStringAsync();

var queryUrl = "https://openspeech.bytedance.com/api/v3/auc/bigmodel/query";

while (true)
{
    try
    {
        // {"header":{"reqid":"","code":45000000,"message":"unexpected end of JSON input"}}
        using var queryHttpResponseMessage =
            await httpClient.PostAsync(queryUrl, new StringContent("{}", Encoding.UTF8, "application/json"));
        var queryHttpResponseText = await queryHttpResponseMessage.Content.ReadAsStringAsync();

        // {"audio_info":{"duration":4223},"result":{"additions":{"duration":"4223"},"text":"打开西瓜白榜。","utterances":[{"end_time":2890,"start_time":1450,"text":"打开西瓜白榜。","words":[{"confidence":0,"end_time":1690,"start_time":1450,"text":"打"},{"confidence":0,"end_time":1890,"start_time":1690,"text":"开"},{"confidence":0,"end_time":2170,"start_time":1930,"text":"西"},{"confidence":0,"end_time":2410,"start_time":2170,"text":"瓜"},{"confidence":0,"end_time":2610,"start_time":2570,"text":"白"},{"confidence":0,"end_time":2890,"start_time":2850,"text":"榜"}]}]}}
        string statusCodeText = string.Empty;
        if (queryHttpResponseMessage.Headers.TryGetValues("X-Api-Status-Code", out var statusCode) &&
            statusCode is string[]
            {
                Length: > 0
            } codeArray)
        {
            statusCodeText = codeArray[0];
        }

        Console.WriteLine($"[{statusCodeText}] {queryHttpResponseText}");

        var speechRecognitionResponse = JsonSerializer.Deserialize<SpeechRecognitionResponse>(queryHttpResponseText);
        if (!string.IsNullOrEmpty(speechRecognitionResponse?.Result?.Text))
        {
            break;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
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