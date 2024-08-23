// See https://aka.ms/new-console-template for more information

using System.Net.Http.Json;

var foo = new Foo();

var jsonContent = JsonContent.Create(foo);
foreach (var jsonContentHeader in jsonContent.Headers)
{
    Console.WriteLine(jsonContentHeader.Key);
}

var httpClient = new HttpClient();
await httpClient.PostAsync("http://www.baidu.com", jsonContent);

await jsonContent.LoadIntoBufferAsync();

foreach (var jsonContentHeader in jsonContent.Headers)
{
    Console.WriteLine(jsonContentHeader.Key);
}

Console.WriteLine("Hello, World!");

class Foo
{
    public int Value { set; get; }
}