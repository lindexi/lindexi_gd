namespace HiwelairchawCekaywebeale;

internal class Program
{
    static async Task Main(string[] args)
    {
        var host = "unknownaddressxxxxxxxxxxxasdxx.xxxxxx";

        using var testHost = TestHostBuilder.GetTestHost(app =>
        {
            app.MapPost("/", () =>
            {
                var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();

                // 测试可以拿到域名
                if (httpContextAccessor.HttpContext!.Request.Host.Host == host)
                {
                    return "Return";
                }

                return null;
            });
        });

        var httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.Headers.Host = host;
        httpRequestMessage.Method = HttpMethod.Post;
        httpRequestMessage.RequestUri = new Uri(testHost.Host);

        using var httpClient = new HttpClient();
        var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        var result = await httpResponseMessage.Content.ReadAsStringAsync();
        Console.WriteLine(result);
    }
}