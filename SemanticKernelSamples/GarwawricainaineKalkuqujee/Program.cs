// See https://aka.ms/new-console-template for more information

using MCPSharp;
using MCPSharp.Model.Results;

using Microsoft.Extensions.AI;

MCPClient client = new MCPClient("AIClient", "1.0",
    @"C:\lindexi\Code\empty\SemanticKernelSamples\CaryojocheanawCefalocaw\bin\Debug\net9.0\CaryojocheanawCefalocaw.exe");
IList<AIFunction> functions = await client.GetFunctionsAsync();

foreach (AIFunction function in functions)
{
    if (function.Name == "add")
    {
        var result = await function.InvokeAsync(
        [
            new KeyValuePair<string, object?>("a", 1),
            new KeyValuePair<string, object?>("b", 2)
        ]);

        if (result is CallToolResult callToolResult)
        {
            var text = callToolResult.Content[0].Text;
            if (int.TryParse(text, out var n))
            {

            }
        }
    }
}
