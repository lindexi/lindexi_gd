using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Net.Http.Headers;

var keyFile = @"C:\lindexi\Work\Qwen.txt";
var key = File.ReadAllText(keyFile);

{
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

    installApplication = installApplication.Replace("\r\n", "\n").Replace("\n", "\\n");

    // 直接测试 POST 内容
    using var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1");
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

    var postContent =
        $$"""
          {
              "model": "qwen3-omni-flash",
              "messages": [
              {
                "role": "user",
                "content": [
                  {
                    "type": "input_audio",
                    "input_audio": {
                      "data": "https://cstore-ali-study-pub.xbstatic.com/running-wechat-mp/uwizqwnxhhwljhohhwvwuolhpmhhihhh.wav",
                      "format": "wav"
                    }
                  },
                  {
                    "type": "text",
                    "text": "你是一个应用软件使用助手，你可以调用工具帮助用户实现电脑的操作。以下这是电脑里面已有的软件：\r\n{{installApplication}}"
                  }
                ]
              }
            ],
              
              "modalities":["text"]
          }
          """;

    var httpResponseMessage = await httpClient.PostAsync(
        "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
        new StringContent(postContent, new MediaTypeHeaderValue("application/json"))
        {
        });
    // {"error":{"message":"<400> InternalError.Algo.InvalidParameter: Voice 'Tina' is not supported.","type":"invalid_request_error","param":null,"code":"invalid_parameter_error"},"id":"chatcmpl-73a3b2bf-f305-9d9f-88b3-927851126cd0","request_id":"73a3b2bf-f305-9d9f-88b3-927851126cd0"}

    /*
     {"choices":[{"message":{"content":"我注意到你提到“打开西瓜白板”，但你的电脑中并没有名为“西瓜白板”的软件。不过，你电脑中安装了：\n\n✅ **希沃白板 5** —— 一款常见的教育互动白板软件，可能你指的是它。\n\n---\n\n### ✅ 建议操作：\n\n**请尝试打开“希沃白板 5”：**\n\n1. 点击 **开始菜单**（或使用 `Win + S` 键搜索）。\n2. 输入：**“希沃白板”** 或 **“Seewo Whiteboard”**。\n3. 点击打开即可。\n\n---\n\n### ❗ 如果你确实指的是“西瓜白板”：\n\n目前市面上没有官方名为“西瓜白板”的主流软件。可能你指的是：\n\n- **西瓜视频**（字节跳动旗下）—— 但这是视频平台，不是白板工具。\n- **“西瓜”是昵称或误记** —— 很可能是“希沃白板”（Seewo）的口误。\n- 或者你指的是 **“ClassIn”、“钉钉白板”、“腾讯会议白板”** 等在线教学白板工具。\n\n---\n\n### 🛠️ 如果你想使用在线白板：\n\n你可以打开浏览器访问：\n\n- **ClassIn 白板**：https://www.classin.com\n- **腾讯会议白板**：会议中点击“白板”按钮\n- **钉钉白板**：钉钉App → 会议 → 白板功能\n- **Microsoft Whiteboard**（微软官方）：在 Microsoft Store 搜索安装\n\n---\n\n### 📌 总结：\n\n> 你电脑中没有“西瓜白板”，但有 **“希沃白板 5”**，建议你打开它。  \n> 如果你指的是其他软件，请确认名称是否正确，或提供更多描述（如功能、用途等），我可以帮你找替代方案。\n\n需要我帮你打开“希沃白板 5”吗？我可以指导你一步步操作 👍","reasoning_content":"","role":"assistant"},"finish_reason":"stop","index":0,"logprobs":null}],"object":"chat.completion","usage":{"prompt_tokens":1555,"completion_tokens":429,"total_tokens":1984,"prompt_tokens_details":{"audio_tokens":54,"text_tokens":1501},"completion_tokens_details":{"text_tokens":429}},"created":1775385563,"system_fingerprint":null,"model":"qwen3-omni-flash","id":"chatcmpl-baabac42-8047-925f-80a4-916118194bd0"}
     */
    var responseText = await httpResponseMessage.Content.ReadAsStringAsync();
    Console.WriteLine(responseText);

    Console.ReadLine();
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
    new ChatMessage(ChatRole.System, "你是一个应用软件使用助手，你可以调用工具帮助用户实现电脑的操作"),
    new ChatMessage(ChatRole.User,
    [
        new TextContent("以下是我说话的内容"),
        //new UriContent("https://pro-en-ali-pub.en5static.com/easinote5_public/uwizqwnxhhqjjhnohwvvxlixjjphihhh.mp4", "video/mpeg"),
        //new UriContent("https://pro-en-ali-pub.en5static.com/easinote5_public/uwixjonmhhqjjhnohwvvwyyhvzphihhh.mp3", "audio/mpeg"),
        new UriContent("https://help-static-aliyun-doc.aliyuncs.com/file-manage-files/zh-CN/20250211/tixcef/cherry.wav",
            "audio/*"),
    ]),
];

var uriContent =
    new UriContent("https://help-static-aliyun-doc.aliyuncs.com/file-manage-files/zh-CN/20250211/tixcef/cherry.wav",
        "audio/*");

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