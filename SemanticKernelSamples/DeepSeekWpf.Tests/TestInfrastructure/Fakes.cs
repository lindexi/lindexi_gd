using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using DeepSeekWpf.Models;
using DeepSeekWpf.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace DeepSeekWpf.Tests.TestInfrastructure;

internal sealed class FakeSettingsService : ISettingsService
{
    public FakeSettingsService(string rootPath)
    {
        SettingsFilePath = Path.Combine(rootPath, "settings.json");
        CurrentSettings = new AppSettings
        {
            CachePath = Path.Combine(rootPath, "cache"),
            DataPath = Path.Combine(rootPath, "data"),
            LogPath = Path.Combine(rootPath, "logs"),
            ChatRequestTimeoutSeconds = 30,
            SendMessageWithEnter = true,
        };
    }

    public AppSettings CurrentSettings { get; private set; }

    public string SettingsFilePath { get; }

    public Exception? LastLoadError { get; set; }

    public int SaveCount { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(CurrentSettings);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CurrentSettings = settings with { };
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task RestoreDefaultsAsync(CancellationToken cancellationToken = default)
    {
        CurrentSettings = AppSettings.CreateDefault();
        return Task.CompletedTask;
    }
}

internal sealed class FakeAppLogger : IAppLogger
{
    public ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public Exception? LastWriteError { get; set; }

    public string LogDirectory { get; set; } = string.Empty;

    public int ClearCount { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask LogAsync(LogLevel level, string message, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        Entries.Enqueue((level, message, exception));
        return ValueTask.CompletedTask;
    }

    public Task ClearLogsAsync(CancellationToken cancellationToken = default)
    {
        ClearCount++;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class FakeUserInteractionService : IUserInteractionService
{
    public bool ConfirmationResult { get; set; }

    public string? SaveFilePath { get; set; }

    public string? CopiedText { get; private set; }

    public List<string> OpenedPaths { get; } = [];

    public List<(string Title, string Message)> Messages { get; } = [];

    public Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken = default) =>
        Task.FromResult(ConfirmationResult);

    public Task<string?> SelectSaveFileAsync(string title, string suggestedFileName, string filter, CancellationToken cancellationToken = default) =>
        Task.FromResult(SaveFilePath);

    public Task ShowMessageAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        Messages.Add((title, message));
        return Task.CompletedTask;
    }

    public Task CopyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        CopiedText = text;
        return Task.CompletedTask;
    }

    public Task OpenPathAsync(string path, CancellationToken cancellationToken = default)
    {
        OpenedPaths.Add(path);
        return Task.CompletedTask;
    }
}

internal sealed class FakeChatRepository : IChatRepository
{
    public ChatRepositoryLoadResult LoadResult { get; set; } = new([], []);

    public List<ChatSession> SavedSessions { get; } = [];

    public List<Guid> DeletedSessionIds { get; } = [];

    public Exception? LoadException { get; set; }

    public Task<ChatRepositoryLoadResult> LoadSessionsAsync(CancellationToken cancellationToken = default)
    {
        return LoadException is null
            ? Task.FromResult(LoadResult)
            : Task.FromException<ChatRepositoryLoadResult>(LoadException);
    }

    public Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SavedSessions.Add(session);
        return Task.CompletedTask;
    }

    public Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DeletedSessionIds.Add(sessionId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeAiChatService : IAiChatService
{
    public Func<ChatSession, CancellationToken, IAsyncEnumerable<AiResponseChunk>> Handler { get; set; } =
        static (_, _) => Empty();

    public int CallCount { get; private set; }

    public List<int> MessageCounts { get; } = [];

    public IAsyncEnumerable<AiResponseChunk> GetReplyAsync(ChatSession session, CancellationToken cancellationToken)
    {
        CallCount++;
        MessageCounts.Add(session.Messages.Count);
        return Handler(session, cancellationToken);
    }

    public static async IAsyncEnumerable<AiResponseChunk> FromChunks(
        IEnumerable<AiResponseChunk> chunks,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return chunk;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<AiResponseChunk> Empty()
    {
        await Task.CompletedTask;
        yield break;
    }
}

internal sealed class FakeAgentModelService : IAgentModelService
{
    public string ConfigurationFilePath { get; set; } = string.Empty;

    public IReadOnlyList<AgentModelDescriptor> RegisteredModels { get; set; } = [];

    public AgentModelDescriptor? SelectedModel { get; set; }

    public IChatClient? ChatClient { get; set; }

    public int ReloadCount { get; private set; }

    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        ReloadCount++;
        return Task.CompletedTask;
    }

    public void SelectModel(string modelSpecifier)
    {
        SelectedModel = RegisteredModels.Single(model => model.Specifier == modelSpecifier);
    }

    public Task<IChatClient> GetSelectedChatClientAsync() =>
        Task.FromResult(ChatClient ?? throw new InvalidOperationException("未设置测试 ChatClient。"));
}

internal sealed class FakeChatExportService : IChatExportService
{
    public int ExportCount { get; private set; }

    public Task ExportMarkdownAsync(ChatSession session, string filePath, CancellationToken cancellationToken = default)
    {
        ExportCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeModelConnectionTestService : IModelConnectionTestService
{
    public ModelConnectionTestResult Result { get; set; } = new(true, null, "连接成功");

    public int CallCount { get; private set; }

    public Task<ModelConnectionTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(Result);
    }
}

internal sealed class FakeDiagnosticsService : IDiagnosticsService
{
    public string Summary { get; set; } = "diagnostics";

    public int ClearCount { get; private set; }

    public Task<string> CreateSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult(Summary);

    public Task ClearLogsAsync(CancellationToken cancellationToken = default)
    {
        ClearCount++;
        return Task.CompletedTask;
    }
}