using AgentLib.ChatRoom.Model;
using AgentLib.Logging;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
#if !NET6_0
using System.Text.Json.Serialization.Metadata;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom;

/// <summary>
/// 聊天室持久化管理器。使用多文件结构：
/// - <c>room.config.json</c>：聊天室配置 + 角色定义
/// - <c>public_logs/{sessionId}.txt</c>：公开聊天记录（复用 <see cref="FileCopilotChatLogger"/> 的文本日志格式）
/// - <c>{sessionId}/{RoleId}/chat_history.xml</c>：每个角色的私有完整记录
/// </summary>
public sealed class ChatRoomPersistence
{
    private readonly string _baseFolder;
    private readonly FileCopilotChatLogger _publicLogger;
    private readonly Dictionary<string, FileCopilotChatLogger> _roleLoggers = [];
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>
    /// 使用指定的持久化根目录创建管理器。
    /// </summary>
    /// <param name="baseFolder">持久化根目录。每个聊天室会话在此目录下拥有自己的子文件夹。</param>
    public ChatRoomPersistence(string baseFolder)
    {
        ArgumentNullException.ThrowIfNull(baseFolder);
        _baseFolder = baseFolder;
        Directory.CreateDirectory(_baseFolder);

        // 公开消息的日志记录器
        _publicLogger = new FileCopilotChatLogger(Path.Join(_baseFolder, "public_logs"));
    }

    /// <summary>
    /// 获取指定会话的存储文件夹路径。
    /// </summary>
    private string GetSessionFolder(string sessionId) => Path.Join(_baseFolder, sessionId);

    /// <summary>
    /// 获取配置文件路径。
    /// </summary>
    private static string GetConfigFilePath(string sessionFolder) => Path.Join(sessionFolder, "room.config.json");

    /// <summary>
    /// 获取角色私有历史文件夹路径。
    /// </summary>
    private static string GetRoleHistoryFolder(string sessionFolder, string roleId) => Path.Join(sessionFolder, roleId);

    /// <summary>
    /// 获取角色 AgentSession 状态文件路径。
    /// </summary>
    private static string GetRoleAgentSessionStateFilePath(string sessionFolder, string roleId) =>
        Path.Join(GetRoleHistoryFolder(sessionFolder, roleId), "agent-session-state.json");

    /// <summary>
    /// 获取或创建指定角色在指定会话中的日志记录器。
    /// </summary>
    private FileCopilotChatLogger GetRoleLogger(string sessionFolder, string roleId)
    {
        string key = $"{sessionFolder}:{roleId}";
        if (_roleLoggers.TryGetValue(key, out FileCopilotChatLogger? logger))
        {
            return logger;
        }

        string roleFolder = GetRoleHistoryFolder(sessionFolder, roleId);
        logger = new FileCopilotChatLogger(roleFolder, roleFolder);
        _roleLoggers[key] = logger;
        return logger;
    }

    /// <summary>
    /// 序列化配置为 JSON 并保存到聊天室文件夹。
    /// </summary>
    /// <param name="data">会话持久化数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SaveConfigAsync(ChatRoomSessionData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        string sessionFolder = GetSessionFolder(data.SessionId.ToString("N"));
        Directory.CreateDirectory(sessionFolder);

        string configPath = GetConfigFilePath(sessionFolder);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
#if NET6_0
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            string json = JsonSerializer.Serialize(data, options);
#else
            string json = JsonSerializer.Serialize(data, ChatRoomJsonSerializerContext.Default.ChatRoomSessionData);
#endif
            await File.WriteAllTextAsync(configPath, json, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 加载指定会话的配置。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话持久化数据；如果会话不存在则返回 <see langword="null"/>。</returns>
    public async Task<ChatRoomSessionData?> LoadConfigAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        string sessionFolder = GetSessionFolder(sessionId);
        string configPath = GetConfigFilePath(sessionFolder);

        if (!File.Exists(configPath))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(configPath, cancellationToken).ConfigureAwait(false);
#if NET6_0
        return JsonSerializer.Deserialize<ChatRoomSessionData>(json);
#else
        return JsonSerializer.Deserialize(json, ChatRoomJsonSerializerContext.Default.ChatRoomSessionData);
#endif
    }

    /// <summary>
    /// 持久化一条公开消息（纯文本日志）。
    /// </summary>
    /// <param name="sessionId">聊天室会话 ID。</param>
    /// <param name="message">公开消息。</param>
    public async Task SavePublicMessageAsync(Guid sessionId, ChatRoomMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // 将 ChatRoomMessage 转为 CopilotChatMessage 以复用 FileCopilotChatLogger
        var copilotMessage = new CopilotChatMessage(
            message.IsSystemMessage ? ChatRole.System :
            message.IsHumanMessage ? ChatRole.User : ChatRole.Assistant,
            message.Content);

        await _publicLogger.LogMessageAsync(sessionId, copilotMessage).ConfigureAwait(false);
    }

    /// <summary>
    /// 持久化角色的私有消息（完整记录，含工具调用和思考内容）。
    /// </summary>
    /// <param name="sessionId">会话 ID（聊天室会话 ID）。</param>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="copilotMessage">角色的完整 <see cref="CopilotChatMessage"/>。</param>
    public async Task SaveRoleMessageAsync(Guid sessionId, string roleId, CopilotChatMessage copilotMessage)
    {
        ArgumentNullException.ThrowIfNull(roleId);
        ArgumentNullException.ThrowIfNull(copilotMessage);

        string sessionFolder = GetSessionFolder(sessionId.ToString());
        FileCopilotChatLogger logger = GetRoleLogger(sessionFolder, roleId);

        // 使用 roleId 派生一个稳定的 Guid 作为角色日志的 sessionId
        var roleSessionId = DeriveGuidFromString($"{sessionId}:{roleId}");
        await logger.LogMessageAsync(roleSessionId, copilotMessage).ConfigureAwait(false);
    }

    /// <summary>
    /// 保存指定角色的 AgentSession 序列化状态。
    /// </summary>
    /// <param name="sessionId">聊天室会话 ID。</param>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="agentSessionState">通过 ChatClientAgent 序列化得到的 AgentSession 状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SaveRoleAgentSessionStateAsync(Guid sessionId, string roleId, JsonElement agentSessionState,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleId);

        string sessionFolder = GetSessionFolder(sessionId.ToString("N"));
        string stateFilePath = GetRoleAgentSessionStateFilePath(sessionFolder, roleId);
        Directory.CreateDirectory(Path.GetDirectoryName(stateFilePath)!);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string tempStateFilePath = $"{stateFilePath}.{Guid.NewGuid():N}.tmp";
            await using (FileStream fileStream = File.Create(tempStateFilePath))
            {
#if NET6_0
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };
                await JsonSerializer.SerializeAsync(fileStream, agentSessionState, options, cancellationToken).ConfigureAwait(false);
#else
                await JsonSerializer.SerializeAsync(fileStream, agentSessionState,
                    ChatRoomJsonSerializerContext.Default.JsonElement,
                    cancellationToken).ConfigureAwait(false);
#endif
            }

            File.Move(tempStateFilePath, stateFilePath, overwrite: true);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 加载指定角色的 AgentSession 序列化状态。
    /// </summary>
    /// <param name="sessionId">聊天室会话 ID。</param>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>角色 AgentSession 状态；不存在时返回 <see langword="null"/>。</returns>
    public async Task<JsonElement?> LoadRoleAgentSessionStateAsync(Guid sessionId, string roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleId);

        string sessionFolder = GetSessionFolder(sessionId.ToString("N"));
        string stateFilePath = GetRoleAgentSessionStateFilePath(sessionFolder, roleId);
        if (!File.Exists(stateFilePath))
        {
            return null;
        }

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using FileStream fileStream = File.OpenRead(stateFilePath);
            using JsonDocument document = await JsonDocument.ParseAsync(fileStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            return document.RootElement.Clone();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 删除指定角色的 AgentSession 序列化状态。
    /// </summary>
    /// <param name="sessionId">聊天室会话 ID。</param>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task DeleteRoleAgentSessionStateAsync(Guid sessionId, string roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleId);

        string sessionFolder = GetSessionFolder(sessionId.ToString("N"));
        string stateFilePath = GetRoleAgentSessionStateFilePath(sessionFolder, roleId);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(stateFilePath))
            {
                File.Delete(stateFilePath);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 列出所有已持久化的会话 ID。
    /// </summary>
    public IReadOnlyList<string> ListSessionIds()
    {
        if (!Directory.Exists(_baseFolder))
        {
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(_baseFolder)
            .Select(d => Path.GetFileName(d))
            .ToList();
    }

    /// <summary>
    /// 删除指定会话的所有持久化数据。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    public void Delete(string sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        string sessionFolder = GetSessionFolder(sessionId);
        if (Directory.Exists(sessionFolder))
        {
            Directory.Delete(sessionFolder, recursive: true);
        }
    }

    private static Guid DeriveGuidFromString(string input)
    {
        // 使用 SHA256 哈希从字符串派生稳定的 Guid
        byte[] hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));
        byte[] guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}