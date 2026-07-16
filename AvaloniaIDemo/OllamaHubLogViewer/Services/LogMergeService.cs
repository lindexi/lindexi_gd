using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OllamaHubLogViewer.Models;

namespace OllamaHubLogViewer.Services;

internal sealed class LogMergeService
{
    private const int CurrentFormatVersion = 2;
    private const string IndexFileName = "merge-index.json";
    private const string ManifestFileName = "merge-manifest.json";
    private const string RequestFileName = "request.log";
    private const string ResponseFileName = "response.log";
    private const string SnapshotDirectoryPrefix = "merged-";
    private const string SourceSnapshotDirectoryName = ".source-snapshots";
    private const string DirectoryTimestampFormat = "yyyy-MM-dd_HH-mm-ss";
    private static readonly TimeSpan ActiveLogGracePeriod = TimeSpan.FromMinutes(1);
    private readonly OpenAiLogLoader _logLoader;

    public LogMergeService()
        : this(new OpenAiLogLoader())
    {
    }

    internal LogMergeService(OpenAiLogLoader logLoader)
    {
        ArgumentNullException.ThrowIfNull(logLoader);
        _logLoader = logLoader;
    }

    public async Task<LogMergeResult> RebuildAsync(
        string sourceRootPath,
        string outputRootPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRootPath);

        string fullSourceRootPath = Path.GetFullPath(sourceRootPath);
        if (!Directory.Exists(fullSourceRootPath))
        {
            throw new DirectoryNotFoundException(fullSourceRootPath);
        }

        string fullOutputRootPath = Path.GetFullPath(outputRootPath);
        Directory.CreateDirectory(fullOutputRootPath);
        string sourceRootFingerprint = ComputeSourceRootFingerprint(fullSourceRootPath);
        string sourceOutputRootPath = Path.Join(fullOutputRootPath, sourceRootFingerprint);
        await using FileStream outputLock = await AcquireOutputLockAsync(
                $"{sourceOutputRootPath}.merge.lock",
                cancellationToken)
            .ConfigureAwait(false);
        string stagingRootPath = $"{sourceOutputRootPath}.building-{Guid.NewGuid():N}";
        Directory.CreateDirectory(stagingRootPath);

        try
        {
            IReadOnlyList<SourceSession> sourceSessions = await LoadSourceSessionsAsync(
                    fullSourceRootPath,
                    stagingRootPath,
                    cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<SessionChain> chains = BuildSessionChains(sourceSessions, cancellationToken);
            List<LogMergeIndexEntry> indexEntries = new(chains.Count);

            foreach (SessionChain chain in chains)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LogMergeIndexEntry indexEntry = await WriteSnapshotAsync(
                        chain,
                        sourceRootFingerprint,
                        stagingRootPath,
                        cancellationToken)
                    .ConfigureAwait(false);
                indexEntries.Add(indexEntry);
            }

            var index = new LogMergeIndex(
                CurrentFormatVersion,
                sourceRootFingerprint,
                DateTimeOffset.UtcNow,
                indexEntries.ToArray());
            await WriteJsonAsync(
                    Path.Join(stagingRootPath, IndexFileName),
                    index,
                    LogMergeJsonContext.Default.LogMergeIndex,
                    cancellationToken)
                .ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            await ValidateSourceSnapshotsAsync(sourceSessions, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await PublishOutputDirectoryAsync(
                    stagingRootPath,
                    sourceOutputRootPath,
                    sourceRootFingerprint,
                    index,
                    sourceSessions,
                    cancellationToken)
                .ConfigureAwait(false);
            return BuildResult(index, sourceOutputRootPath);
        }
        catch
        {
            TryDeleteDirectory(stagingRootPath);
            throw;
        }
    }

    public async Task<LogMergeResult> LoadExistingAsync(
        string sourceRootPath,
        string outputRootPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRootPath);

        string fullSourceRootPath = Path.GetFullPath(sourceRootPath);
        string sourceRootFingerprint = ComputeSourceRootFingerprint(fullSourceRootPath);
        string sourceOutputRootPath = Path.Join(Path.GetFullPath(outputRootPath), sourceRootFingerprint);
        string indexPath = Path.Join(sourceOutputRootPath, IndexFileName);
        if (!File.Exists(indexPath))
        {
            return LogMergeResult.Empty(sourceOutputRootPath);
        }

        try
        {
            await using FileStream stream = OpenRead(indexPath);
            LogMergeIndex? index = await JsonSerializer
                .DeserializeAsync(
                    stream,
                    LogMergeJsonContext.Default.LogMergeIndex,
                    cancellationToken)
                .ConfigureAwait(false);
            if (index is null
                || index.FormatVersion != CurrentFormatVersion
                || index.Sessions is null
                || !string.Equals(
                    index.SourceRootFingerprint,
                    sourceRootFingerprint,
                    StringComparison.Ordinal))
            {
                return LogMergeResult.Empty(sourceOutputRootPath);
            }

            LogMergeIndex validatedIndex = await ValidateExistingIndexAsync(
                    index,
                    sourceOutputRootPath,
                    sourceRootFingerprint,
                    cancellationToken)
                .ConfigureAwait(false);
            return BuildResult(validatedIndex, sourceOutputRootPath);
        }
        catch (JsonException exception)
        {
            Trace.TraceWarning("无法解析合并日志索引 {0}：{1}", indexPath, exception.Message);
            return LogMergeResult.Empty(sourceOutputRootPath);
        }
        catch (IOException exception)
        {
            Trace.TraceWarning("无法读取合并日志索引 {0}：{1}", indexPath, exception.Message);
            return LogMergeResult.Empty(sourceOutputRootPath);
        }
        catch (UnauthorizedAccessException exception)
        {
            Trace.TraceWarning("没有权限读取合并日志索引 {0}：{1}", indexPath, exception.Message);
            return LogMergeResult.Empty(sourceOutputRootPath);
        }
        catch (ArgumentException exception)
        {
            Trace.TraceWarning("合并日志索引包含无效路径 {0}：{1}", indexPath, exception.Message);
            return LogMergeResult.Empty(sourceOutputRootPath);
        }
    }

    private async Task<IReadOnlyList<SourceSession>> LoadSourceSessionsAsync(
        string sourceRootPath,
        string stagingRootPath,
        CancellationToken cancellationToken)
    {
        string[] sessionDirectories = Directory
            .EnumerateDirectories(sourceRootPath)
            .Where(static directory => File.Exists(Path.Join(directory, RequestFileName)))
            .OrderBy(static directory => Path.GetFileName(directory), StringComparer.Ordinal)
            .ToArray();
        List<SourceSession> sourceSessions = new(sessionDirectories.Length);
        string sourceSnapshotRootPath = Path.Join(stagingRootPath, SourceSnapshotDirectoryName);
        Directory.CreateDirectory(sourceSnapshotRootPath);

        foreach (string sessionDirectory in sessionDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string directoryName = Path.GetFileName(sessionDirectory);
            string snapshotDirectory = Path.Join(sourceSnapshotRootPath, directoryName);
            Directory.CreateDirectory(snapshotDirectory);

            bool copiedRequest = await TryCopyStableFileAsync(
                    Path.Join(sessionDirectory, RequestFileName),
                    Path.Join(snapshotDirectory, RequestFileName),
                    cancellationToken)
                .ConfigureAwait(false);
            string sourceResponsePath = Path.Join(sessionDirectory, ResponseFileName);
            bool sourceHasResponse = File.Exists(sourceResponsePath);
            bool copiedResponse = !sourceHasResponse
                                  || await TryCopyStableFileAsync(
                                          sourceResponsePath,
                                          Path.Join(snapshotDirectory, ResponseFileName),
                                          cancellationToken)
                                      .ConfigureAwait(false);
            if (!copiedRequest || !copiedResponse)
            {
                throw new LogSourceChangedException(directoryName);
            }

            LogConversation conversation = await _logLoader
                .LoadAsync(snapshotDirectory, cancellationToken)
                .ConfigureAwait(false);

            string sourceRequestPath = Path.Join(sessionDirectory, RequestFileName);
            if (!conversation.RequestParseSucceeded)
            {
                if (WasRecentlyModified(sourceRequestPath))
                {
                    throw new LogSourceChangedException(directoryName);
                }

                Trace.TraceWarning("跳过长期静止但无法解析请求的日志目录 {0}。", sessionDirectory);
                continue;
            }

            if (!sourceHasResponse)
            {
                if (WasRecentlyModified(sessionDirectory))
                {
                    throw new LogSourceChangedException(directoryName);
                }

                Trace.TraceInformation("暂不合并尚无响应的日志目录 {0}。", sessionDirectory);
                continue;
            }

            if (conversation.InvalidResponseLineCount > 0 || !conversation.ResponseCompleted)
            {
                if (WasRecentlyModified(sourceResponsePath))
                {
                throw new LogSourceChangedException(directoryName);
                }

                Trace.TraceWarning("跳过长期静止但响应不完整的日志目录 {0}。", sessionDirectory);
                continue;
            }

            if (conversation.RequestMessageCount == 0)
            {
                Trace.TraceWarning("跳过没有请求消息的日志目录 {0}。", sessionDirectory);
                continue;
            }

            IReadOnlyList<LogChatMessage> requestMessages = conversation.Messages
                .Take(conversation.RequestMessageCount)
                .ToArray();
            IReadOnlyList<LogChatMessage> responseMessages = conversation.Messages
                .Skip(conversation.RequestMessageCount)
                .ToArray();
            sourceSessions.Add(new SourceSession(
                sessionDirectory,
                snapshotDirectory,
                directoryName,
                GetSessionSortTimestamp(sessionDirectory),
                requestMessages,
                responseMessages,
                sourceHasResponse));
        }

        return sourceSessions
            .OrderBy(static session => session.SortTimestamp)
            .ThenBy(static session => session.DirectoryName, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<SessionChain> BuildSessionChains(
        IReadOnlyList<SourceSession> sourceSessions,
        CancellationToken cancellationToken)
    {
        SourceSession?[] parents = new SourceSession?[sourceSessions.Count];
        var childCounts = new Dictionary<SourceSession, int>(sourceSessions.Count);

        for (int childIndex = 0; childIndex < sourceSessions.Count; childIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SourceSession child = sourceSessions[childIndex];
            SourceSession? parent = null;
            int bestContinuationMessageCount = -1;

            for (int parentIndex = childIndex - 1; parentIndex >= 0; parentIndex--)
            {
                SourceSession candidate = sourceSessions[parentIndex];
                int continuationMessageCount = candidate.RequestMessages.Count
                                               + candidate.ResponseMessages.Count;
                if (continuationMessageCount <= bestContinuationMessageCount
                    || !IsStrictContinuation(candidate, child))
                {
                    continue;
                }

                parent = candidate;
                bestContinuationMessageCount = continuationMessageCount;
            }

            parents[childIndex] = parent;
            childCounts.TryAdd(child, 0);
            if (parent is not null)
            {
                childCounts[parent] = childCounts.GetValueOrDefault(parent) + 1;
            }
        }

        List<SessionChain> chains = [];
        for (int sessionIndex = 0; sessionIndex < sourceSessions.Count; sessionIndex++)
        {
            SourceSession terminalSession = sourceSessions[sessionIndex];
            if (childCounts.GetValueOrDefault(terminalSession) > 0)
            {
                continue;
            }

            List<SourceSession> chainSessions = [];
            int currentIndex = sessionIndex;
            while (currentIndex >= 0)
            {
                SourceSession currentSession = sourceSessions[currentIndex];
                chainSessions.Add(currentSession);
                SourceSession? parent = parents[currentIndex];
                currentIndex = parent is null
                    ? -1
                    : FindSessionIndex(sourceSessions, parent, currentIndex - 1);
            }

            chainSessions.Reverse();
            if (chainSessions.Count > 1)
            {
                chains.Add(new SessionChain(chainSessions));
            }
        }

        return chains;
    }

    private static bool IsStrictContinuation(SourceSession parent, SourceSession child)
    {
        if (parent.ResponseMessages.Count == 0)
        {
            return false;
        }

        int expectedPrefixCount = parent.RequestMessages.Count + parent.ResponseMessages.Count;
        if (child.RequestMessages.Count <= expectedPrefixCount)
        {
            return false;
        }

        for (int index = 0; index < parent.RequestMessages.Count; index++)
        {
            if (!AreEquivalent(parent.RequestMessages[index], child.RequestMessages[index]))
            {
                return false;
            }
        }

        for (int index = 0; index < parent.ResponseMessages.Count; index++)
        {
            if (!AreEquivalent(
                    parent.ResponseMessages[index],
                    child.RequestMessages[parent.RequestMessages.Count + index]))
            {
                return false;
            }
        }

        IReadOnlyList<LogChatMessage> appendedMessages = child.RequestMessages
            .Skip(expectedPrefixCount)
            .ToArray();
        return IsToolRoundTrip(parent.ResponseMessages, appendedMessages);
    }

    private static bool IsToolRoundTrip(
        IReadOnlyList<LogChatMessage> responseMessages,
        IReadOnlyList<LogChatMessage> appendedMessages)
    {
        LogToolCall[] toolCalls = responseMessages
            .SelectMany(static message => message.ToolCalls)
            .ToArray();
        if (toolCalls.Length == 0
            || toolCalls.Any(static toolCall => string.IsNullOrWhiteSpace(toolCall.Id)))
        {
            return false;
        }

        HashSet<string> pendingToolCallIds = toolCalls
            .Select(static toolCall => toolCall.Id)
            .ToHashSet(StringComparer.Ordinal);
        if (pendingToolCallIds.Count != toolCalls.Length
            || appendedMessages.Count != pendingToolCallIds.Count)
        {
            return false;
        }

        foreach (LogChatMessage message in appendedMessages)
        {
            if (message.Role != LogChatRole.Tool
                || string.IsNullOrWhiteSpace(message.ToolCallId)
                || !pendingToolCallIds.Remove(message.ToolCallId))
            {
                return false;
            }
        }

        return pendingToolCallIds.Count == 0;
    }

    private static bool AreEquivalent(LogChatMessage left, LogChatMessage right)
    {
        if (left.Role != right.Role
            || !string.Equals(left.RawRole, right.RawRole, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(left.Content, right.Content, StringComparison.Ordinal)
            || !string.Equals(left.ReasoningContent, right.ReasoningContent, StringComparison.Ordinal)
            || !string.Equals(left.Name, right.Name, StringComparison.Ordinal)
            || !string.Equals(left.ToolCallId, right.ToolCallId, StringComparison.Ordinal)
            || left.ToolCalls.Count != right.ToolCalls.Count)
        {
            return false;
        }

        for (int index = 0; index < left.ToolCalls.Count; index++)
        {
            LogToolCall leftToolCall = left.ToolCalls[index];
            LogToolCall rightToolCall = right.ToolCalls[index];
            if (leftToolCall.Index != rightToolCall.Index
                || !string.Equals(leftToolCall.Id, rightToolCall.Id, StringComparison.Ordinal)
                || !string.Equals(leftToolCall.Type, rightToolCall.Type, StringComparison.Ordinal)
                || !string.Equals(leftToolCall.Name, rightToolCall.Name, StringComparison.Ordinal)
                || !string.Equals(leftToolCall.Arguments, rightToolCall.Arguments, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<LogMergeIndexEntry> WriteSnapshotAsync(
        SessionChain chain,
        string sourceRootFingerprint,
        string stagingRootPath,
        CancellationToken cancellationToken)
    {
        SourceSession terminalSession = chain.Sessions[^1];
        string mergedSessionId = await ComputeMergedSessionIdAsync(chain.Sessions, cancellationToken)
            .ConfigureAwait(false);
        string mergedSessionDirectoryName = $"{SnapshotDirectoryPrefix}{terminalSession.DirectoryName}-{mergedSessionId}";
        string mergedSessionDirectoryPath = Path.Join(stagingRootPath, mergedSessionDirectoryName);
        Directory.CreateDirectory(mergedSessionDirectoryPath);

        await CopyFileAsync(
                Path.Join(terminalSession.SnapshotDirectoryPath, RequestFileName),
                Path.Join(mergedSessionDirectoryPath, RequestFileName),
                cancellationToken)
            .ConfigureAwait(false);
        string responsePath = Path.Join(terminalSession.SnapshotDirectoryPath, ResponseFileName);
        if (terminalSession.HasResponse && File.Exists(responsePath))
        {
            await CopyFileAsync(
                    responsePath,
                    Path.Join(mergedSessionDirectoryPath, ResponseFileName),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        string[] sourceDirectoryNames = chain.Sessions
            .Select(static session => session.DirectoryName)
            .ToArray();
        string requestSha256 = Convert.ToHexString(await ComputeFileHashAsync(
                Path.Join(terminalSession.SnapshotDirectoryPath, RequestFileName),
                cancellationToken)
            .ConfigureAwait(false));
        string responseSha256 = terminalSession.HasResponse && File.Exists(responsePath)
            ? Convert.ToHexString(await ComputeFileHashAsync(responsePath, cancellationToken)
                .ConfigureAwait(false))
            : string.Empty;
        var manifest = new LogMergeManifest(
            CurrentFormatVersion,
            sourceRootFingerprint,
            mergedSessionId,
            terminalSession.DirectoryName,
            requestSha256,
            responseSha256,
            DateTimeOffset.UtcNow,
            sourceDirectoryNames);
        await WriteJsonAsync(
                Path.Join(mergedSessionDirectoryPath, ManifestFileName),
                manifest,
                LogMergeJsonContext.Default.LogMergeManifest,
                cancellationToken)
            .ConfigureAwait(false);

        return new LogMergeIndexEntry(
            mergedSessionDirectoryName,
            terminalSession.DirectoryName,
            terminalSession.SortTimestamp,
            sourceDirectoryNames);
    }

    private static async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using FileStream sourceStream = OpenRead(sourcePath);
        await using FileStream destinationStream = new(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> TryCopyStableFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FileSnapshot before;
            try
            {
                before = GetFileSnapshot(sourcePath);
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                await CopyFileAsync(sourcePath, destinationPath, cancellationToken).ConfigureAwait(false);
                FileSnapshot after = GetFileSnapshot(sourcePath);
                long copiedLength = new FileInfo(destinationPath).Length;
                if (before == after && copiedLength == after.Length)
                {
                    byte[] sourceHash;
                    sourceHash = await ComputeFileHashAsync(sourcePath, cancellationToken)
                        .ConfigureAwait(false);

                    byte[] destinationHash;
                    destinationHash = await ComputeFileHashAsync(destinationPath, cancellationToken)
                        .ConfigureAwait(false);

                    FileSnapshot afterHash = GetFileSnapshot(sourcePath);
                    if (after == afterHash && sourceHash.AsSpan().SequenceEqual(destinationHash))
                    {
                        return true;
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }

            if (attempt == 0)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    private static FileSnapshot GetFileSnapshot(string path)
    {
        var fileInfo = new FileInfo(path);
        fileInfo.Refresh();
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("日志文件不存在。", path);
        }

        return new FileSnapshot(fileInfo.Length, fileInfo.LastWriteTimeUtc);
    }

    private static async Task<byte[]> ComputeFileHashAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = OpenRead(path);
        return await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ValidateSourceSnapshotsAsync(
        IReadOnlyList<SourceSession> sourceSessions,
        CancellationToken cancellationToken)
    {
        foreach (SourceSession sourceSession in sourceSessions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await FilesMatchAsync(
                    Path.Join(sourceSession.SourceDirectoryPath, RequestFileName),
                    Path.Join(sourceSession.SnapshotDirectoryPath, RequestFileName),
                    cancellationToken)
                .ConfigureAwait(false))
            {
                throw new LogSourceChangedException(sourceSession.DirectoryName);
            }

            string sourceResponsePath = Path.Join(sourceSession.SourceDirectoryPath, ResponseFileName);
            string snapshotResponsePath = Path.Join(sourceSession.SnapshotDirectoryPath, ResponseFileName);
            if (sourceSession.HasResponse
                && !await FilesMatchAsync(sourceResponsePath, snapshotResponsePath, cancellationToken)
                    .ConfigureAwait(false))
            {
                throw new LogSourceChangedException(sourceSession.DirectoryName);
            }
        }
    }

    private static async Task<bool> FilesMatchAsync(
        string leftPath,
        string rightPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(leftPath) || !File.Exists(rightPath))
        {
            return false;
        }

        byte[] leftHash = await ComputeFileHashAsync(leftPath, cancellationToken).ConfigureAwait(false);
        byte[] rightHash = await ComputeFileHashAsync(rightPath, cancellationToken).ConfigureAwait(false);

        return leftHash.AsSpan().SequenceEqual(rightHash);
    }

    private static async Task<LogMergeIndex> ValidateExistingIndexAsync(
        LogMergeIndex index,
        string sourceOutputRootPath,
        string sourceRootFingerprint,
        CancellationToken cancellationToken)
    {
        List<LogMergeIndexEntry> validEntries = new(index.Sessions.Length);
        foreach (LogMergeIndexEntry? entry in index.Sessions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry is not null
                && await TryValidateSnapshotAsync(
                        entry,
                        sourceOutputRootPath,
                        sourceRootFingerprint,
                        cancellationToken)
                    .ConfigureAwait(false))
            {
                validEntries.Add(entry);
            }
        }

        return index with { Sessions = validEntries.ToArray() };
    }

    private static async Task<bool> TryValidateSnapshotAsync(
        LogMergeIndexEntry entry,
        string sourceOutputRootPath,
        string sourceRootFingerprint,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entry.MergedSessionDirectoryName)
                || string.IsNullOrWhiteSpace(entry.TerminalSourceDirectoryName)
                || entry.SourceDirectoryNames is null
                || entry.SourceDirectoryNames.Length == 0
                || entry.SourceDirectoryNames.Any(static name => string.IsNullOrWhiteSpace(name))
                || !string.Equals(
                    entry.SourceDirectoryNames[^1],
                    entry.TerminalSourceDirectoryName,
                    StringComparison.Ordinal))
            {
                return false;
            }

            string mergedDirectoryPath = GetContainedMergedDirectoryPath(
                sourceOutputRootPath,
                entry.MergedSessionDirectoryName);
            string manifestPath = Path.Join(mergedDirectoryPath, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                return false;
            }

            await using FileStream stream = OpenRead(manifestPath);
            LogMergeManifest? manifest = await JsonSerializer
                .DeserializeAsync(
                    stream,
                    LogMergeJsonContext.Default.LogMergeManifest,
                    cancellationToken)
                .ConfigureAwait(false);
            if (manifest is null
                || manifest.FormatVersion != CurrentFormatVersion
                || string.IsNullOrWhiteSpace(manifest.SourceRootFingerprint)
                || string.IsNullOrWhiteSpace(manifest.MergedSessionId)
                || string.IsNullOrWhiteSpace(manifest.TerminalSourceDirectoryName)
                || string.IsNullOrWhiteSpace(manifest.RequestSha256)
                || string.IsNullOrWhiteSpace(manifest.ResponseSha256)
                || manifest.SourceDirectoryNames is null
                || manifest.SourceDirectoryNames.Length == 0
                || manifest.SourceDirectoryNames.Any(static name => string.IsNullOrWhiteSpace(name))
                || !string.Equals(
                    manifest.SourceRootFingerprint,
                    sourceRootFingerprint,
                    StringComparison.Ordinal)
                || !string.Equals(
                    manifest.TerminalSourceDirectoryName,
                    entry.TerminalSourceDirectoryName,
                    StringComparison.Ordinal)
                || !manifest.SourceDirectoryNames.SequenceEqual(
                    entry.SourceDirectoryNames,
                    StringComparer.Ordinal))
            {
                return false;
            }

            string requestPath = Path.Join(mergedDirectoryPath, RequestFileName);
            string responsePath = Path.Join(mergedDirectoryPath, ResponseFileName);
            if (!File.Exists(requestPath) || !File.Exists(responsePath))
            {
                return false;
            }

            string requestSha256 = Convert.ToHexString(
                await ComputeFileHashAsync(requestPath, cancellationToken).ConfigureAwait(false));
            string responseSha256 = Convert.ToHexString(
                await ComputeFileHashAsync(responsePath, cancellationToken).ConfigureAwait(false));
            string expectedMergedSessionId = ComputeMergedSessionId(
                entry.SourceDirectoryNames,
                requestSha256,
                responseSha256);
            string expectedDirectoryName = $"{SnapshotDirectoryPrefix}{entry.TerminalSourceDirectoryName}-{expectedMergedSessionId}";
            return string.Equals(requestSha256, manifest.RequestSha256, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(responseSha256, manifest.ResponseSha256, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       manifest.MergedSessionId,
                       expectedMergedSessionId,
                       StringComparison.Ordinal)
                   && string.Equals(
                       entry.MergedSessionDirectoryName,
                       expectedDirectoryName,
                       StringComparison.Ordinal);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            Trace.TraceWarning("跳过损坏的合并日志清单：{0}", exception.Message);
            return false;
        }
        catch (IOException exception)
        {
            Trace.TraceWarning("跳过无法读取的合并日志快照：{0}", exception.Message);
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            Trace.TraceWarning("跳过无权读取的合并日志快照：{0}", exception.Message);
            return false;
        }
        catch (ArgumentException exception)
        {
            Trace.TraceWarning("跳过路径无效的合并日志快照：{0}", exception.Message);
            return false;
        }
    }

    private static string GetContainedMergedDirectoryPath(
        string sourceOutputRootPath,
        string mergedSessionDirectoryName)
    {
        string mergedDirectoryPath = Path.GetFullPath(
            Path.Join(sourceOutputRootPath, mergedSessionDirectoryName));
        if (!string.Equals(
                Path.GetFileName(mergedSessionDirectoryName),
                mergedSessionDirectoryName,
                StringComparison.Ordinal))
        {
            throw new ArgumentException("合并日志目录名必须是单一子目录。", nameof(mergedSessionDirectoryName));
        }

        string relativeMergedDirectoryPath = Path.GetRelativePath(
            sourceOutputRootPath,
            mergedDirectoryPath);
        if (relativeMergedDirectoryPath.StartsWith("..", StringComparison.Ordinal)
            || Path.IsPathRooted(relativeMergedDirectoryPath))
        {
            throw new ArgumentException("合并日志目录超出输出根目录。", nameof(mergedSessionDirectoryName));
        }

        return mergedDirectoryPath;
    }

    private static bool WasRecentlyModified(string path)
    {
        DateTime lastWriteTimeUtc = File.Exists(path)
            ? File.GetLastWriteTimeUtc(path)
            : Directory.GetLastWriteTimeUtc(path);
        return DateTime.UtcNow - lastWriteTimeUtc <= ActiveLogGracePeriod;
    }

    private static async Task WriteJsonAsync<T>(
        string path,
        T value,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 16 * 1024,
            useAsync: true);
        await JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo, cancellationToken)
            .ConfigureAwait(false);
    }

    private static FileStream OpenRead(string path)
    {
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 64 * 1024,
            useAsync: true);
    }

    private static async Task<FileStream> AcquireOutputLockAsync(
        string lockPath,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    useAsync: true);
            }
            catch (IOException)
            {
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static LogMergeResult BuildResult(LogMergeIndex index, string sourceOutputRootPath)
    {
        List<LogMergedSession> mergedSessions = new(index.Sessions.Length);
        foreach (LogMergeIndexEntry? entry in index.Sessions)
        {
            if (entry is null
                || string.IsNullOrWhiteSpace(entry.MergedSessionDirectoryName)
                || entry.SourceDirectoryNames is null)
            {
                continue;
            }

            string mergedDirectoryPath;
            try
            {
                mergedDirectoryPath = GetContainedMergedDirectoryPath(
                    sourceOutputRootPath,
                    entry.MergedSessionDirectoryName);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (!Directory.Exists(mergedDirectoryPath)
                || !File.Exists(Path.Join(mergedDirectoryPath, RequestFileName)))
            {
                continue;
            }

            mergedSessions.Add(new LogMergedSession(
                mergedDirectoryPath,
                entry.TerminalSourceDirectoryName,
                entry.SortTimestamp,
                entry.SourceDirectoryNames));
        }

        return new LogMergeResult(sourceOutputRootPath, mergedSessions);
    }

    private static async Task PublishOutputDirectoryAsync(
        string stagingRootPath,
        string sourceOutputRootPath,
        string sourceRootFingerprint,
        LogMergeIndex index,
        IReadOnlyList<SourceSession> sourceSessions,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(sourceOutputRootPath);
        foreach (string stagingSessionDirectory in Directory
                     .EnumerateDirectories(stagingRootPath)
                     .Where(static directory => !string.Equals(
                         Path.GetFileName(directory),
                         SourceSnapshotDirectoryName,
                         StringComparison.Ordinal)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string directoryName = Path.GetFileName(stagingSessionDirectory);
            string targetSessionDirectory = Path.Join(sourceOutputRootPath, directoryName);
            if (Directory.Exists(targetSessionDirectory))
            {
                LogMergeIndexEntry? entry = index.Sessions.FirstOrDefault(candidate =>
                    candidate is not null
                    && string.Equals(
                        candidate.MergedSessionDirectoryName,
                        directoryName,
                        StringComparison.Ordinal));
                bool targetIsValid = entry is not null
                                     && await TryValidateSnapshotAsync(
                                             entry,
                                             sourceOutputRootPath,
                                             sourceRootFingerprint,
                                             cancellationToken)
                                         .ConfigureAwait(false);
                if (targetIsValid)
                {
                    Directory.Delete(stagingSessionDirectory, recursive: true);
                    continue;
                }

                Directory.Delete(targetSessionDirectory, recursive: true);
            }

            Directory.Move(stagingSessionDirectory, targetSessionDirectory);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await ValidateSourceSnapshotsAsync(sourceSessions, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        File.Move(
            Path.Join(stagingRootPath, IndexFileName),
            Path.Join(sourceOutputRootPath, IndexFileName),
            overwrite: true);
        TryDeleteDirectory(stagingRootPath);
    }

    private static string ComputeSourceRootFingerprint(string sourceRootPath)
    {
        string normalizedPath = Path.TrimEndingDirectorySeparator(sourceRootPath);
        if (OperatingSystem.IsWindows())
        {
            normalizedPath = normalizedPath.ToUpperInvariant();
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }

    private static async Task<string> ComputeMergedSessionIdAsync(
        IReadOnlyList<SourceSession> sessions,
        CancellationToken cancellationToken)
    {
        SourceSession terminalSession = sessions[^1];
        byte[] requestHash = await ComputeFileHashAsync(
                Path.Join(terminalSession.SnapshotDirectoryPath, RequestFileName),
                cancellationToken)
            .ConfigureAwait(false);
        string responsePath = Path.Join(terminalSession.SnapshotDirectoryPath, ResponseFileName);
        byte[] responseHash = File.Exists(responsePath)
            ? await ComputeFileHashAsync(responsePath, cancellationToken).ConfigureAwait(false)
            : [];
        return ComputeMergedSessionId(
            sessions.Select(static session => session.DirectoryName),
            Convert.ToHexString(requestHash),
            Convert.ToHexString(responseHash));
    }

    private static string ComputeMergedSessionId(
        IEnumerable<string> sourceDirectoryNames,
        string requestSha256,
        string responseSha256)
    {
        string chainKey = CurrentFormatVersion.ToString(CultureInfo.InvariantCulture)
                          + '\n'
                          + string.Join('\n', sourceDirectoryNames)
                          + '\n'
                          + requestSha256
                          + '\n'
                          + responseSha256;
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(chainKey));
        return Convert.ToHexString(hash.AsSpan(0, 12)).ToLowerInvariant();
    }

    private static DateTimeOffset GetSessionSortTimestamp(string sessionDirectory)
    {
        string directoryName = Path.GetFileName(sessionDirectory);
        if (directoryName.Length >= DirectoryTimestampFormat.Length
            && DateTime.TryParseExact(
                directoryName[..DirectoryTimestampFormat.Length],
                DirectoryTimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime timestamp))
        {
            return new DateTimeOffset(DateTime.SpecifyKind(timestamp, DateTimeKind.Local));
        }

        string requestPath = Path.Join(sessionDirectory, RequestFileName);
        DateTime lastWriteTime = File.Exists(requestPath)
            ? File.GetLastWriteTime(requestPath)
            : Directory.GetLastWriteTime(sessionDirectory);
        return new DateTimeOffset(lastWriteTime);
    }

    private static int FindSessionIndex(
        IReadOnlyList<SourceSession> sourceSessions,
        SourceSession session,
        int startIndex)
    {
        for (int index = startIndex; index >= 0; index--)
        {
            if (ReferenceEquals(sourceSessions[index], session))
            {
                return index;
            }
        }

        return -1;
    }

    private static void TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException exception)
        {
            Trace.TraceWarning("无法清理合并日志目录 {0}：{1}", path, exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            Trace.TraceWarning("无法清理合并日志目录 {0}：{1}", path, exception.Message);
        }
    }

    private sealed record SourceSession(
        string SourceDirectoryPath,
        string SnapshotDirectoryPath,
        string DirectoryName,
        DateTimeOffset SortTimestamp,
        IReadOnlyList<LogChatMessage> RequestMessages,
        IReadOnlyList<LogChatMessage> ResponseMessages,
        bool HasResponse);

    private readonly record struct FileSnapshot(long Length, DateTime LastWriteTimeUtc);

    private sealed record SessionChain(IReadOnlyList<SourceSession> Sessions);
}

internal sealed class LogSourceChangedException(string sessionDirectoryName)
    : IOException($"日志目录在合并期间仍在变化：{sessionDirectoryName}");

internal sealed record LogMergedSession(
    string DirectoryPath,
    string TerminalSourceDirectoryName,
    DateTimeOffset SortTimestamp,
    IReadOnlyList<string> SourceDirectoryNames);

internal sealed record LogMergeResult(
    string OutputDirectoryPath,
    IReadOnlyList<LogMergedSession> MergedSessions)
{
    public static LogMergeResult Empty(string outputDirectoryPath)
    {
        return new LogMergeResult(outputDirectoryPath, []);
    }
}
