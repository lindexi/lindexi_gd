// See https://aka.ms/new-console-template for more information

using MCPSharp;
using Microsoft.Extensions.AI;

MCPClient client = new MCPClient("AIClient", "1.0",
    @"C:\lindexi\Code\empty\SemanticKernelSamples\CaryojocheanawCefalocaw\bin\Debug\net9.0\CaryojocheanawCefalocaw.exe");
IList<AIFunction> functions = await client.GetFunctionsAsync();

foreach (AIFunction function in functions)
{

}
