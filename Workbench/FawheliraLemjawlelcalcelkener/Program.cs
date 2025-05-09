// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using System.Threading.Channels;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

var transportFactory = new InterprocessTransportFactory();

McpServerOptions options = new()
{
    ServerInfo = new Implementation() { Name = "MyServer", Version = "1.0.0" },
    Capabilities = new ServerCapabilities()
    {
        Tools = new ToolsCapability()
        {
            ListToolsHandler = (request, cancellationToken) =>
                ValueTask.FromResult(new ListToolsResult()
                {
                    Tools =
                    [
                        new Tool()
                        {
                            Name = "echo",
                            Description = "Echoes the input back to the client.",
                            InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                                {
                                    "type": "object",
                                    "properties": {
                                      "message": {
                                        "type": "string",
                                        "description": "The input to echo back"
                                      }
                                    },
                                    "required": ["message"]
                                }
                                """),
                        }
                    ]
                }),

            CallToolHandler = (request, cancellationToken) =>
            {
                if (request.Params?.Name == "echo")
                {
                    if (request.Params.Arguments?.TryGetValue("message", out var message) is not true)
                    {
                        throw new McpException("Missing required argument 'message'");
                    }

                    return ValueTask.FromResult(new CallToolResponse()
                    {
                        Content = [new Content() { Text = $"Echo: {message}", Type = "text" }]
                    });
                }

                throw new McpException($"Unknown tool: '{request.Params?.Name}'");
            },
        }
    },
};

await using IMcpServer server = McpServerFactory.Create(transportFactory.GetServerTransport(), options);
_ = server.RunAsync();

// 以下是客户端代码
var client = await McpClientFactory.CreateAsync(transportFactory.GetClientTransport());

// Print the list of tools available from the server.
foreach (var tool in await client.ListToolsAsync())
{
    Console.WriteLine($"{tool.Name} ({tool.Description})");
}

var dictionary = new Dictionary<string, object?>()
{
    { "message", "Hello, World!" }
};
CallToolResponse callToolResponse = await client.CallToolAsync("echo", dictionary);
foreach (var content in callToolResponse.Content)
{
    Console.WriteLine($"CallToolResponse: Type={content.Type} Text='{content.Text}'");
}

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

        public ChannelReader<JsonRpcMessage> MessageReader => Channel.Reader;
    }
}