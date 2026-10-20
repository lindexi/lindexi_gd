using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeepSeekSDK.Tests;

[TestClass]
public sealed class DeepSeekSearchIntegrationTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("Integration")]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SearchAsyncCallsRealServiceAsync(bool needContent)
    {
        const string keyPath = @"c:\lindexi\Work\Key\deepseek.txt";
        if (!File.Exists(keyPath))
        {
            Assert.Inconclusive($"未找到开发者 API Key 文件，跳过付费真实接口测试：{keyPath}");
        }

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var apiKey = (await File.ReadAllTextAsync(keyPath, cancellation.Token)).Trim();
        using var httpClient = new HttpClient();
        var client = new DeepSeekClient(httpClient, apiKey);

        var response = await client.SearchAsync(new DeepSeekSearchRequest
        {
            Query = "DeepSeek 官方 API 文档",
            Count = 2,
            OriginalQuestion = "在哪里查看 DeepSeek 官方 API 文档？",
            NeedContent = needContent
        }, cancellation.Token);

        TestContext.WriteLine($"need_content={needContent}，返回结果数：{response.Results.Count}");
        Assert.IsTrue(response.Results.Count is > 0 and <= 2);
        foreach (var result in response.Results)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Title));
            Assert.IsTrue(Uri.TryCreate(result.Url, UriKind.Absolute, out _));
            Assert.IsNotNull(result.Snippet);
            Assert.IsNotNull(result.Summary);
            Assert.IsNotNull(result.Content);
            Assert.IsTrue(result.RerankScore is >= 0 and <= 3);
            Assert.IsTrue(result.Authority is >= -1 and <= 4);
        }

        if (!needContent)
        {
            CollectionAssert.AreEqual(new[] { string.Empty, string.Empty }.Take(response.Results.Count).ToArray(),
                response.Results.Select(result => result.Content).ToArray());
        }
    }
}
