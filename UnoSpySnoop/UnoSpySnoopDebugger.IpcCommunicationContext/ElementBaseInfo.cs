using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnoSpySnoopDebugger.IpcCommunicationContext;

public record ElementBaseInfo(string Token, string ElementType, string? ElementName)
{
}