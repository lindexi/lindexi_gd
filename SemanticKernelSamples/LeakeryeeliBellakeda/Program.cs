// See https://aka.ms/new-console-template for more information

using Microsoft.SemanticKernel;

var keyFile = @"C:\lindexi\CA\Key";
var key = File.ReadAllText(keyFile);

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion("gpt-4o", endpoint: "https://lindexi.openai.azure.com/", apiKey: key)
    .Build();

while (true)
{
    var text  = Console.ReadLine();

    if (string.IsNullOrEmpty(text))
    {
        break;
    }

    await foreach (var streamingKernelContent in kernel.InvokePromptStreamingAsync(text))
    {
        Console.Write(streamingKernelContent.ToString());
    }
}

Console.Read();

Console.WriteLine("Hello, World!");
