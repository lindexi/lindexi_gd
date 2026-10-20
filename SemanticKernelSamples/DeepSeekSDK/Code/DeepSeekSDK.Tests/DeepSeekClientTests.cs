using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeepSeekSDK.Tests;

[TestClass]
public sealed class DeepSeekClientTests
{
    [TestMethod]
    public async Task SearchAsyncSendsExpectedRequestAsync()
    {
        using var handler = new StubHandler(async (message, cancellationToken) =>
        {
            var body = await message.Content!.ReadAsStringAsync(cancellationToken);
            var actual = new
            {
                Method = message.Method.Method,
                Url = message.RequestUri!.AbsoluteUri,
                Authorization = message.Headers.Authorization!.ToString(),
                ContentType = message.Content.Headers.ContentType!.MediaType,
                Body = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(body))
            };
            var expected = new
            {
                Method = "POST",
                Url = "https://api.deepseek.com/v1/tools/search",
                Authorization = "Bearer test-key",
                ContentType = (string?)"application/json",
                Body = "{\"query\":\"test\",\"count\":5,\"ori_question\":\"question\",\"localization\":\"1.1.1.1\",\"need_content\":true}"
            };
            Assert.AreEqual(expected, actual);
            return JsonResponse("{\"results\":[]}");
        });
        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekClient(httpClient, "test-key");

        await client.SearchAsync(new DeepSeekSearchRequest
        {
            Query = "test",
            Count = 5,
            OriginalQuestion = "question",
            Localization = "1.1.1.1",
            NeedContent = true
        });
    }

    [TestMethod]
    public async Task SearchAsyncSendsDefaultsAsync()
    {
        using var handler = new StubHandler(async (message, cancellationToken) =>
        {
            using var body = JsonDocument.Parse(await message.Content!.ReadAsStringAsync(cancellationToken));
            Assert.AreEqual(
                (10, JsonValueKind.Null, string.Empty, false),
                (body.RootElement.GetProperty("count").GetInt32(),
                 body.RootElement.GetProperty("ori_question").ValueKind,
                 body.RootElement.GetProperty("localization").GetString(),
                 body.RootElement.GetProperty("need_content").GetBoolean()));
            return JsonResponse("{\"results\":[]}");
        });
        using var httpClient = new HttpClient(handler);
        await new DeepSeekClient(httpClient, "test-key").SearchAsync(new() { Query = "test" });
    }

    [TestMethod]
    public async Task SearchAsyncDeserializesAllResultFieldsAsync()
    {
        const string json = """
            {"results":[{"rerank_score":2.5,"title":"Title","url":"https://example.com/","snippet":"Short","summary":"Long","content":"Body","site_name":"Example","site_icon":"https://example.com/icon.png","date":"2026-01-02T03:04:05Z","authority":4}]}
            """;
        using var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse(json)));
        using var httpClient = new HttpClient(handler);
        var response = await new DeepSeekClient(httpClient, "test-key").SearchAsync(new() { Query = "test" });

        CollectionAssert.AreEqual(new[]
        {
            new DeepSeekSearchResult
            {
                RerankScore = 2.5f, Title = "Title", Url = "https://example.com/",
                Snippet = "Short", Summary = "Long", Content = "Body", SiteName = "Example",
                SiteIcon = "https://example.com/icon.png",
                Date = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), Authority = 4
            }
        }, response.Results.ToArray());
    }

    [TestMethod]
    public async Task SearchAsyncAcceptsNullableResultFieldsAsync()
    {
        const string json = """
            {"results":[{"rerank_score":0,"title":"Title","url":"https://example.com/","snippet":"","summary":"","content":"","site_name":null,"site_icon":null,"date":null,"authority":-1}]}
            """;
        using var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse(json)));
        using var httpClient = new HttpClient(handler);
        var response = await new DeepSeekClient(httpClient, "test-key").SearchAsync(new() { Query = "test" });

        Assert.AreEqual((null, null, null, string.Empty, -1),
            (response.Results[0].SiteName, response.Results[0].SiteIcon,
             response.Results[0].Date, response.Results[0].Content, response.Results[0].Authority));
    }

    [TestMethod]
    public async Task SearchAsyncAcceptsEmptyResultsAsync()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse("{\"results\":[]}")));
        using var httpClient = new HttpClient(handler);
        var response = await new DeepSeekClient(httpClient, "test-key").SearchAsync(new() { Query = "test" });

        Assert.IsEmpty(response.Results);
    }

    [TestMethod]
    [DataRow(null, 0, null)]
    [DataRow("", 51, "")]
    [DataRow(" \t\r\n", -1, " \t")]
    public async Task SearchAsyncSendsParametersWithoutValidationAsync(string? query, int count, string? question)
    {
        using var handler = new StubHandler(async (message, cancellationToken) =>
        {
            using var body = JsonDocument.Parse(await message.Content!.ReadAsStringAsync(cancellationToken));
            Assert.AreEqual((query, count, question),
                (body.RootElement.GetProperty("query").GetString(),
                 body.RootElement.GetProperty("count").GetInt32(),
                 body.RootElement.GetProperty("ori_question").GetString()));
            return JsonResponse("{\"results\":[]}");
        });
        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekClient(httpClient, "test-key");

        await client.SearchAsync(new() { Query = query!, Count = count, OriginalQuestion = question });
    }

    [TestMethod]
    public async Task SearchAsyncSendsLongTextWithoutTruncationAsync()
    {
        var text = string.Concat(Enumerable.Repeat("😀", 129));
        using var handler = new StubHandler(async (message, cancellationToken) =>
        {
            using var body = JsonDocument.Parse(await message.Content!.ReadAsStringAsync(cancellationToken));
            Assert.AreEqual((text, text),
                (body.RootElement.GetProperty("query").GetString(),
                 body.RootElement.GetProperty("ori_question").GetString()));
            return JsonResponse("{\"results\":[]}");
        });
        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekClient(httpClient, "test-key");

        await client.SearchAsync(new() { Query = text, OriginalQuestion = text });
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(50)]
    public async Task SearchAsyncAcceptsUnicodeAndCountBoundariesAsync(int count)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse("{\"results\":[]}")));
        using var httpClient = new HttpClient(handler);
        var text = string.Concat(Enumerable.Repeat("😀", 128));
        var result = await new DeepSeekClient(httpClient, "test-key").SearchAsync(
            new() { Query = text, OriginalQuestion = text, Count = count });

        Assert.IsEmpty(result.Results);
    }

    [TestMethod]
    [DataRow(400)]
    [DataRow(401)]
    [DataRow(429)]
    [DataRow(500)]
    public async Task SearchAsyncPreservesHttpErrorStatusAsync(int status)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)));
        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekClient(httpClient, "test-key");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.SearchAsync(new() { Query = "test" }));

        Assert.AreEqual((HttpStatusCode)status, exception.StatusCode);
    }

    [TestMethod]
    [DataRow("null")]
    [DataRow("{}")]
    [DataRow("not json")]
    public async Task SearchAsyncRejectsInvalidResponseAsync(string json)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse(json)));
        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekClient(httpClient, "test-key");

        await Assert.ThrowsAsync<JsonException>(() => client.SearchAsync(new() { Query = "test" }));
    }

    [TestMethod]
    public async Task SearchAsyncPropagatesCancellationAsync()
    {
        using var handler = new StubHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return JsonResponse("{\"results\":[]}");
        });
        using var httpClient = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var client = new DeepSeekClient(httpClient, "test-key");

        await Assert.ThrowsAsync<OperationCanceledException>(() => client.SearchAsync(new() { Query = "test" }, cancellation.Token));
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => sendAsync(request, cancellationToken);
    }
}
