// See https://aka.ms/new-console-template for more information

using var httpClient = new HttpClient();
using var httpResponseMessage = await httpClient.GetAsync("http://127.0.0.1:7799/success");
var responseText = await httpResponseMessage.Content.ReadAsStringAsync();

Console.WriteLine("Hello, World!");
