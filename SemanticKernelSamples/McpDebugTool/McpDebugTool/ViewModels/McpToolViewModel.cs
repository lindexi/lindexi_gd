using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using ModelContextProtocol.Client;

namespace McpDebugTool.ViewModels;

public sealed class McpToolViewModel : ObservableObject
{
    private bool _isExpanded;

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

    public ObservableCollection<McpToolParameterViewModel> Parameters { get; }

    public McpClientTool? Tool { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool HasParameters => Parameters.Count > 0;

    public bool HasNoParameters => !HasParameters;

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

    private static IEnumerable<McpToolParameterViewModel> CreateParameters(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object || !schema.TryGetProperty("properties", out JsonElement properties) || properties.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        HashSet<string> requiredSet = [];
        if (schema.TryGetProperty("required", out JsonElement requiredElement) && requiredElement.ValueKind == JsonValueKind.Array)
        {
            requiredSet = requiredElement
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.Ordinal);
        }

        return properties.EnumerateObject()
            .Select(property => new McpToolParameterViewModel(property.Name, property.Value.Clone(), requiredSet.Contains(property.Name)))
            .OrderBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
