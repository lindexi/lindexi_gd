// See https://aka.ms/new-console-template for more information

using System.Net.Http.Json;

var url = "http://172.20.114.91:5017";

var httpClient = new HttpClient();



while (true)
{
    Console.WriteLine($"请输入聊天内容");
    var prompt = Console.ReadLine();
    if (string.IsNullOrEmpty(prompt))
    {
        continue;
    }

    var chatRequest = new ChatRequest(prompt);

    var httpRequestMessage = new HttpRequestMessage();
    httpRequestMessage.Content = JsonContent.Create(chatRequest);
    httpRequestMessage.Method = HttpMethod.Post;
    httpRequestMessage.RequestUri = new Uri($"{url}/Chat");

    using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);

    using var stream = await httpResponseMessage.Content.ReadAsStreamAsync();
    using var streamReader = new StreamReader(stream);
    var buffer = new char[1];

    while (streamReader.ReadBlock(buffer.AsSpan()) > 0)
    {
        Console.Write(buffer[0]);
    }

    Console.WriteLine();
    Console.WriteLine("聊天完成");
    Console.WriteLine();
}

record ChatRequest(string Prompt)
{
}