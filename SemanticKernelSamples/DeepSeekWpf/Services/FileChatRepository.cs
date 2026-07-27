using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using AgentLib.Model;
using DeepSeekWpf.Models;
using Microsoft.Extensions.AI;

namespace DeepSeekWpf.Services;

public sealed class FileChatRepository : IChatRepository
{
    private const int CurrentSchemaVersion = 2;
    private readonly ISettingsService _settingsService;
    private readonly ConcurrentDictionary<Guid, SessionWriteState> _sessionWriteStates = new();
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public FileChatRepository(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<ChatRepositoryLoadResult> LoadSessionsAsync(CancellationToken cancellationToken = default)
    {
        var storageDirectory = await EnsureStorageDirectoryAsync(cancellationToken);
        List<string> files;
        try
        {
            files = Directory.EnumerateFiles(storageDirectory, "*.json", SearchOption.TopDirectoryOnly).ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw CreateStorageException("无法枚举会话数据目录。", storageDirectory, exception);
        }

        var loadTasks = files.Select(filePath => LoadSessionFileAsync(filePath, cancellationToken));
        var fileResults = await Task.WhenAll(loadTasks);
        cancellationToken.ThrowIfCancellationRequested();

        var sessions = fileResults
            .Where(result => result.Session is not null)
            .Select(result => result.Session!)
            .OrderByDescending(session => session.UpdatedAt)
            .ToList();
        var issues = fileResults
            .Where(result => result.Issue is not null)
            .Select(result => result.Issue!)
            .ToList();

        return new ChatRepositoryLoadResult(sessions, issues);
    }

    public async Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var snapshot = ChatSessionDto.FromModel(session);
        var writeState = _sessionWriteStates.GetOrAdd(session.Id, static _ => new SessionWriteState());
        var operationVersion = writeState.ReserveOperationVersion();

        await writeState.Gate.WaitAsync(cancellationToken);
        try
        {
            if (operationVersion < writeState.LatestOperationVersion)
            {
                return;
            }

            var storageDirectory = await EnsureStorageDirectoryAsync(cancellationToken);
            var filePath = Path.Combine(storageDirectory, $"{session.Id}.json");
            var tempFilePath = Path.Combine(storageDirectory, $".{session.Id}.{Guid.NewGuid():N}.tmp");

            try
            {
                await using (var stream = new FileStream(
                                 tempFilePath,
                                 FileMode.CreateNew,
                                 FileAccess.Write,
                                 FileShare.None,
                                 4096,
                                 FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await JsonSerializer.SerializeAsync(stream, snapshot, _serializerOptions, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(tempFilePath, filePath, true);
            }
            catch (OperationCanceledException)
            {
                TryDeleteTemporaryFile(tempFilePath);
                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                TryDeleteTemporaryFile(tempFilePath);
                throw CreateStorageException("无法保存会话。", filePath, exception);
            }
        }
        finally
        {
            writeState.Gate.Release();
        }
    }

    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var writeState = _sessionWriteStates.GetOrAdd(sessionId, static _ => new SessionWriteState());
        var operationVersion = writeState.ReserveOperationVersion();
        await writeState.Gate.WaitAsync(cancellationToken);
        try
        {
            if (operationVersion < writeState.LatestOperationVersion)
            {
                return;
            }

            var storageDirectory = await EnsureStorageDirectoryAsync(cancellationToken);
            var filePath = Path.Combine(storageDirectory, $"{sessionId}.json");
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw CreateStorageException("无法删除会话。", filePath, exception);
            }
        }
        finally
        {
            writeState.Gate.Release();
        }
    }

    private async Task<SessionFileLoadResult> LoadSessionFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var sessionDto = MigrateToCurrentSchema(document.RootElement);
            return new SessionFileLoadResult(sessionDto.ToModel(), null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new SessionFileLoadResult(
                null,
                new ChatLoadIssue(
                    Path.GetFileName(filePath),
                    exception.GetType().Name,
                    "原文件已保留在 Sessions 目录；请备份后修复或移走该文件，再重新加载。"));
        }
    }

    private ChatSessionDto MigrateToCurrentSchema(JsonElement root)
    {
        var schemaVersion = root.TryGetProperty(nameof(ChatSessionDto.SchemaVersion), out var versionElement)
            ? versionElement.GetInt32()
            : 1;

        return schemaVersion switch
        {
            1 => MigrateV1(root),
            CurrentSchemaVersion => DeserializeRequired<ChatSessionDto>(root),
            _ => throw new NotSupportedException($"不支持会话架构版本 {schemaVersion}。当前支持的版本为 {CurrentSchemaVersion}。"),
        };
    }

    private ChatSessionDto MigrateV1(JsonElement root)
    {
        var version1 = DeserializeRequired<ChatSessionV1Dto>(root);
        return new ChatSessionDto
        {
            SchemaVersion = CurrentSchemaVersion,
            Id = version1.Id,
            Title = version1.Title,
            CreatedAt = version1.CreatedAt,
            UpdatedAt = version1.UpdatedAt,
            Messages = version1.Messages.Select(message => new ChatMessageDto
            {
                Id = message.Id,
                Role = message.Role,
                Text = message.Content,
                Reasoning = message.ThoughtContent,
                CreatedAt = message.CreatedAt,
            }).ToList(),
        };
    }

    private T DeserializeRequired<T>(JsonElement root)
    {
        return root.Deserialize<T>(_serializerOptions)
               ?? throw new JsonException("会话文件内容为空。");
    }

    private async Task<string> EnsureStorageDirectoryAsync(CancellationToken cancellationToken)
    {
        var storageDirectory = Path.Combine(_settingsService.CurrentSettings.DataPath, "Sessions");
        try
        {
            Directory.CreateDirectory(storageDirectory);
            var probePath = Path.Combine(storageDirectory, $".write-probe-{Guid.NewGuid():N}.tmp");
            await using (var stream = new FileStream(
                             probePath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             1,
                             FileOptions.Asynchronous | FileOptions.DeleteOnClose))
            {
                await stream.WriteAsync(new byte[] { 0 }, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            return storageDirectory;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw CreateStorageException("会话数据目录不可写。", storageDirectory, exception);
        }
    }

    private static StorageException CreateStorageException(string message, string path, Exception exception)
    {
        return new StorageException($"{message} 路径：{path}", exception);
    }

    private static void TryDeleteTemporaryFile(string tempFilePath)
    {
        try
        {
            File.Delete(tempFilePath);
        }
        catch
        {
        }
    }

    private sealed class SessionWriteState
    {
        private long _latestOperationVersion;

        public SemaphoreSlim Gate { get; } = new(1, 1);

        public long LatestOperationVersion => Volatile.Read(ref _latestOperationVersion);

        public long ReserveOperationVersion()
        {
            return Interlocked.Increment(ref _latestOperationVersion);
        }
    }

    private sealed record SessionFileLoadResult(ChatSession? Session, ChatLoadIssue? Issue);

    private sealed class ChatSessionDto
    {
        public int SchemaVersion { get; init; } = CurrentSchemaVersion;

        public Guid Id { get; init; }

        public string Title { get; init; } = "新对话";

        public DateTime CreatedAt { get; init; }

        public DateTime UpdatedAt { get; init; }

        public List<ChatMessageDto> Messages { get; init; } = [];

        public static ChatSessionDto FromModel(ChatSession session)
        {
            return new ChatSessionDto
            {
                Id = session.Id,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Messages = session.Messages.Select(ChatMessageDto.FromModel).ToList(),
            };
        }

        public ChatSession ToModel()
        {
            var session = new ChatSession
            {
                Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
                Title = string.IsNullOrWhiteSpace(Title) ? "新对话" : Title,
                CreatedAt = CreatedAt == default ? DateTime.Now : CreatedAt,
                UpdatedAt = UpdatedAt == default ? DateTime.Now : UpdatedAt,
            };

            foreach (var message in Messages)
            {
                session.Messages.Add(message.ToModel());
            }

            return session;
        }
    }

    private sealed class ChatMessageDto
    {
        public Guid Id { get; init; }

        public string Role { get; init; } = nameof(ChatRole.User);

        public string Text { get; init; } = string.Empty;

        public string Reasoning { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }

        public static ChatMessageDto FromModel(ChatMessageViewModel message)
        {
            return new ChatMessageDto
            {
                Id = message.Id,
                Role = message.Role.ToString(),
                Text = message.Content,
                Reasoning = message.ThoughtContent,
                CreatedAt = message.CreatedAt,
            };
        }

        public ChatMessageViewModel ToModel()
        {
            var message = new CopilotChatMessage(ParseRole(Role), string.Empty);
            message.AppendReasoning(Reasoning);
            message.AppendText(Text);
            return new ChatMessageViewModel(
                message,
                Id == Guid.Empty ? null : Id,
                CreatedAt == default ? null : CreatedAt);
        }
    }

    private sealed class ChatSessionV1Dto
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = "新对话";

        public DateTime CreatedAt { get; init; }

        public DateTime UpdatedAt { get; init; }

        public List<ChatMessageV1Dto> Messages { get; init; } = [];
    }

    private sealed class ChatMessageV1Dto
    {
        public Guid Id { get; init; }

        public string Role { get; init; } = nameof(ChatRole.User);

        public string Content { get; init; } = string.Empty;

        public string ThoughtContent { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }
    }

    private static ChatRole ParseRole(string role)
    {
        if (string.Equals(role, nameof(ChatRole.System), StringComparison.OrdinalIgnoreCase))
        {
            return ChatRole.System;
        }

        if (string.Equals(role, nameof(ChatRole.Assistant), StringComparison.OrdinalIgnoreCase))
        {
            return ChatRole.Assistant;
        }

        return ChatRole.User;
    }
}