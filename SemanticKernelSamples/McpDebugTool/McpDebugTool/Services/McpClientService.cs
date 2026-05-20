using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpDebugTool.Services;

public sealed class McpClientService : IAsyncDisposable
{
    private const string ClientName = "McpDebugTool";
    private McpClient? _client;

    public bool IsConnected => _client is not null;

    public async Task<IReadOnlyList<McpClientTool>> ConnectAsync(string serverUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            throw new ArgumentException("请输入有效的 MCP HTTP 地址。", nameof(serverUrl));
        }

        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri? endpoint))
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "无法识别 MCP HTTP 地址：{0}", serverUrl), nameof(serverUrl));
        }

        await DisconnectAsync();

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = endpoint,
            Name = ClientName,
        });

        _client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        IList<McpClientTool> tools = await _client.ListToolsAsync(options: null, cancellationToken: cancellationToken);

        return tools
            .OrderBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<CallToolResult> CallToolAsync(McpClientTool tool, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(arguments);

        return await tool.CallAsync(arguments, cancellationToken: cancellationToken);
    }

    public async Task DisconnectAsync()
    {
        if (_client is null)
        {
            return;
        }

        await _client.DisposeAsync();
        _client = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
