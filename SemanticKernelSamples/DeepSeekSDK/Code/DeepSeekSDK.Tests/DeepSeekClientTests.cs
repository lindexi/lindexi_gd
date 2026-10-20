using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeepSeekSDK.Tests;

[TestClass]
public sealed class DeepSeekClientTests
{
    /// <summary>验证真实服务拒绝非法参数时保留 HTTP 状态码及错误信息。</summary>
    [TestMethod]
    [TestCategory("Integration")]
    [DataRow(false)]
    [DataRow(true)]
    public async Task InvalidRequestReturnsHttpErrorAsync(bool fetch)
    {
        const string keyPath = @"c:\lindexi\Work\Key\deepseek.txt";
        if (!File.Exists(keyPath))
        {
            return;
        }

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var apiKey = (await File.ReadAllTextAsync(keyPath, cancellation.Token)).Trim();
        using var httpClient = new HttpClient();
        var client = new DeepSeekClient(httpClient, apiKey);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            if (fetch)
            {
                await client.FetchAsync(new() { Url = string.Empty }, cancellation.Token);
            }
            else
            {
                await client.SearchAsync(new() { Query = string.Empty }, cancellation.Token);
            }
        });

        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(exception.Message));
    }

    /// <summary>验证真实网页抓取返回页面标题及正文。</summary>
    [TestMethod]
    [TestCategory("Integration")]
    public async Task FetchAsyncReturnsRealPageAsync()
    {
        const string keyPath = @"c:\lindexi\Work\Key\deepseek.txt";
        if (!File.Exists(keyPath))
        {
            return;
        }

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var apiKey = (await File.ReadAllTextAsync(keyPath, cancellation.Token)).Trim();
        using var httpClient = new HttpClient();
        var client = new DeepSeekClient(httpClient, apiKey);

        var response = await client.FetchAsync(new DeepSeekFetchRequest
        {
            Url = "https://api-docs.deepseek.com/"
        }, cancellation.Token);

        StringAssert.Contains(response.Content, "DeepSeek");
    }

    /// <summary>验证默认参数及显式指定正文选项时的真实搜索结果。</summary>
    [TestMethod]
    [TestCategory("Integration")]
    [DataRow(null)]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SearchAsyncReturnsRealResultsAsync(bool? needContent)
    {
        const string keyPath = @"c:\lindexi\Work\Key\deepseek.txt";
        if (!File.Exists(keyPath))
        {
            return;
        }

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var apiKey = (await File.ReadAllTextAsync(keyPath, cancellation.Token)).Trim();
        using var httpClient = new HttpClient();
        var client = new DeepSeekClient(httpClient, apiKey);

        var request = new DeepSeekSearchRequest
        {
            Query = "DeepSeek 官方 API 文档"
        };
        if (needContent.HasValue)
        {
            request = request with
            {
                Count = 2,
                OriginalQuestion = "在哪里查看 DeepSeek 官方 API 文档？",
                NeedContent = needContent.Value
            };
        }

        var response = await client.SearchAsync(request, cancellation.Token);

        Assert.IsTrue(response.Results.Count > 0 && response.Results.Count <= request.Count);
        foreach (var result in response.Results)
        {
            Assert.IsNotNull(result.Title);
            Assert.IsTrue(Uri.TryCreate(result.Url, UriKind.Absolute, out _));
            Assert.IsNotNull(result.Snippet);
            Assert.IsNotNull(result.Summary);
            Assert.IsNotNull(result.Content);
            Assert.IsTrue(result.RerankScore is >= 0 and <= 3);
            Assert.IsTrue(result.Authority is >= -1 and <= 4);
            if (!request.NeedContent)
            {
                Assert.AreEqual(string.Empty, result.Content);
            }
        }
    }
}
