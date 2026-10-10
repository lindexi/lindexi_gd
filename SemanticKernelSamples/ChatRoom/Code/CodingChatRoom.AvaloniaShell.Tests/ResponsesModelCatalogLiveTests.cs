using System.Net.Http.Headers;
using System.Text.Json;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
[TestCategory("LiveApi")]
[DoNotParallelize]
public sealed class ResponsesModelCatalogLiveTests
{
    [TestMethod]
    public async Task AlternativeCatalogModelShouldContinueNativeCompactionAsync()
    {
        const string keyPath = @"C:\lindexi\Work\Key\MiniMax.txt";
        if (!File.Exists(keyPath)) return;
        const string model = "MiniMax-M2.7";
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var http = new HttpClient { BaseAddress = new Uri("https://api.minimaxi.com/v1/") };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await File.ReadAllTextAsync(keyPath, timeout.Token)).Trim());
        string marker = $"PROJECT-{Guid.NewGuid():N}";
        var exchanges = new List<object>();
        string report = Path.Combine(Path.GetTempPath(), $"compaction-alternative-model-{Guid.NewGuid():N}.json");
        try
        {
            var history = new List<JsonElement> { User($"The C# project identifier is {marker}. Acknowledge briefly.") };
            using var first = await PostAsync("responses", new { model, input = history, stream = false });
            history.AddRange(first.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()));
            var question = User("Repeat the project identifier given above. Only output its exact value.");
            using var baseline = await PostAsync("responses", new { model, input = history.Append(question).ToArray(), stream = false });
            Console.WriteLine($"Alternative model baseline: {Text(baseline.RootElement)}");
            StringAssert.Contains(Text(baseline.RootElement), marker);
            using var compact = await PostAsync("responses/compact", new { model, input = history });
            var output = compact.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToArray();
            using var continuation = await PostAsync("responses", new { model, input = output.Append(question).ToArray(), stream = false });
            Console.WriteLine($"Alternative model continuation: {Text(continuation.RootElement)}");
            StringAssert.Contains(Text(continuation.RootElement), marker);
        }
        finally
        {
            await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { model, marker, exchanges },
                new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Alternative model report: {report}");
        }

        async Task<JsonDocument> PostAsync(string path, object payload)
        {
            string request = JsonSerializer.Serialize(payload);
            using var body = new StringContent(request, System.Text.Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, body, timeout.Token);
            string text = await response.Content.ReadAsStringAsync(timeout.Token);
            exchanges.Add(new { path, request, Status = (int)response.StatusCode, Response = text });
            Console.WriteLine($"Alternative model {path}: HTTP {(int)response.StatusCode}");
            Assert.IsTrue(response.IsSuccessStatusCode, text);
            return JsonDocument.Parse(text);
        }
        static JsonElement User(string text) => JsonSerializer.SerializeToElement(new
        {
            type = "message", role = "user", content = new[] { new { type = "input_text", text } },
        });
        static string Text(JsonElement response) => string.Join("", response.GetProperty("output").EnumerateArray()
            .Where(item => item.GetProperty("type").GetString() == "message")
            .SelectMany(item => item.GetProperty("content").EnumerateArray())
            .Where(part => part.GetProperty("type").GetString() == "output_text")
            .Select(part => part.GetProperty("text").GetString()));
    }

    [TestMethod]
    public async Task ModelCatalogShouldProvideActualModelIdentifiersAsync()
    {
        const string keyPath = @"C:\lindexi\Work\Key\MiniMax.txt";
        if (!File.Exists(keyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await File.ReadAllTextAsync(keyPath, timeout.Token)).Trim());
        using var response = await http.GetAsync("https://api.minimaxi.com/v1/models", timeout.Token);
        string body = await response.Content.ReadAsStringAsync(timeout.Token);
        Console.WriteLine($"Model catalog HTTP status: {(int)response.StatusCode}");
        Assert.IsTrue(response.IsSuccessStatusCode, body);
        using var document = JsonDocument.Parse(body);
        string[] models = document.RootElement.GetProperty("data").EnumerateArray()
            .Select(item => item.GetProperty("id").GetString() ?? throw new InvalidDataException("Model ID missing."))
            .ToArray();
        Console.WriteLine($"Available model identifiers: {string.Join(", ", models)}");
        await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), "responses-model-catalog.json"),
            JsonSerializer.Serialize(models, new JsonSerializerOptions { WriteIndented = true }), timeout.Token);
        Assert.IsNotEmpty(models);
    }
}
