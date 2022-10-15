// See https://aka.ms/new-console-template for more information

using System.Net;
using NilerlanaihikaWhurreeberhalur.Proxy;

#pragma warning disable CA1416 // Validate platform compatibility
var dynamicHttpWindowsProxy = new DynamicHttpWindowsProxy();

HttpClient.DefaultProxy = dynamicHttpWindowsProxy;

LewheawelwairFelobearja();

var socketsHttpHandler = new SocketsHttpHandler();
var httpClient = new HttpClient(socketsHttpHandler);

while (true)
{
    try
    {
        var httpResponseMessage = await httpClient.GetAsync("https://www.baidu.com");
        Console.WriteLine($"https://www.baidu.com {httpResponseMessage.StatusCode}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"https://www.baidu.com {e}");
    }

    Console.ReadLine();
}

async void LewheawelwairFelobearja()
{
    await Task.Delay(5000);
    dynamicHttpWindowsProxy.Start();
}