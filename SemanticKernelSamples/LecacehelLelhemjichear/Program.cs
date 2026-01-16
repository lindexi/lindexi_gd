// See https://aka.ms/new-console-template for more information
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using OpenAI.Files;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAIClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var file = @"C:\lindexi\Image.png";

// 豆包对于 FileStatus 处理错误，传入了 "processing" 导致失败
//var fileClient = openAIClient.GetOpenAIFileClient();
//ClientResult<OpenAIFile> clientResult = await fileClient.UploadFileAsync(file, "user_data");
//var uploadedFileName = clientResult.Value.Filename;

using var httpClient = new HttpClient();

var uploadFileUrl = "https://ark.cn-beijing.volces.com/api/v3/files";
using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uploadFileUrl);
httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
using var multipartFormDataContent = new MultipartFormDataContent();
// {"error":{"code":"InvalidParameter","message":"Purpose must be user_data. Request id: 0217685302674875ed129591da48e074cf9db74a161503f61e734","param":"purpose","type":"BadRequest"}}
multipartFormDataContent.Add(new StringContent("user_data"), "purpose");
await using var fileStream = File.OpenRead(file);
multipartFormDataContent.Add(new StreamContent(fileStream), "file", "Image.png");
httpRequestMessage.Content = multipartFormDataContent;

using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
httpResponseMessage.EnsureSuccessStatusCode();
var uploadResponse = await httpResponseMessage.Content.ReadAsStringAsync();
/*
{"object":"file","id":"file-20260116102532-jnrpj","purpose":"user_data","filename":"Image.png","bytes":1086184,"mime_type":"image/png","created_at":1768530332,"expire_at":1769135132,"status":"processing"}
 */
var jsonNode = JsonNode.Parse(uploadResponse);
var uploadedFileName = jsonNode?["id"]?.AsValue().ToString();

if (string.IsNullOrEmpty(uploadedFileName))
{
    Console.WriteLine($"上传失败 Response={uploadResponse}");
    return;
}

//await CheckAsync(uploadedFileName, key);

var chatClient = openAIClient.GetChatClient("ep-20260115192014-kgkxq");

AIFunction weatherFunction = AIFunctionFactory.Create(GetWeather);

ChatClientAgent aiAgent = chatClient.CreateAIAgent(tools:
[
    weatherFunction,
    AIFunctionFactory.Create(GetDateTime),
]);

var agentThread = aiAgent.GetNewThread();

ChatMessage message = new(ChatRole.User,
[
    new TextContent("图中这个地方今天的天气怎样"),
    //new UriContent($"file://{uploadedFileName}", "image/jpeg")
    new HostedFileContent(uploadedFileName),
]);

await foreach (var agentRunResponseUpdate in aiAgent.RunStreamingAsync(message, agentThread))
{
    Console.Write(agentRunResponseUpdate.Text);
}

Console.WriteLine();

await foreach (var agentRunResponseUpdate in aiAgent.RunStreamingAsync("这个笑话不好笑，给我换一个", agentThread))
{
    Console.Write(agentRunResponseUpdate.Text);
}

Console.Read();

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location, [Description("查询天气的日期")] string date)
{
    return $"{location} 城市温度为 100 摄氏度";
}

[Description("Get the current date and time.")]
static DateTime GetDateTime() => DateTime.Now.AddYears(1000);

static async Task CheckAsync(string uploadedFileName, string key)
{
    // 检索文件
    var findFileUrl = $"https://ark.cn-beijing.volces.com/api/v3/files/{uploadedFileName}";
    using var httpClient = new HttpClient();
    using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, findFileUrl);
    httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
    using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
    var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
    // {"object":"file","id":"file-20260116103421-875jp","purpose":"user_data","filename":"Image.png","bytes":1086184,"mime_type":"image/png","created_at":1768530861,"expire_at":1769135661,"status":"active"}
}