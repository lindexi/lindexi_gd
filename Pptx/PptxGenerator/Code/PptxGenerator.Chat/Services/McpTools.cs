using System.Text;
using System.Text.Json;
using DotNetCampus.ModelContextProtocol.Clients;
using DotNetCampus.ModelContextProtocol.Protocol.Messages;
using OpenAI.Responses;

namespace PptxGenerator.Chat.Services;

internal sealed record ChatTool(ResponseTool Definition, string Label, bool Remote,
    Func<FunctionCallResponseItem, CancellationToken, Task<ToolResult>> InvokeAsync);

internal sealed record ToolImage(string MediaType, string Base64);
internal sealed record ToolResult(FunctionCallOutputResponseItem Output, bool IsError = false,
    string? DisplayText = null, IReadOnlyList<ToolImage>? Images = null);
internal sealed record ToolActivity(string Label, string Name, string Arguments, string State, string Result,
    IReadOnlyList<ToolImage>? Images = null);

internal sealed class McpTools
{
    private static readonly IReadOnlyDictionary<string, (string ModelName, string LabelKey)> Allowed =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["RenderSlideML"] = ("RenderSlideML", "ToolRender"),
            ["get_slide_info"] = ("GetSlideInfo", "ToolSlide"),
            ["get_slide_screenshot"] = ("GetSlideScreenshot", "ToolScreenshot"),
            ["Register_SlideML_Resource"] = ("Register_SlideML_Resource", "ToolResource"),
            ["export_courseware_markdown"] = ("ExportCoursewareMarkdown", "ToolExport"),
            ["get_current_courseware_info"] = ("GetCurrentCoursewareInfo", "ToolCoursewareInfo"),
        };

    private readonly McpTargetRouting _routing;
    private McpClient? _client;

    internal McpTools(McpTargetRouting routing) => _routing = routing;

    private static McpClient CreateClient(Uri address) =>
        new McpClientBuilder("PptxGenerator.Chat", "1.0.0").WithHttp(address.AbsoluteUri).Build();

    internal static async Task<bool> ProbeAsync(Uri address)
    {
        var probe = new McpTools(new McpTargetRouting());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        try
        {
            probe._client = CreateClient(address);
            await probe._client.EnsureConnectedAsync(timeout.Token).ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            ChatDiagnostics.Failure("ProbeMcp", exception, "Host", address.Host);
            return false;
        }
        finally
        {
            await probe.CloseAsync().ConfigureAwait(false);
        }
    }

    internal async Task<IReadOnlyDictionary<string, ChatTool>> DiscoverAsync(Uri address, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        _client = CreateClient(address);
        var discovered = new Dictionary<string, Tool>(StringComparer.Ordinal);
        string? cursor = null;
        do
        {
            var page = await _client.ListToolsAsync(cursor, timeout.Token).ConfigureAwait(false);
            foreach (var tool in page.Tools) discovered.TryAdd(tool.Name, tool);
            cursor = page.NextCursor;
        } while (!string.IsNullOrEmpty(cursor));

        var list = McpTargetRouting.FindTool(discovered.Values, "find_shell");
        var bind = McpTargetRouting.FindTool(discovered.Values, "bind_shell");
        var tools = new Dictionary<string, ChatTool>(StringComparer.Ordinal);
        if (list is not null && bind is not null)
        {
            Register(list, McpTargetRouting.ListTool, "列出所有打开课件", isTargetTool: true);
            Register(bind, McpTargetRouting.BindTool, "绑定操作区", isTargetTool: true);
        }
        foreach (var tool in discovered.Values)
            if (Allowed.TryGetValue(tool.Name, out var entry)) Register(tool, entry.ModelName, UiText.Get(entry.LabelKey));
        return tools;

        void Register(Tool tool, string name, string label, bool isTargetTool = false)
        {
            var schema = System.Text.Json.Nodes.JsonObject.Create(tool.InputSchema);
            bool routed = !isTargetTool &&
                schema?["properties"] is System.Text.Json.Nodes.JsonObject properties && properties.Remove(McpTargetRouting.TargetParameter);
            var (parameters, description) = name switch
            {
                McpTargetRouting.ListTool => ("{\"type\":\"object\",\"properties\":{}}", "列出所有打开的课件和目标 ID。绑定操作区前先调用此工具。"),
                McpTargetRouting.BindTool => ("{\"type\":\"object\",\"properties\":{\"targetId\":{\"type\":\"string\"}},\"required\":[\"targetId\"]}",
                    $"使用 {McpTargetRouting.ListTool} 返回的目标 ID 绑定或切换本会话的课件操作区。"),
                _ => (schema?.ToJsonString() ?? tool.InputSchema.GetRawText(), routed
                    ? $"{tool.Description} 使用当前会话已绑定的操作区；未绑定时先调用 {McpTargetRouting.ListTool}，再调用 {McpTargetRouting.BindTool}。"
                    : tool.Description),
            };
            var definition = ResponseTool.CreateFunctionTool(name, BinaryData.FromString(parameters), false, description);
            tools.TryAdd(name, new ChatTool(definition, label, true,
                (call, ct) => InvokeAsync(address, name, tool.Name, routed, call, ct)));
        }
    }

    private async Task<ToolResult> InvokeAsync(Uri address, string name, string remoteName, bool routed, FunctionCallResponseItem call, CancellationToken token)
    {
        using var arguments = JsonDocument.Parse(call.FunctionArguments.ToMemory());
        var result = await _routing.InvokeAsync(address, name, routed, arguments.RootElement,
            (_, args, ct) => _client!.CallToolAsync(remoteName, args, ct), token).ConfigureAwait(false);
        return ConvertResult(call.CallId, result);
    }

    internal static ToolResult ConvertResult(string callId, CallToolResult result)
    {
        var images = result.Content.OfType<ImageContentBlock>()
            .Select(image => new ToolImage(image.MimeType, image.Data)).ToArray();
        var otherContent = result.Content.Where(block => block is not ImageContentBlock).ToArray();
        bool isError = result.IsError == true;
        bool imageOnly = images.Length > 0 && !isError && result.StructuredContent is null && otherContent.Length == 0;
        var text = imageOnly ? string.Empty : JsonSerializer.Serialize(new
        {
            isError,
            structuredContent = result.StructuredContent,
            content = otherContent,
        });
        var output = ResponseItem.CreateFunctionCallOutputItem(callId, text);
        if (images.Length == 0) return new ToolResult(output, isError);

        var parts = new List<object>();
        if (!imageOnly) parts.Add(new { type = "input_text", text });
        foreach (var image in images)
            parts.Add(new { type = "input_image", image_url = $"data:{image.MediaType};base64,{image.Base64}" });
        output.Patch.Set(Encoding.UTF8.GetBytes("$.output"), BinaryData.FromObjectAsJson(parts));
        return new ToolResult(output, isError, text, images);
    }

    internal async Task CloseAsync()
    {
        if (_client is null) return;
        try
        {
            await _client.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            ChatDiagnostics.Failure("CloseMcp", exception, "Client", "MCP");
        }
    }
}
