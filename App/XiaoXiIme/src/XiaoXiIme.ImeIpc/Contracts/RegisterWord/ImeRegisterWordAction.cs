using System.Text.Json.Serialization;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

[JsonConverter(typeof(JsonCamelCaseStringEnumConverter<ImeRegisterWordAction>))]
public enum ImeRegisterWordAction
{
    Register,
    Unregister,
    Enumerate,
}