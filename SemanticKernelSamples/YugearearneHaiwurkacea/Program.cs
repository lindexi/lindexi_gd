// See https://aka.ms/new-console-template for more information

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ComponentModel;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;


var keyFile = @"C:\lindexi\Work\deepseek.txt";
var key = File.ReadAllText(keyFile);

#pragma warning disable MEAI001
var messageCountingChatReducer = new MessageCountingChatReducer(2);
var inMemoryChatMessageStore = new InMemoryChatMessageStore(messageCountingChatReducer);
inMemoryChatMessageStore.Add(new ChatMessage(ChatRole.User, "asdasdasd1"));
inMemoryChatMessageStore.Add(new ChatMessage(ChatRole.User, "asdasdasd2"));
inMemoryChatMessageStore.Add(new ChatMessage(ChatRole.User, "asdasdasd3"));
inMemoryChatMessageStore.Add(new ChatMessage(ChatRole.User, "asdasdasd4"));
inMemoryChatMessageStore.Add(new ChatMessage(ChatRole.User, "asdasdasd5"));
var chatMessages = await inMemoryChatMessageStore.GetMessagesAsync();

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://api.deepseek.com/v1")
});

var chatClient = openAiClient.GetChatClient("deepseek-chat");


AIFunction weatherFunction = AIFunctionFactory.Create(GetWeather);

ChatClientAgent aiAgent = chatClient.CreateAIAgent(tools:
[
    weatherFunction,
    AIFunctionFactory.Create(GetDateTime),
]);

var agentWithMiddleware = aiAgent.AsBuilder()
    .Use(runFunc: CustomAgentRunMiddleware, runStreamingFunc: null)
    .Use(CustomFunctionCallingMiddleware)
    .Build();

var agentRunResponse = await agentWithMiddleware.RunAsync("今天北京的天气咋样");

Console.WriteLine(agentRunResponse);

Console.Read();
return;


[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location,
    [Description("查询天气的日期")] string date)
{
    return $"查询不到 {location} 城市信息";
}

[Description("Get the current date and time.")]
static async Task<DateTime> GetDateTime() => DateTime.Now.AddYears(1000);

async Task<AgentRunResponse> CustomAgentRunMiddleware
(
    IEnumerable<ChatMessage> messages,
    AgentThread? thread,
    AgentRunOptions? options,
    AIAgent innerAgent,
    CancellationToken cancellationToken
)
{
    Console.WriteLine($"Input: {messages.Count()}");
    var response = await innerAgent.RunAsync(messages, thread, options, cancellationToken).ConfigureAwait(false);
    Console.WriteLine($"Output: {response.Messages.Count}");
    return response;
}

async ValueTask<object?> CustomFunctionCallingMiddleware
(
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