// See https://aka.ms/new-console-template for more information

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),

});

/*
 *System.ClientModel.ClientResultException:“HTTP 400 (BadRequest: InvalidParameter)
   Parameter: response_format.type
   
   The parameter `response_format.type` specified in the request are not valid: `json_schema` is not supported by this model. Request id: 021774405147745e3c0010419774ef230e4bef6206f9366dbefa7”
   
 */
var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

//var jsonSerializerOptions = new JsonSerializerOptions();
//var jsonSchemaAsNode = jsonSerializerOptions.GetJsonSchemaAsNode(typeof(PersonInfo));
//var jsonString = jsonSchemaAsNode.ToJsonString();
var serviceCollection = new ServiceCollection();


IChatClient client = chatClient.AsIChatClient();

serviceCollection.AddSingleton(client);
serviceCollection.AddSingleton(s =>
{
    var innerClient = s.GetRequiredService<IChatClient>();
    var functionInvokingChatClient = new FunctionInvokingChatClient(innerClient, functionInvocationServices: s);

    functionInvokingChatClient.FunctionInvoker = (context, token) =>
    {
        return context.Function.InvokeAsync(context.Arguments, token);
    };

    return functionInvokingChatClient;
});

var chatClientBuilder = client
    .AsBuilder();

var agent = chatClientBuilder
        .Use((innerClient, services) =>
        {
            var invokingChatClient = services.GetService<FunctionInvokingChatClient>();

            return innerClient;
        })
        //.Use(runFunc: CustomAgentRunMiddleware, runStreamingFunc: RunStreamingFunc)
        
        .BuildAIAgent(options: new ChatClientAgentOptions()
        {
            ChatOptions = new ChatOptions()
            {
                //ResponseFormat = ChatResponseFormat.Json
                Tools = [
                    AIFunctionFactory.Create(GetWeather)
                ]
            }
        });


static async ValueTask<object?> CustomFunctionCallingMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"Function Name: {context!.Function.Name}");
    var result = await next(context, cancellationToken);
    Console.WriteLine($"Function Call Result: {result}");

    return result;
}

await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(new ChatMessage(ChatRole.User, "今天北京的天气咋样.")))
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

Console.Read();

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location,
    [Description("查询天气的日期")] string date)
{
    return $"查询不到 {location} 城市信息";
}