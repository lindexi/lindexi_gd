using System.Text.Json.Serialization;

namespace XiaoXiIme.Foundation;

[JsonConverter(typeof(JsonCamelCaseStringEnumConverter))]
public enum ImeGuidelineLevel
{
    None,
    Info,
    Warning,
    Error,
    Reading,
    NoCandidate,
    InvalidInput
}
