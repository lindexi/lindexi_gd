// See https://aka.ms/new-console-template for more information

using System.Net.Http.Json;

var url = "http://127.0.0.1:5017";

var httpClient = new HttpClient();

var chatRequest = new ChatRequest("Hello");

var httpRequestMessage = new HttpRequestMessage();
httpRequestMessage.Content = JsonContent.Create(chatRequest);
httpRequestMessage.Method = HttpMethod.Post;
httpRequestMessage.RequestUri = new Uri($"{url}/Chat");

using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseHeadersRead);

using var stream = await httpResponseMessage.Content.ReadAsStreamAsync();
using var streamReader = new StreamReader(stream);
var buffer = new char[1];

while (streamReader.ReadBlock(buffer.AsSpan()) > 0)
{
    Console.Write(buffer[0]);
}

record ChatRequest(string Prompt)
{
}