using System.Text.Json.Serialization;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>
/// Identifies whether a conversion-list request looks up candidates by reading or readings by text.
/// </summary>
[JsonConverter(typeof(JsonCamelCaseStringEnumConverter))]
public enum ImeConversionListKind
{
    Conversion,
    ReverseConversion,
}
