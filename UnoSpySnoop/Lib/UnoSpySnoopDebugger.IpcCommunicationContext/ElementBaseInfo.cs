using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnoSpySnoopDebugger.IpcCommunicationContext;

public class ElementBaseInfo(string token, string elementTypeName, string elementTypeFullName, string? elementName)
{
    public string Token { get; init; } = token;
    public string ElementTypeName { get; init; } = elementTypeName;
    public string ElementTypeFullName { get; init; } = elementTypeFullName;
    public string? ElementName { get; init; } = elementName;
}
