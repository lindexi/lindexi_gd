// See https://aka.ms/new-console-template for more information
using OllamaSharp;
using OllamaSharp.Models.Chat;

using System.Text.Json;
using System.Text;
using OllamaSharp.Models;

var uri = new Uri("http://localhost:11434");
var ollama = new OllamaApiClient(uri);

var models = await ollama.ListLocalModels();

var model = models.FirstOrDefault();
if (model == null)
{
    await ollama.PullModel("llama3:8b", status =>
    {
        Console.WriteLine(status.Status);
    });
}
else
{
    models = await ollama.ListLocalModels();
    model = models.First();
}

ollama.SelectedModel = model.Name;

while (true)
{
    Console.Write("请输入你的对话内容：");
    var input = Console.ReadLine();
    var chatRequest = new ChatRequest()
    {
        Messages = new List<Message>()
        {
            new Message(ChatRole.User, input)
        },
        Model = ollama.SelectedModel,
        Stream = false,
    };

    var messages = await ollama.SendChat(chatRequest, stream =>
    {
        Console.WriteLine(stream.Message.Content);
    });
}


//foreach (var message in messages)
//{
//    Console.WriteLine(message.Content);
//}

Console.WriteLine("Hello, World!");
