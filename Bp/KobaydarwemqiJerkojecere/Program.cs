// See https://aka.ms/new-console-template for more information

using System.Net.Http.Json;

var url = "http://172.20.114.91:5017/Chat";

var httpClient = new HttpClient();

while (true)
{
    Console.WriteLine($"请输入聊天内容");
    var prompt = Console.ReadLine();
    prompt = $@"<|user|>{prompt}<|end|><|assistant|>";
    var chatRequest = new ChatRequest(prompt);

    var responseMessage = await httpClient.PostAsJsonAsync(url, chatRequest);
    var responseText = await responseMessage.Content.ReadAsStringAsync();
    Console.WriteLine(responseText);
}

record ChatRequest(string Prompt);