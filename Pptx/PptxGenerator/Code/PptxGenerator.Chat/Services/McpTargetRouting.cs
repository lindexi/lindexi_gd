using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetCampus.ModelContextProtocol.Protocol.Messages;

namespace PptxGenerator.Chat.Services;

internal sealed record McpTargetBinding(string Address, string Id);

internal sealed class McpTargetRouting
{
    internal const string ListTool = "ListOpenCourseware";
    internal const string BindTool = "BindCoursewareTarget";

    internal static Tool? FindTool(IEnumerable<Tool> tools, string nameFragment)
    {
        var matches = tools.Where(tool => tool.Name.Contains(nameFragment, StringComparison.OrdinalIgnoreCase))
            .Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
    internal const string TargetParameter = "shellId";
    private readonly HashSet<string> _listed = new(StringComparer.Ordinal);
    private Uri? _listAddress;
    internal McpTargetBinding? Binding { get; private set; }

    internal void Restore(McpTargetBinding? binding)
    {
        Binding = binding;
        _listAddress = null;
        _listed.Clear();
    }

    internal async Task<CallToolResult> InvokeAsync(Uri address, string name, bool routed, JsonElement arguments,
        Func<string, JsonElement?, CancellationToken, Task<CallToolResult>> invokeAsync, CancellationToken token)
    {
        if (name == ListTool)
        {
            var result = await invokeAsync(name, JsonSerializer.SerializeToElement(new { searchingMode = "All" }), token).ConfigureAwait(false);
            if (result.IsError != true)
            {
                _listAddress = address;
                _listed.Clear();
                foreach (var id in ReadIds(result)) _listed.Add(id);
            }
            return result;
        }

        if (name == BindTool)
        {
            if (arguments.ValueKind != JsonValueKind.Object || !arguments.TryGetProperty("targetId", out var target) ||
                target.ValueKind != JsonValueKind.String || target.GetString() is not { } id ||
                _listAddress != address || !_listed.Contains(id))
                return CallToolResult.FromError($"请先调用 {ListTool} 列出所有打开课件，再使用返回的目标 ID 调用 {BindTool} 绑定操作区。");

            var result = await invokeAsync(name, JsonSerializer.SerializeToElement(new { shellId = id }), token).ConfigureAwait(false);
            if (result.IsError != true) Binding = new McpTargetBinding(address.AbsoluteUri, id);
            return result;
        }

        if (routed)
        {
            if (Binding is not { } binding || binding.Address != address.AbsoluteUri)
                return CallToolResult.FromError($"当前会话尚未绑定操作区。请先调用 {ListTool} 列出所有打开课件，再使用目标 ID 调用 {BindTool}，随后重试当前工具。");

            var routedArguments = JsonObject.Create(arguments);
            if (routedArguments is null) return CallToolResult.FromError("工具参数应为 JSON 对象，请按工具定义重新调用。");
            routedArguments[TargetParameter] = binding.Id;
            return await invokeAsync(name, JsonSerializer.SerializeToElement(routedArguments), token).ConfigureAwait(false);
        }

        return await invokeAsync(name, arguments, token).ConfigureAwait(false);
    }

    private static IEnumerable<string> ReadIds(CallToolResult result)
    {
        if (result.StructuredContent is { } structured && TryProperty(structured, "shells", out _))
            return ReadIds(structured);
        foreach (var block in result.Content.OfType<TextContentBlock>())
        {
            try
            {
                using var json = JsonDocument.Parse(block.Text);
                if (TryProperty(json.RootElement, "shells", out _)) return ReadIds(json.RootElement);
            }
            catch (JsonException) { continue; }
        }
        return Array.Empty<string>();
    }

    private static string[] ReadIds(JsonElement root)
    {
        if (!TryProperty(root, "shells", out var shells) || shells.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();
        var ids = new List<string>();
        foreach (var shell in shells.EnumerateArray())
            if (TryProperty(shell, "id", out var id) && id.ValueKind == JsonValueKind.String &&
                id.GetString() is { Length: > 0 } value) ids.Add(value);
        return ids.ToArray();
    }

    private static bool TryProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject())
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
        value = default;
        return false;
    }
}
