using System.Net;

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
Console.WriteLine($"SecurityProtocol: {ServicePointManager.SecurityProtocol}");

var urls = new[] { "https://cn.bing.com/", "https://api.steampp.net/" };

foreach (var url in urls)
{
    try
    {
        using var client = new HttpClient(new WinHttpHandler());
        client.DefaultRequestVersion = HttpVersion.Version20;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        var htmlStr = await client.GetStringAsync(url);
        Console.WriteLine("Http2 OK");
        Console.WriteLine($"len: {htmlStr.Length}");
    }
    catch (Exception e)
    {
        Console.WriteLine("Http2 Error: ");
        Console.WriteLine(e);
    }

    try
    {
        using var client = new HttpClient(new WinHttpHandler());
        client.DefaultRequestVersion = HttpVersion.Version11;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        var htmlStr = await client.GetStringAsync(url);
        Console.WriteLine("Http11 OK");
        Console.WriteLine($"len: {htmlStr.Length}");
    }
    catch (Exception e)
    {
        Console.WriteLine("Http11 Error: ");
        Console.WriteLine(e);
    }
}

Console.ReadLine();