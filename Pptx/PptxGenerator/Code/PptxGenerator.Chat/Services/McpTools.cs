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
    private static readonly IReadOnlyDictionary<string, string> Allowed = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["RenderSlideML"] = "ToolRender",
        ["get_slide_info"] = "ToolSlide",
        ["get_slide_screenshot"] = "ToolScreenshot",
        ["Register_SlideML_Resource"] = "ToolResource",
        ["export_courseware_markdown"] = "ToolExport",
    };

    private McpClient? _client;

    internal async Task<IReadOnlyDictionary<string, ChatTool>> DiscoverAsync(Uri address, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        _client = new McpClientBuilder("PptxGenerator.Chat", "1.0.0").WithHttp(address.AbsoluteUri).Build();
        var tools = new Dictionary<string, ChatTool>(StringComparer.Ordinal);
        string? cursor = null;
        do
        {
            var page = await _client.ListToolsAsync(cursor, timeout.Token).ConfigureAwait(false);
            foreach (var tool in page.Tools)
            {
                if (!Allowed.TryGetValue(tool.Name, out var label)) continue;
                var definition = ResponseTool.CreateFunctionTool(tool.Name,
                    BinaryData.FromString(tool.InputSchema.GetRawText()), false, tool.Description);
                tools.TryAdd(tool.Name, new ChatTool(definition, UiText.Get(label), true, InvokeAsync));
            }
            cursor = page.NextCursor;
        } while (!string.IsNullOrEmpty(cursor));
        return tools;
    }

    private async Task<ToolResult> InvokeAsync(FunctionCallResponseItem call, CancellationToken token)
    {
        using var arguments = JsonDocument.Parse(call.FunctionArguments.ToMemory());
        var result = await _client!.CallToolAsync(call.FunctionName, arguments.RootElement, token).ConfigureAwait(false);
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
