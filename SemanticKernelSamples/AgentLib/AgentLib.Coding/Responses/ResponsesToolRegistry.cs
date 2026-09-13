using System.Text.Json;
using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace AgentLib.Coding.Responses;

internal sealed class ResponsesToolRegistry
{
    private readonly IReadOnlyDictionary<string, Entry> _entries;

    public ResponsesToolRegistry(IEnumerable<ToolRegistration> registrations)
    {
        _entries = registrations
            .Select(CreateEntry)
            .ToDictionary(entry => entry.Function.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<ResponseTool> Tools => _entries.Values.Select(entry => (ResponseTool)entry.Tool).ToArray();

    public ToolCallPresentation? CreatePresentation(FunctionCallContent call)
    {
        return _entries.TryGetValue(call.Name, out Entry? entry)
            ? entry.Registration.CreatePresentation?.Invoke(call.Arguments ?? new Dictionary<string, object?>())
            : null;
    }

    public async Task<object?> InvokeAsync(
        string functionName,
        BinaryData arguments,
        CancellationToken cancellationToken)
    {
        if (!_entries.TryGetValue(functionName, out Entry? entry))
        {
            return $"未找到工具：{functionName}";
        }

        Dictionary<string, object?> values = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            arguments.ToString(),
            entry.Function.JsonSerializerOptions) ?? [];
        var functionArguments = new AIFunctionArguments(values!);
        try
        {
            return await entry.Function.InvokeAsync(functionArguments, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return $"工具执行失败：{exception.Message}";
        }
    }

    private static Entry CreateEntry(ToolRegistration registration)
    {
        if (registration.Tool is not AIFunction function)
        {
            throw new NotSupportedException($"Responses API 仅支持函数工具：{registration.Tool.Name}");
        }

        return new Entry(registration, function, function.AsOpenAIResponseTool());
    }

    private sealed record Entry(
        ToolRegistration Registration,
        AIFunction Function,
        FunctionTool Tool);
}
