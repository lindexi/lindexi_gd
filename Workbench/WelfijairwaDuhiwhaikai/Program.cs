// See https://aka.ms/new-console-template for more information

using System.Net;

var httpClient = new HttpClient(new FakeMessage())
{
    BaseAddress = new Uri("http://baidu.com/foo/api/v1"),
};

var response1 = await httpClient.GetAsync("bar/aswqwe/q123");
var response2 = await httpClient.GetAsync("/bar/aswqwe/q123");



Console.WriteLine("Hello, World!");


class FakeMessage : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine(request.RequestUri);

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Hello, World!")
        });
    }
}