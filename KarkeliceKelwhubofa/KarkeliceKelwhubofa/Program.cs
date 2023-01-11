// See https://aka.ms/new-console-template for more information

using System.Net;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);

var socketsHttpHandler = new SocketsHttpHandler();
var httpClient = new HttpClient(socketsHttpHandler)
{
    //DefaultRequestVersion = HttpVersion.Version20
};
var response = await httpClient.GetAsync("https://learn.microsoft.com");
await response.Content.ReadAsStringAsync();

Console.WriteLine("Hello, World!");
