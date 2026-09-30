using System.Text.Json;
using System.Text.Json.Serialization;

namespace XiaoXiIme.Foundation;

/// <summary>
/// Writes enum members as camelCase strings for AOT JSON source generation.
/// The converter is public and has a parameterless constructor so other assemblies
/// can list it in <c>JsonSourceGenerationOptions.Converters</c>.
/// </summary>
public sealed class JsonCamelCaseStringEnumConverter : JsonStringEnumConverter
{
    public JsonCamelCaseStringEnumConverter()
        : base(JsonNamingPolicy.CamelCase)
    {
    }
}
