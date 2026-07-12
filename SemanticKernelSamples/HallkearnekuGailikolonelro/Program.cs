
using System.ClientModel;
using OpenAI;

var key = await File.ReadAllTextAsync(@"C:\lindexi\Work\Key\OpenAI.txt");
var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://model." +
                       "server." +
                       "lindexi.com" +
                       "/v1")
});

var modelClient = openAiClient.GetOpenAIModelClient();
var clientResult = await modelClient.GetModelsAsync();
foreach (var model in clientResult.Value)
{
    Console.WriteLine(model.Id);
}
Console.WriteLine("Hello, World!");
