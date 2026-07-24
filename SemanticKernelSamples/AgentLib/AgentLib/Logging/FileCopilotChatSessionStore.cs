using AgentLib.Core;
using AgentLib.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace AgentLib.Logging;

/// <summary>
/// 在显式目录中保存、列出、加载和删除 Copilot 聊天会话。
/// </summary>
public sealed class FileCopilotChatSessionStore
{
    /// <summary>
    /// 当前会话历史格式版本。
    /// </summary>
    public const int CurrentFormatVersion = 2;

    private static readonly Encoding Utf8EncodingWithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _sessionDirectory;
    private readonly string? _logDirectory;

    /// <summary>
    /// 使用显式会话目录和可选日志目录创建文件会话存储。
    /// </summary>
    /// <param name="sessionDirectory">会话历史目录。</param>
    /// <param name="logDirectory">对应日志根目录；删除会话时用于清理关联日志。</param>
    public FileCopilotChatSessionStore(string sessionDirectory, string? logDirectory = null)
    {
        ArgumentHelper.ThrowIfNullOrWhiteSpace(sessionDirectory);
        _sessionDirectory = Path.GetFullPath(sessionDirectory);
        _logDirectory = string.IsNullOrWhiteSpace(logDirectory) ? null : Path.GetFullPath(logDirectory);
    }

    /// <summary>
    /// 保存完整会话及其代理状态，同一会话始终更新同一个历史文件。
    /// </summary>
    public async Task SaveSessionAsync(
        CopilotChatSession session,
        JsonElement? agentSessionState,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_sessionDirectory);
            string filePath = FindSessionFile(session.SessionId)
                              ?? Path.Join(_sessionDirectory, $"{session.StartedTime:yyyyMMdd_HHmmss}_{session.SessionId:N}.xml");
            var document = new XDocument(CopilotChatHistoryXmlCodec.CreateRootElement(
                session.SessionId,
                session.StartedTime,
                session.Title,
                CurrentFormatVersion,
                session.ChatMessages,
                agentSessionState));
            await SaveDocumentAsync(filePath, document, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 列出所有可读取的会话摘要；损坏文件会被隔离跳过。
    /// </summary>
    public async Task<IReadOnlyList<CopilotChatSessionSummary>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_sessionDirectory))
        {
            return [];
        }

        string[] files = Directory.GetFiles(_sessionDirectory, "*.xml", SearchOption.TopDirectoryOnly);
        var summaries = new List<CopilotChatSessionSummary>(files.Length);
        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                CopilotChatSessionPersistenceData persistenceData = await LoadFileAsync(file, cancellationToken).ConfigureAwait(false);
                summaries.Add(new CopilotChatSessionSummary
                {
                    SessionId = persistenceData.SessionId,
                    Title = persistenceData.Title,
                    StartedTime = persistenceData.StartedTime,
                    MessageCount = persistenceData.Messages.Count,
                });
            }
            catch (Exception exception) when (exception is InvalidDataException or XmlException or JsonException or FormatException)
            {
            }
        }

        return summaries.OrderByDescending(summary => summary.StartedTime).ToArray();
    }

    /// <summary>
    /// 按会话 ID 加载持久化的会话数据。
    /// </summary>
    public async Task<CopilotChatSessionPersistenceData> LoadSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        string filePath = FindSessionFile(sessionId)
                          ?? throw new FileNotFoundException($"未找到会话 {sessionId} 的历史文件。", sessionId.ToString());
        return await LoadFileAsync(filePath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 删除会话历史及对应日志文件。
    /// </summary>
    /// <returns>存在并删除了历史或日志文件时返回 <see langword="true"/>。</returns>
    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            bool deleted = false;
            string? historyFile = FindSessionFile(sessionId);
            if (historyFile is not null)
            {
                File.Delete(historyFile);
                deleted = true;
            }

            if (_logDirectory is not null && Directory.Exists(_logDirectory))
            {
                foreach (string logFile in Directory.GetFiles(_logDirectory, $"*{sessionId:N}*.log", SearchOption.AllDirectories))
                {
                    File.Delete(logFile);
                    deleted = true;
                }
            }

            return deleted;
        }
        finally
        {
            _gate.Release();
        }
    }

    private string? FindSessionFile(Guid sessionId)
    {
        if (!Directory.Exists(_sessionDirectory))
        {
            return null;
        }

        return Directory.GetFiles(_sessionDirectory, $"*_{sessionId:N}.xml", SearchOption.TopDirectoryOnly).SingleOrDefault();
    }

    private static async Task<CopilotChatSessionPersistenceData> LoadFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        XDocument document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        CopilotChatSessionPersistenceData persistenceData = CopilotChatHistoryXmlCodec.ReadPersistenceData(document);
        if (persistenceData.FormatVersion is < 1 or > CurrentFormatVersion)
        {
            throw new InvalidDataException($"不支持的聊天历史格式版本：{persistenceData.FormatVersion}。");
        }

        return persistenceData;
    }

    internal static async Task SaveDocumentAsync(string filePath, XDocument document, CancellationToken cancellationToken = default)
    {
        string directory = Path.GetDirectoryName(filePath)
                           ?? throw new InvalidOperationException($"无法确定会话文件 '{filePath}' 的目录。");
        string temporaryFilePath = Path.Join(directory, $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var fileStream = new FileStream(temporaryFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await using (XmlWriter xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings
            {
                Async = true,
                Encoding = Utf8EncodingWithoutBom,
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = true,
            }))
            {
                await document.SaveAsync(xmlWriter, cancellationToken).ConfigureAwait(false);
                await xmlWriter.FlushAsync().ConfigureAwait(false);
                await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryFilePath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFilePath))
            {
                File.Delete(temporaryFilePath);
            }
        }
    }
}
