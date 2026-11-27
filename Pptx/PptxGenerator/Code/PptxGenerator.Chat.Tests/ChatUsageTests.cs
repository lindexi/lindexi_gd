using OpenAI.Responses;
using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat.Tests;

[TestClass]
public sealed class ChatUsageTests
{
    [TestMethod]
    public void ContinuationsAccumulateReportedUsage()
    {
        var initial = new ResponseTokenUsage
        {
            TotalTokenCount = 120, InputTokenCount = 100, OutputTokenCount = 20,
            InputTokenDetails = new() { CachedTokenCount = 40 },
            OutputTokenDetails = new() { ReasoningTokenCount = 5 },
        };
        var continuation = new ResponseTokenUsage
        {
            TotalTokenCount = 230, InputTokenCount = 200, OutputTokenCount = 30,
            InputTokenDetails = new() { CachedTokenCount = 60 },
            OutputTokenDetails = new() { ReasoningTokenCount = 7 },
        };

        var usage = new ChatUsage().Add(initial).Add(continuation);

        Assert.AreEqual(new ChatUsage(350, 300, 50, 12, 100), usage);
    }

    [TestMethod]
    public void UnreportedDetailsRemainAbsent()
    {
        var usage = new ChatUsage().Add(new ResponseTokenUsage
        {
            TotalTokenCount = 30, InputTokenCount = 20, OutputTokenCount = 10,
        });

        Assert.AreEqual(new ChatUsage(30, 20, 10, null, null), usage);
    }
}
