using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using OpenAI;

using System.ClientModel;
using System.Net.Http.Headers;

var keyFile = @"C:\lindexi\Work\Qwen.txt";
var key = File.ReadAllText(keyFile);

{
    // 直接测试 POST 内容
    using var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1");
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

    var postContent =
        """
        {
            "model": "qwen3-omni-flash",
            "messages": [
            {
              "role": "user",
              "content": [
                {
                  "type": "input_audio",
                  "input_audio": {
                    "data": "https://help-static-aliyun-doc.aliyuncs.com/file-manage-files/zh-CN/20250211/tixcef/cherry.wav",
                    "format": "wav"
                  }
                },
                {
                  "type": "text",
                  "text": "这段音频在说什么"
                }
              ]
            }
          ],
            
            "modalities":["text"]
        }
        """;

    var httpResponseMessage = await httpClient.PostAsync("https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
        new StringContent(postContent, new MediaTypeHeaderValue("application/json"))
        {
        });
    // {"error":{"message":"<400> InternalError.Algo.InvalidParameter: Voice 'Tina' is not supported.","type":"invalid_request_error","param":null,"code":"invalid_parameter_error"},"id":"chatcmpl-73a3b2bf-f305-9d9f-88b3-927851126cd0","request_id":"73a3b2bf-f305-9d9f-88b3-927851126cd0"}
    var responseText = await httpResponseMessage.Content.ReadAsStringAsync();
    Console.WriteLine(responseText);
}

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1"),
});

//openAiClient.GetAudioClient("qwen3-omni-flash").

var chatClient = openAiClient.GetChatClient("qwen3-omni-flash");


// qwen3.5-omni-plus: System.ClientModel.ClientResultException:“HTTP 403 (access_denied: access_denied)

var agent = chatClient.AsIChatClient()
    .AsBuilder()
    .BuildAIAgent(new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                //AIFunctionFactory.Create(OpenApplication),
                //AIFunctionFactory.Create(WriteFileInfo)
            ]
        }
    });

ChatMessage[] chatMessageList =
[
    new ChatMessage(ChatRole.System,"你是一个应用软件使用助手，你可以调用工具帮助用户实现电脑的操作"),
    new ChatMessage(ChatRole.User,
    [
        new TextContent("以下是我说话的内容"),
        //new UriContent("https://pro-en-ali-pub.en5static.com/easinote5_public/uwizqwnxhhqjjhnohwvvxlixjjphihhh.mp4", "video/mpeg"),
        //new UriContent("https://pro-en-ali-pub.en5static.com/easinote5_public/uwixjonmhhqjjhnohwvvwyyhvzphihhh.mp3", "audio/mpeg"),
        new UriContent("https://help-static-aliyun-doc.aliyuncs.com/file-manage-files/zh-CN/20250211/tixcef/cherry.wav","audio/*"),
    ]),
];

var uriContent = new UriContent("https://help-static-aliyun-doc.aliyuncs.com/file-manage-files/zh-CN/20250211/tixcef/cherry.wav", "audio/*");

//var testInput = new ChatMessage(ChatRole.User, "请问 mp3 的 MIME 类型字符串是什么？");
//_ = testInput;

await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(chatMessageList))
{
    if (reasoningAgentResponseUpdate.IsFirstThinking)
    {
        Console.WriteLine($"思考：");
    }

    if (reasoningAgentResponseUpdate.IsThinkingEnd && reasoningAgentResponseUpdate.IsFirstOutputContent)
    {
        Console.WriteLine();
        Console.WriteLine("----------");
    }

    Console.Write(reasoningAgentResponseUpdate.Reasoning);
    Console.Write(reasoningAgentResponseUpdate.Text);
}

Console.WriteLine();
Console.WriteLine($"结束");
Console.WriteLine();
Console.ReadLine();

Console.WriteLine("Hello, World!");



