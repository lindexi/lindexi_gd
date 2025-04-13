using MCPSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaryojocheanawCefalocaw;
internal class ModelContextProtocolTest
{
    public  async Task RunAsync()
    {
        await MCPServer.StartAsync("CalculatorServer", "1.0.0");
    }
}
