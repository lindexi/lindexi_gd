// See https://aka.ms/new-console-template for more information

using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

using Azure.Core;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ImageGeneration;

var httpClient = new HttpClient()
{
    BaseAddress = new Uri("https://lindexi.openai.azure.com")
};
var azureOpenAiImageGeneration = new AzureOpenAIImageGeneration(args[0], httpClient, logger: new Logger());

var generateImageAsync = await azureOpenAiImageGeneration.GenerateImageAsync("一可以豆子", 1024, 1024);

httpClient.DefaultRequestHeaders.Add("api-key", args[0]);

var stringContent = new StringContent("{\r\n\r\n    \"prompt\": \"USER_PROMPT_GOES_HERE\",\r\n\r\n    \"n\": 1,\r\n\r\n    \"size\": \"1024x1024\"\r\n\r\n}", Encoding.UTF8, MediaTypeNames.Application.Json);

var httpResponseMessage = await httpClient.PostAsync("https://lindexi.openai.azure.com/openai/images/generations:submit?api-version=2023-06-01-preview", stringContent);
var text = await httpResponseMessage.Content.ReadAsStringAsync();

var builder = new KernelBuilder();

builder.WithAzureChatCompletionService("GPT35",                  // Azure OpenAI Deployment Name
    "https://lindexi.openai.azure.com/", // Azure OpenAI Endpoint
    args[0]);      // Azure OpenAI Key

//builder.WithAzureTextCompletionService(
//    "GPT35",                  // Azure OpenAI Deployment Name
//    "https://lindexi.openai.azure.com/", // Azure OpenAI Endpoint
//    args[0]);      // Azure OpenAI Key

// Alternative using OpenAI
//builder.WithOpenAITextCompletionService(
//         "text-davinci-003",               // OpenAI Model name
//         "...your OpenAI API Key...");     // OpenAI API Key
builder.WithLogger(new Logger());

var kernel = builder.Build();

var prompt = @"{{$input}}

One line TLDR with the fewest words.";

var summarize = kernel.CreateSemanticFunction(prompt);

string text1 = @"
1st Law of Thermodynamics - Energy cannot be created or destroyed.
2nd Law of Thermodynamics - For a spontaneous process, the entropy of the universe increases.
3rd Law of Thermodynamics - A perfect crystal at zero Kelvin has zero entropy.";

string text2 = @"
1. An object at rest remains at rest, and an object in motion remains in motion at constant speed and in a straight line unless acted on by an unbalanced force.
2. The acceleration of an object depends on the mass of the object and the amount of force applied.
3. Whenever one object exerts a force on another object, the second object exerts an equal and opposite on the first.";

var result = await summarize.InvokeAsync(text1);
Console.WriteLine(result);

Console.WriteLine(await summarize.InvokeAsync(text2));

// Output:
//   Energy conserved, entropy increases, zero entropy at 0K.
//   Objects move in response to forces.

Console.WriteLine("Hello, World!");

class Logger : ILogger, IDisposable
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        Console.WriteLine(message);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return this;
    }

    public void Dispose()
    {
    }
}