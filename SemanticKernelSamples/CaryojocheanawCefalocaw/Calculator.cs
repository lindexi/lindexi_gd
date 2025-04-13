using MCPSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaryojocheanawCefalocaw;

public class Calculator
{
    [McpTool("add", "Adds two numbers")]  // Note: [McpFunction] is deprecated, use [McpTool] instead
    public static int Add([McpParameter(true)] int a, [McpParameter(true)] int b)
    {
        return a + b;
    }
}