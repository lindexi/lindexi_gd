// See https://aka.ms/new-console-template for more information
using OllamaSharp;
using OllamaSharp.Models.Chat;

var uri = new Uri("http://localhost:11434");
var ollama = new OllamaApiClient(uri);

var models = await ollama.ListLocalModels();

ollama.SelectedModel = models.First().Name;

while (true)
{
    var input = Console.ReadLine();
    var chatRequest = new ChatRequest()
    {
        Messages = new List<Message>()
        {
            new Message(ChatRole.User, input)
        },
        Stream = false,
    };
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
