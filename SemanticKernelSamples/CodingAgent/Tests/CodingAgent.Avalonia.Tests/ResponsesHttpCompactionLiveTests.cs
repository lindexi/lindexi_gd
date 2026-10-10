using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CodingAgent.AvaloniaShell.Tests;

[TestClass]
[TestCategory("LiveApi")]
[DoNotParallelize]
public sealed class ResponsesHttpCompactionLiveTests
{
    private const string KeyPath = @"C:\lindexi\Work\Key\MiniMax.txt";
    private const string Endpoint = "https://api.minimaxi.com/v1/";
    private const string ModelId = "MiniMax-M3";

    [TestMethod]
    [TestCategory("CompactionAcceptance")]
    public async Task RawHttpCompactionShouldPreserveContextWithoutSdkAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var http = new HttpClient { BaseAddress = new Uri(Endpoint) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await File.ReadAllTextAsync(KeyPath, cancellation.Token)).Trim());
        string marker = $"PROJECT-{Guid.NewGuid():N}";
        string reportPath = Path.Combine(Path.GetTempPath(), $"responses-http-compact-{Guid.NewGuid():N}.json");
        var records = new List<Exchange>();
        try
        {
            var history = new List<JsonElement>
            {
                UserMessage($"For this C# project the test identifier is {marker}. Acknowledge it briefly."),
            };
            using var first = await PostAsync("responses", new { model = ModelId, input = history, stream = false, store = false });
            history.AddRange(first.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()));
            const string historyQuestion = "Repeat the project test identifier specified earlier. Reply only with its exact value.";
            JsonElement question = UserMessage(historyQuestion);
            using var baseline = await PostAsync("responses", new
            {
                model = ModelId, input = history.Append(question).ToArray(), stream = false, store = false,
            });
            StringAssert.Contains(Answer(baseline.RootElement), marker);
            using var compact = await PostAsync("responses/compact", new { model = ModelId, input = history });
            var compacted = compact.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToList();
            using var continuation = await PostAsync("responses", new
            {
                model = ModelId, input = compacted.Append(question).ToArray(), stream = false, store = false,
            });
            Console.WriteLine($"Raw HTTP continuation: {Answer(continuation.RootElement)}");
            StringAssert.Contains(Answer(continuation.RootElement), marker);
            compacted.Add(question);
            compacted.AddRange(continuation.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()));
            compacted.Add(UserMessage("Repeat that exact project identifier once more."));
            using var second = await PostAsync("responses", new { model = ModelId, input = compacted, stream = false, store = false });
            StringAssert.Contains(Answer(second.RootElement), marker);
        }
        finally
        {
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { marker, records },
                new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Raw HTTP report: {reportPath}");
        }

        async Task<JsonDocument> PostAsync(string path, object payload)
        {
            string requestJson = JsonSerializer.Serialize(payload);
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, content, cancellation.Token);
            string responseJson = await response.Content.ReadAsStringAsync(cancellation.Token);
            records.Add(new Exchange(new Uri(http.BaseAddress!, path).ToString(), requestJson,
                (int)response.StatusCode, responseJson));
            Assert.IsTrue(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {responseJson}");
            return JsonDocument.Parse(responseJson);
        }
    }

    private static JsonElement UserMessage(string text) => JsonSerializer.SerializeToElement(new
    {
        type = "message", role = "user", content = new[] { new { type = "input_text", text } },
    });

    private static string Answer(JsonElement response) => string.Join("", response.GetProperty("output").EnumerateArray()
        .Where(item => item.GetProperty("type").GetString() == "message")
        .SelectMany(item => item.GetProperty("content").EnumerateArray())
        .Where(part => part.GetProperty("type").GetString() == "output_text")
        .Select(part => part.GetProperty("text").GetString()));

    private sealed record Exchange(string Url, string Request, int Status, string Response);
}
