using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace HttpWebClients.Configurations;

class MergedJsonSerializerContext : JsonSerializerContext
{
    public MergedJsonSerializerContext(JsonSerializerOptions? options) : base(options)
    {
        GeneratedSerializerOptions = options;
    }

    public override JsonTypeInfo? GetTypeInfo(Type type)
    {
        foreach (var jsonSerializerContext in JsonSerializerContextList)
        {
            if (jsonSerializerContext.GetTypeInfo(type) is { } typeInfo)
            {
                return typeInfo;
            }
        }

        return null;
    }

    public void Add(JsonSerializerContext context)
    {
        JsonSerializerContextList.Add(context);
    }

    public void AddRange(IEnumerable<JsonSerializerContext> context) => JsonSerializerContextList.AddRange(context);

    private List<JsonSerializerContext> JsonSerializerContextList { get; } = [];

    protected override JsonSerializerOptions? GeneratedSerializerOptions { get; }
}