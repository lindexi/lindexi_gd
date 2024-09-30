// See https://aka.ms/new-console-template for more information

using System.ClientModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

var endpoint = "https://lindexi.openai.azure.com/"; // 请换成你的地址
var apiKey = File.ReadAllText(@"C:\lindexi\CA\Key"); // 请换成你的密钥
var model = "GPT4o";

var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton<IChatCompletionService>(new Foo());

var kernel = builder.Build();
kernel.Plugins.AddFromType<LightsPlugin>("Lights");
#pragma warning disable SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
#pragma warning restore SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。

// Create chat history
var history = new ChatHistory();

var chatCompletionService = kernel.Services.GetRequiredService<IChatCompletionService>();

var chatCompletionOptions = new ChatCompletionOptions();
//chatCompletionOptions.Functions.Add(new ChatFunction("change_state", "Changes the state of the light",new BinaryData()));
chatCompletionOptions.ToolChoice = ChatToolChoice.Auto;

var functionTool = ChatTool.CreateFunctionTool("TurnOn", "Turn on the light");
chatCompletionOptions.ToolChoice = ChatToolChoice.Auto;
chatCompletionOptions.Tools.Add(functionTool);
var chatMessageList = new List<ChatMessage>()
{
    new UserChatMessage("Hello, can you help me turn the light?"),
};

// {
// 	"messages": 
// 	[{
// 		"role": "user",
// 		"content": "Hello, can you help me turn the light?"
// 	}],
// 	"model": "GPT4o",
// 	"tools": 
// 	[{
// 		"type": "function",
// 		"function": 
// 		{
// 			"description": "Turn on the light",
// 			"name": "TurnOn"
// 		}
// 	}],
// 	"tool_choice": "auto"
// }

var azureOpenAiClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));

var chatClient = azureOpenAiClient.GetChatClient(model);

// Request:
// POST https://lindexi.openai.azure.com/openai/deployments/GPT4o/chat/completions?api-version=2024-07-01-preview HTTP/1.1
// Host: lindexi.openai.azure.com
// Accept: application/json
// User-Agent: azsdk-net-AI.OpenAI/2.0.0-beta.5 (.NET 9.0.0-preview.5.24306.7; Microsoft Windows 10.0.22631)
// x-ms-client-request-id: e955c91b-d9eb-4deb-9972-d0ea31091a41
// api-key: 979119e393cc44f9bba29fd430c0f70b
// Content-Type: application/json
// Content-Length: 211
// 
// {"messages":[{"role":"user","content":"Hello, can you help me turn the light?"}],"model":"GPT4o","tools":[{"type":"function","function":{"description":"Turn on the light","name":"TurnOn"}}],"tool_choice":"auto"}

// Response:
// HTTP/1.1 200 OK
// Content-Length: 751
// Content-Type: application/json
// x-ms-region: East US
// apim-request-id: 1fac2da2-0d7d-43dc-9ced-cc04dc070711
// x-ratelimit-remaining-requests: 9
// x-accel-buffering: no
// x-ms-rai-invoked: true
// x-request-id: 97a7b987-8f57-459f-bb46-2f198dfffb7b
// Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
// azureml-model-session: d095-20240921234250
// x-content-type-options: nosniff
// x-envoy-upstream-service-time: 188
// x-ms-client-request-id: e955c91b-d9eb-4deb-9972-d0ea31091a41
// x-ratelimit-remaining-tokens: 9350
// Date: Mon, 30 Sep 2024 01:50:30 GMT
// 
// {"choices":[{"content_filter_results":{},"finish_reason":"tool_calls","index":0,"logprobs":null,"message":{"content":null,"role":"assistant","tool_calls":[{"function":{"arguments":"{}","name":"TurnOn"},"id":"call_UDVnXMRxw25rQHFPtdQtjfA7","type":"function"}]}}],"created":1727661030,"id":"chatcmpl-ACzqQfAgFOHi7HMXZlmgDARJeKXkw","model":"gpt-4o-2024-05-13","object":"chat.completion","prompt_filter_results":[{"prompt_index":0,"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}}}],"system_fingerprint":"fp_67802d9a6d","usage":{"completion_tokens":11,"prompt_tokens":46,"total_tokens":57}}
ClientResult<ChatCompletion> chatResult = await chatClient.CompleteChatAsync(chatMessageList, chatCompletionOptions);

var chatCompletion = chatResult.Value;
var toolCalls = chatCompletion.ToolCalls;
/*
{
    {
        "choices":
        [{
            "content_filter_results": {},
            "finish_reason": "tool_calls",
            "index": 0,
            "logprobs": null,
            "message": {
                "content": null,
                "role": "assistant",
                "tool_calls": [{
                    "function": {
                        "arguments": "{}",
                        "name": "TurnOn"
                    },
                    "id": "call_eC7dd4KG9t4zJ0wnjtqI0pBp",
                    "type": "function"
                }]
            }
        }],
        "created": 1727660789,
        "id": "chatcmpl-ACzmXjKjrHkJuQn3mZ6m9p647DgdG",
        "model": "gpt-4o-2024-05-13",
        "object": "chat.completion",
        "prompt_filter_results": [{
            "prompt_index": 0,
            "content_filter_results": {
                "hate": {
                    "filtered": false,
                    "severity": "safe"
                },
                "self_harm": {
                    "filtered": false,
                    "severity": "safe"
                },
                "sexual": {
                    "filtered": false,
                    "severity": "safe"
                },
                "violence": {
                    "filtered": false,
                    "severity": "safe"
                }
            }
        }],
        "system_fingerprint": "fp_67802d9a6d",
        "usage": {
            "completion_tokens": 11,
            "prompt_tokens": 46,
            "total_tokens": 57
        }
    }
}
 */
foreach (var chatToolCall in toolCalls)
{
}

// Get the response from the AI
var result = await chatCompletionService.GetChatMessageContentAsync(
    history,
    executionSettings: openAIPromptExecutionSettings,
    kernel: kernel
);

class Foo : IChatCompletionService
{
    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null, CancellationToken cancellationToken = new CancellationToken())
    {
        await Task.CompletedTask;
        return [];
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null, Kernel? kernel = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await Task.CompletedTask;
        yield break;
    }
}

public class LightsPlugin
{
    // Mock data for the lights
    private readonly List<LightModel> lights = new()
    {
        new LightModel { Id = 1, Name = "Table Lamp", IsOn = false },
        new LightModel { Id = 2, Name = "Porch light", IsOn = false },
        new LightModel { Id = 3, Name = "Chandelier", IsOn = true }
    };

    [KernelFunction("get_lights")]
    [Description("Gets a list of lights and their current state")]
    [return: Description("An array of lights")]
    public async Task<List<LightModel>> GetLightsAsync()
    {
        return lights;
    }

    [KernelFunction("change_state")]
    [Description("Changes the state of the light")]
    [return: Description("The updated state of the light; will return null if the light does not exist")]
    public async Task<LightModel?> ChangeStateAsync(int id, bool isOn)
    {
        var light = lights.FirstOrDefault(light => light.Id == id);

        if (light == null)
        {
            return null;
        }

        // Update the light with the new state
        light.IsOn = isOn;

        return light;
    }
}

public class LightModel
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("is_on")] public bool? IsOn { get; set; }
}