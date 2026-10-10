using System.Text.Json;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ResponsesCompactionReportTests
{
    [TestMethod]
    public void RecordedContinuationShouldContainEntireCompactionOutputUnchanged()
    {
        const string path = @"C:\Users\lindexi\AppData\Local\Temp\compaction-coding-flow-93719387ee9346e188d16c7425d5b9c5.json";
        if (!File.Exists(path)) return;
        using var report = JsonDocument.Parse(File.ReadAllText(path));
        var exchanges = report.RootElement.GetProperty("records").EnumerateArray().ToArray();
        int compactIndex = Array.FindIndex(exchanges, exchange =>
            exchange.GetProperty("Url").GetString()?.EndsWith("/responses/compact", StringComparison.Ordinal) == true);
        Assert.IsGreaterThanOrEqualTo(0, compactIndex);
        using var compactRequest = JsonDocument.Parse(exchanges[compactIndex].GetProperty("Request").GetString()!);
        using var compactResponse = JsonDocument.Parse(exchanges[compactIndex].GetProperty("Response").GetString()!);
        using var continuation = JsonDocument.Parse(exchanges[compactIndex + 1].GetProperty("Request").GetString()!);
        var output = compactResponse.RootElement.GetProperty("output").EnumerateArray().ToArray();
        var input = continuation.RootElement.GetProperty("input").EnumerateArray().ToArray();
        Console.WriteLine($"Original history count: {compactRequest.RootElement.GetProperty("input").GetArrayLength()}; compact output count: {output.Length}; continuation input count: {input.Length}");
        Assert.AreEqual(output.Length + 1, input.Length);
        for (int i = 0; i < output.Length; i++)
        {
            Assert.IsTrue(JsonElement.DeepEquals(output[i], input[i]), $"Native output item {i} changed in continuation request.");
        }
        Assert.AreEqual("user", input[^1].GetProperty("role").GetString());
    }
}
