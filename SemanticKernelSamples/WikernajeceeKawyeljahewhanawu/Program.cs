// See https://aka.ms/new-console-template for more information

using ModelContextProtocol.Client;

var httpClientTransport = new HttpClientTransport(new HttpClientTransportOptions()
{
    Endpoint = new Uri("http://127.0.0.1:53903/mcp")
});

var mcpClient = await McpClient.CreateAsync(httpClientTransport);
var toolList = await mcpClient.ListToolsAsync();
foreach (var mcpClientTool in toolList)
{
    Console.WriteLine(mcpClientTool.Name);
}

Console.WriteLine("Hello, World!");
