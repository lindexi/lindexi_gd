// See https://aka.ms/new-console-template for more information
using OllamaSharp;
using OllamaSharp.Models.Chat;

using System.Text.Json;
using System.Text;

var uri = new Uri("http://localhost:11434");
var ollama = new OllamaApiClient(uri);

var models = await ollama.ListLocalModels();

ollama.SelectedModel = models.First().Name;

while (true)
{

    var input = "Hello";
    var chatRequest = new ChatRequest()
    {
        Messages = new List<Message>()
        {
            new Message(ChatRole.User, input)
        },
        Model = ollama.SelectedModel,
        Stream = false,
    };

    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
    {
        Content = new StringContent(JsonSerializer.Serialize(chatRequest), Encoding.UTF8, "application/json")
    };
    var httpClient = new HttpClient()
    {
        BaseAddress = uri
    };
    var response = await httpClient.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();

    var messages = await ollama.SendChat(chatRequest, stream =>
    {
        if (!stream.Done)
        {
            Console.WriteLine(stream.Message.Content);
        }
    });
}


//foreach (var message in messages)
//{
//    Console.WriteLine(message.Content);
//}

Console.WriteLine("Hello, World!");
