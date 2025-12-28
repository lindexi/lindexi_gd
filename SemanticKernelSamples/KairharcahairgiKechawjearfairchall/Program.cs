// See https://aka.ms/new-console-template for more information

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Channels;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\deepseek.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://api.deepseek.com/v1")
});

var chatClient = openAiClient.GetChatClient("deepseek-chat");

AIFunction weatherFunction = AIFunctionFactory.Create(GetWeather);

ChatClientAgent weatherAgent = chatClient.CreateAIAgent
(
    name: "WeatherAgent",
    description: "An agent that answers questions about the weather.",
    tools:
    [
        weatherFunction,
        AIFunctionFactory.Create(GetDateTime),
    ]
);
// 将一个 Agent 当成工具
AIFunction weatherAgentFunction = weatherAgent.AsAIFunction();

// 再从工具创建 MCP 工具
McpServerTool tool = McpServerTool.Create(weatherAgentFunction);

// 接下来就是创建 MCP 服务和添加对应的测试逻辑

// 使用自定义的进程内传输工厂，用来在当前进程内同时作为 MCP 服务器和客户端通讯
var interprocessTransportFactory = new InterprocessTransportFactory();

// 用 HostApplicationBuilder 来创建 MCP 服务器
HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    .AddMcpServer()
    // 使用 STDIO 作为传输协议
    .WithStdioServerTransport()
    // 注册工具
    .WithTools([tool]);

// 使用自定义的进程内传输，代替 STDIO 传输
builder.Services.AddSingleton<ITransport>(interprocessTransportFactory.GetServerTransport());

IHost host = builder.Build();
await host.StartAsync();

// 创建 MCP 客户端
IClientTransport clientTransport = interprocessTransportFactory.GetClientTransport();

McpClient mcpClient = await McpClient.CreateAsync(clientTransport);

IList<McpClientTool> toolList = await mcpClient.ListToolsAsync();

foreach (var mcpClientTool in toolList)
{
    Console.WriteLine($"Tool {mcpClientTool.Name}");
}

CallToolResult callToolResult = await mcpClient.CallToolAsync("WeatherAgent", arguments: new AIFunctionArguments()
{
    { "query", "今天北京的天气咋样" }
});

foreach (var contentBlock in callToolResult.Content)
{
    if (contentBlock is TextContentBlock textContentBlock)
    {
        Console.WriteLine(textContentBlock.Text);
    }
}

return;


[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location,
    [Description("查询天气的日期")] string date)
{
    return $"The weather in {location} is cloudy with a high of 100°C";
}

[Description("Get the current date and time.")]
static DateTime GetDateTime()
{
    var time = DateTime.Now;
    return new DateTime(3000, 1, 15, time.Hour, time.Minute, time.Second);
}

// [dotnet MCP 无魔法 本地进程内服务端客户端调用和通讯示例 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18869179 )
class InterprocessTransportFactory
{
    public InterprocessTransportFactory()
    {
        _clientTransport = new ClientTransport(this);
        _serverTransport = new ServerTransport(this);
    }

    private readonly ClientTransport _clientTransport;
    private readonly ServerTransport _serverTransport;

    public IClientTransport GetClientTransport()
        => _clientTransport;

    public ITransport GetServerTransport()
        => _serverTransport;

    class ClientTransport(InterprocessTransportFactory factory) : TransportBase, IClientTransport, ITransport
    {
        private readonly InterprocessTransportFactory _factory = factory;

        public Task<ITransport> ConnectAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult<ITransport>(this);
        }

        public string Name => "ClientTransport";

        public override async Task SendMessageAsync(JsonRpcMessage message,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await _factory._serverTransport.Channel.Writer.WriteAsync(message, cancellationToken);
        }
    }

    class ServerTransport(InterprocessTransportFactory factory) : TransportBase
    {
        private readonly InterprocessTransportFactory _factory = factory;

        public override async Task SendMessageAsync(JsonRpcMessage message,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await _factory._clientTransport.Channel.Writer.WriteAsync(message, cancellationToken);
        }
    }

    abstract class TransportBase : ITransport
    {
        public Channel<JsonRpcMessage> Channel { get; } =
            System.Threading.Channels.Channel.CreateUnbounded<JsonRpcMessage>();

        public ValueTask DisposeAsync()
        {
            Channel.Writer.Complete();
            return ValueTask.CompletedTask;
        }

        public abstract Task SendMessageAsync(JsonRpcMessage message,
            CancellationToken cancellationToken = new CancellationToken());

        public string? SessionId { get; }

        public ChannelReader<JsonRpcMessage> MessageReader => Channel.Reader;
    }
}