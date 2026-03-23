// See https://aka.ms/new-console-template for more information

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),

});

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");
var chatCompletionOptions = new ChatCompletionOptions();
#pragma warning disable SCME0001

// https://www.volcengine.com/docs/82379/1449737
// 提供 thinking 字段控制是否关闭深度思考能力，实现“复杂任务深度推理，简单任务高效响应”的精细控制，获得成本、效率收益
// 取值说明：
// enabled：强制开启，强制开启深度思考能力。
// disabled：强制关闭深度思考能力。
// auto：模型自行判断是否进行深度思考。
chatCompletionOptions.Patch.Set("$.thinking"u8, BinaryData.FromString("""{ "type": "disabled" }"""));
/*
 *extra_body={
       # "thinking": {"type": "disabled"}, #  Manually disable deep thinking
   }
 */
#pragma warning restore SCME0001

bool isThinking = true;
bool isAnyOutput = false;
bool isAnyThinking = false;
bool isFirstTextOutput = true;

await foreach (var streamingChatCompletionUpdate in chatClient.CompleteChatStreamingAsync([new UserChatMessage("你好")], chatCompletionOptions))
{
    var chatMessageContent = streamingChatCompletionUpdate.ContentUpdate;

#pragma warning disable SCME0001
    ref JsonPatch patch = ref streamingChatCompletionUpdate.Patch;
#pragma warning restore SCME0001

    if (patch.TryGetJson("$.choices[0].delta"u8, out var data))
    {
        var jsonElement = JsonElement.Parse(data.Span);
        if (jsonElement.TryGetProperty("reasoning_content", out var reasoningContent))
        {
            if (!isAnyOutput && isThinking)
            {
                Console.WriteLine("思考：");
            }

            isAnyThinking = true;
            Console.Write(reasoningContent.ToString());
        }
    }

    foreach (var chatMessageContentPart in chatMessageContent)
    {
        if (!string.IsNullOrEmpty(chatMessageContentPart.Text))
        {
            isThinking = false;

            if (isAnyThinking && isFirstTextOutput)
            {
                // 有思考，且当前是首次内容输出，输出分隔符
                Console.WriteLine();
                Console.WriteLine("---------");
            }

            isFirstTextOutput = false;
        }

        Console.Write(chatMessageContentPart.Text);
    }

    isAnyOutput = true;
}

Console.WriteLine();

Console.Read();
