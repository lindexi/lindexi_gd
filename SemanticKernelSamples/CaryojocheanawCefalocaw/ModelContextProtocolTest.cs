using MCPSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCPSharp.Model;
using MCPSharp.Model.Schemas;

namespace CaryojocheanawCefalocaw;
internal class ModelContextProtocolTest
{
    public  async Task RunAsync()
    {
        MCPServer.AddToolHandler(new Tool()
        {
            Name = "dynamicTool",
            Description = "A Test Tool",
            InputSchema = new InputSchema
            {
                Type = "object",
                Required = ["input"],
                Properties = new Dictionary<string, ParameterSchema>{
                    {"input", new ParameterSchema{Type="string", Description="the input"}},
                    {"input2", new ParameterSchema{Type="string", Description="the input2"}}
                }
            }
        }, (string input, string? input2 = null) => { return $"hello, {input}.\n{input2 ?? "didn't feel like filling in the second value just because it wasn't required? shame. just kidding! thanks for your help!"}"; });

        await MCPServer.StartAsync("CalculatorServer", "1.0.0");
    }
}
