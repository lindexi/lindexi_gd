using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using ModelContextProtocol.Client;

namespace McpDebugTool.ViewModels;

public sealed class McpToolViewModel : ObservableObject
{
    private CancellationTokenSource? _callCancellationTokenSource;
    private bool _isCalling;
    private string _callStatus = "尚未调用。";
    private string _resultText = "请选择参数后调用工具，结果会显示在这里。";

    public McpToolViewModel(McpClientTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        Tool = tool;
        Name = tool.Name;
        Description = tool.Description;
        Parameters = new ObservableCollection<McpToolParameterViewModel>(CreateParameters(tool.JsonSchema));
    }

    public McpToolViewModel(string name, string? description, JsonElement schema)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        Description = description;
        Parameters = new ObservableCollection<McpToolParameterViewModel>(CreateParameters(schema));
    }

    public string Name { get; }

    public string? Description { get; }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public ObservableCollection<McpToolParameterViewModel> Parameters { get; }

    public McpClientTool? Tool { get; }

    public bool IsCalling
    {
        get => _isCalling;
        private set => SetProperty(ref _isCalling, value);
    }

    public bool HasParameters => Parameters.Count > 0;

    public bool HasNoParameters => !HasParameters;

    public string CallStatus
    {
        get => _callStatus;
        private set => SetProperty(ref _callStatus, value);
    }

    public string ResultText
    {
        get => _resultText;
        private set => SetProperty(ref _resultText, value);
    }

    public IReadOnlyDictionary<string, object?> BuildArguments()
    {
        Dictionary<string, object?> arguments = new(StringComparer.Ordinal);

        foreach (McpToolParameterViewModel parameter in Parameters)
        {
            object? value = parameter.GetValue();
            if (value is not null)
            {
                arguments[parameter.Name] = value;
            }
        }

        return arguments;
    }

    public CancellationToken StartInvocation()
    {
        if (IsCalling)
        {
            throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, "工具 {0} 正在调用中。", Name));
        }

        _callCancellationTokenSource = new CancellationTokenSource();
        IsCalling = true;
        CallStatus = "调用中...";
        return _callCancellationTokenSource.Token;
    }

    public void CancelInvocation()
    {
        _callCancellationTokenSource?.Cancel();
    }

    public void CompleteInvocation(string status, string resultText)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(resultText);

        _callCancellationTokenSource?.Dispose();
        _callCancellationTokenSource = null;
        IsCalling = false;
        CallStatus = status;
        ResultText = resultText;
    }

    private static IEnumerable<McpToolParameterViewModel> CreateParameters(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object || !schema.TryGetProperty("properties", out JsonElement properties) || properties.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        HashSet<string> requiredSet = [];
        if (schema.TryGetProperty("required", out JsonElement requiredElement) && requiredElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in requiredElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string? name = item.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        requiredSet.Add(name);
                    }
                }
            }
        }

        return properties.EnumerateObject()
            .Select(property => new McpToolParameterViewModel(property.Name, property.Value.Clone(), requiredSet.Contains(property.Name)))
            .OrderBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
