using System.IO;
using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat.Tests;

[TestClass]
public sealed class ChatSessionStoreTests
{
    [TestMethod]
    public async Task SavedSessionRestoresToolDetailsAndAttachmentsAsync()
    {
        var store = CreateStore();
        var summary = new SessionSummary(Guid.NewGuid(), "Lesson", DateTimeOffset.UtcNow);
        var image = new ToolImage("image/png", "AQID");
        var tool = new ToolActivity("Screenshot", "get_slide_screenshot", "{}", "Returned", "Result", [image]);
        var session = new ChatSession(summary,
        [
            new SessionMessage(true, "Lesson", "", "", [new ChatImage("reference.png", "image/png", [1, 2, 3])], [], []),
            new SessionMessage(false, "Done", "", "30 tokens", [], [], [new SessionPart(0, null, tool)]),
        ]);

        await store.SaveAsync(session);
        var restored = await store.LoadAsync(summary.Id);
        var assistant = restored.Messages[1];

        Assert.AreEqual(("AQID", "30 tokens", "AQID"), (Convert.ToBase64String(restored.Messages[0].Images.Single().Data),
            assistant.UsageSummary, assistant.Parts.Single().Tool?.Images?.Single().Base64));
    }

    [TestMethod]
    public async Task UpdatedSessionAppearsOnceAndMovesToTopAsync()
    {
        var store = CreateStore();
        var first = new SessionSummary(Guid.NewGuid(), "First", DateTimeOffset.UtcNow.AddDays(-2));
        var second = new SessionSummary(Guid.NewGuid(), "Second", first.UpdatedAt.AddDays(1));
        await store.SaveAsync(new ChatSession(first, []));
        await store.SaveAsync(new ChatSession(second, []));

        await store.SaveAsync(new ChatSession(first with { UpdatedAt = second.UpdatedAt.AddDays(1) }, []));
        var (sessions, _) = await store.ListAsync();

        CollectionAssert.AreEqual(new[] { first.Id, second.Id }, sessions.Select(session => session.Id).ToArray());
    }

    [TestMethod]
    public async Task SavedSessionRestoresMcpTargetBindingAsync()
    {
        var store = CreateStore();
        var summary = new SessionSummary(Guid.NewGuid(), "Lesson", DateTimeOffset.UtcNow);
        var binding = new McpTargetBinding("http://localhost:7916/mcp", "123");
        await store.SaveAsync(new ChatSession(summary, [new SessionMessage(true, "Lesson", "", "", [], [], [])], binding));

        var restored = await store.LoadAsync(summary.Id);

        Assert.AreEqual(binding, restored.TargetBinding);
    }

    private static ChatSessionStore CreateStore() =>
        new(Directory.CreateTempSubdirectory("PptxGenerator.Chat.Sessions-").FullName);
}
