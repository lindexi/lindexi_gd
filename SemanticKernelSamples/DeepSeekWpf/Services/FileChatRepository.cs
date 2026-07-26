using System.IO;
using System.Linq;
using System.Text.Json;
using AgentLib.Model;
using DeepSeekWpf.Models;
using Microsoft.Extensions.AI;

namespace DeepSeekWpf.Services;

public sealed class FileChatRepository : IChatRepository
{
    private readonly ISettingsService _settingsService;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public FileChatRepository(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<ChatSession>> LoadSessionsAsync(CancellationToken cancellationToken = default)
    {
        var storageDirectory = GetStorageDirectory();
        if (!Directory.Exists(storageDirectory))
        {
            return [];
        }

        var files = Directory.EnumerateFiles(storageDirectory, "*.json", SearchOption.TopDirectoryOnly).ToList();
        var loadTasks = files.Select(filePath => LoadSessionAsync(filePath, cancellationToken));
        var sessions = await Task.WhenAll(loadTasks);

        return sessions
            .Where(session => session is not null)
            .Cast<ChatSession>()
            .OrderByDescending(session => session.UpdatedAt)
            .ToList();
    }

    public void SaveSession(ChatSession session)
    {
        var filePath = GetSessionFilePath(session.Id);
        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, ChatSessionDto.FromModel(session), _serializerOptions);
    }

    public void DeleteSession(Guid sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private async Task<ChatSession?> LoadSessionAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var sessionDto = await JsonSerializer.DeserializeAsync<ChatSessionDto>(
                stream,
                _serializerOptions,
                cancellationToken);
            return sessionDto?.ToModel();
        }
        catch
        {
            return null;
        }
    }

    private string GetStorageDirectory()
    {
        var dataPath = Path.Combine(_settingsService.CurrentSettings.DataPath, "Sessions");
        Directory.CreateDirectory(dataPath);
        return dataPath;
    }

    private string GetSessionFilePath(Guid sessionId)
    {
        return Path.Combine(GetStorageDirectory(), $"{sessionId}.json");
    }

    private sealed class ChatSessionDto
    {
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

        public string Content { get; init; } = string.Empty;

        public string ThoughtContent { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }

        public static ChatMessageDto FromModel(ChatMessageViewModel message)
        {
            return new ChatMessageDto
            {
                Id = message.Id,
                Role = message.Role.ToString(),
                Content = message.Content,
                ThoughtContent = message.ThoughtContent,
                CreatedAt = message.CreatedAt,
            };
        }

        public ChatMessageViewModel ToModel()
        {
            var role = ParseRole(Role);
            var message = new CopilotChatMessage(role, string.Empty);
            message.AppendReasoning(ThoughtContent);
            message.AppendText(Content);
            return new ChatMessageViewModel(
                message,
                Id == Guid.Empty ? null : Id,
                CreatedAt == default ? null : CreatedAt);
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
}
