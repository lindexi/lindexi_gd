using System.Text.Json;
using AgentLib.Model;
using DeepSeekWpf.Models;
using DeepSeekWpf.Services;
using DeepSeekWpf.Tests.TestInfrastructure;

namespace DeepSeekWpf.Tests;

[TestClass]
public sealed class FileChatRepositoryTests
{
    [TestMethod]
    public async Task LoadSessionsAsync_V1WithoutVersion_MigratesMessages()
    {
        using var temp = new TempDirectory();
        var repository = CreateRepository(temp, out var sessionsPath);
        Directory.CreateDirectory(sessionsPath);
        var id = Guid.NewGuid();
        await File.WriteAllTextAsync(Path.Combine(sessionsPath, $"{id}.json"), $$"""
            {
              "Id": "{{id}}",
              "Title": "legacy",
              "CreatedAt": "2025-01-01T00:00:00",
              "UpdatedAt": "2025-01-02T00:00:00",
              "Messages": [{ "Role": "Assistant", "Content": "answer", "ThoughtContent": "reason" }]
            }
            """);

        var result = await repository.LoadSessionsAsync();

        Assert.AreEqual(1, result.Sessions.Count);
        Assert.AreEqual("answer", result.Sessions[0].Messages[0].Content);
        Assert.AreEqual("reason", result.Sessions[0].Messages[0].ThoughtContent);
        Assert.AreEqual(0, result.Issues.Count);
    }

    [TestMethod]
    public async Task SaveSessionAsync_WritesSchemaVersionTwoAndRoundTrips()
    {
        using var temp = new TempDirectory();
        var repository = CreateRepository(temp, out var sessionsPath);
        var session = CreateSession("saved", "hello");

        await repository.SaveSessionAsync(session);
        var json = await File.ReadAllTextAsync(Path.Combine(sessionsPath, $"{session.Id}.json"));
        var result = await repository.LoadSessionsAsync();

        using var document = JsonDocument.Parse(json);
        Assert.AreEqual(2, document.RootElement.GetProperty("SchemaVersion").GetInt32());
        Assert.AreEqual("hello", result.Sessions.Single().Messages.Single().Content);
    }

    [TestMethod]
    public async Task LoadSessionsAsync_FutureVersionAndCorruptJson_ReturnIssuesWithoutBlockingValidSession()
    {
        using var temp = new TempDirectory();
        var repository = CreateRepository(temp, out var sessionsPath);
        var valid = CreateSession("valid", "ok");
        await repository.SaveSessionAsync(valid);
        await File.WriteAllTextAsync(Path.Combine(sessionsPath, "future.json"), "{\"SchemaVersion\":99}");
        await File.WriteAllTextAsync(Path.Combine(sessionsPath, "corrupt.json"), "{ broken");

        var result = await repository.LoadSessionsAsync();

        Assert.AreEqual(1, result.Sessions.Count);
        Assert.AreEqual(2, result.Issues.Count);
        CollectionAssert.AreEquivalent(
            new[] { "future.json", "corrupt.json" },
            result.Issues.Select(issue => issue.FileName).ToArray());
    }

    [TestMethod]
    public async Task SaveSessionAsync_ConcurrentSnapshots_FinalFileContainsLatestSnapshot()
    {
        using var temp = new TempDirectory();
        var repository = CreateRepository(temp, out _);
        var session = CreateSession("first", "one");
        var first = repository.SaveSessionAsync(session);
        session.Title = "latest";
        session.Messages[0].ReplaceContent("two", string.Empty);
        var second = repository.SaveSessionAsync(session);

        await Task.WhenAll(first, second);
        var loaded = await repository.LoadSessionsAsync();

        Assert.AreEqual("latest", loaded.Sessions.Single().Title);
        Assert.AreEqual("two", loaded.Sessions.Single().Messages.Single().Content);
    }

    [TestMethod]
    public async Task DeleteAndSave_OrderDeterminesFinalState()
    {
        using var temp = new TempDirectory();
        var repository = CreateRepository(temp, out var sessionsPath);
        var session = CreateSession("ordered", "value");

        await repository.SaveSessionAsync(session);
        await repository.DeleteSessionAsync(session.Id);
        Assert.IsFalse(File.Exists(Path.Combine(sessionsPath, $"{session.Id}.json")));

        await repository.SaveSessionAsync(session);
        Assert.IsTrue(File.Exists(Path.Combine(sessionsPath, $"{session.Id}.json")));
    }

    [TestMethod]
    public async Task Operations_CanceledToken_ThrowOperationCanceledException()
    {
        using var temp = new TempDirectory();
        var repository = CreateRepository(temp, out _);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => repository.LoadSessionsAsync(cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => repository.SaveSessionAsync(CreateSession("x", "y"), cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => repository.DeleteSessionAsync(Guid.NewGuid(), cancellation.Token));
    }

    private static FileChatRepository CreateRepository(TempDirectory temp, out string sessionsPath)
    {
        var settings = new FakeSettingsService(temp.Path);
        sessionsPath = Path.Combine(settings.CurrentSettings.DataPath, "Sessions");
        return new FileChatRepository(settings);
    }

    private static ChatSession CreateSession(string title, string content)
    {
        var session = new ChatSession { Title = title };
        session.Messages.Add(new ChatMessageViewModel(CopilotChatMessage.CreateUser(content)));
        return session;
    }
}