using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

            MessageSignature[] requestMessages = BuildMessageSignatures(
                conversation.Messages,
                startIndex: 0,
                conversation.RequestMessageCount);
            MessageSignature[] responseMessages = BuildMessageSignatures(
                conversation.Messages,
                conversation.RequestMessageCount,
                conversation.Messages.Count - conversation.RequestMessageCount);
            string[] responseToolCallIds = GetResponseToolCallIds(
                conversation.Messages,
                conversation.RequestMessageCount);
            sourceSessions.Add(new SourceSession(
                sessionDirectory,
                snapshotDirectory,
                directoryName,
                GetSessionSortTimestamp(sessionDirectory),
                requestMessages,
                responseMessages,
                responseToolCallIds,
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
        int[] parentIndexes = new int[sourceSessions.Count];
        Array.Fill(parentIndexes, -1);
        int[] childCounts = new int[sourceSessions.Count];

        for (int childIndex = 0; childIndex < sourceSessions.Count; childIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SourceSession child = sourceSessions[childIndex];
            int bestParentIndex = -1;
            int bestContinuationMessageCount = -1;

            for (int parentIndex = childIndex - 1; parentIndex >= 0; parentIndex--)
            {
                SourceSession candidate = sourceSessions[parentIndex];
                int continuationMessageCount = candidate.RequestMessages.Length
                                               + candidate.ResponseMessages.Length;
                if (continuationMessageCount <= bestContinuationMessageCount
                    || !IsStrictContinuation(candidate, child))
                {
                    continue;
                }

                bestParentIndex = parentIndex;
                bestContinuationMessageCount = continuationMessageCount;
            }

            parentIndexes[childIndex] = bestParentIndex;
            if (bestParentIndex >= 0)
            {
                childCounts[bestParentIndex]++;
            }
        }

        List<SessionChain> chains = new(sourceSessions.Count);
        for (int sessionIndex = 0; sessionIndex < sourceSessions.Count; sessionIndex++)
        {
            SourceSession terminalSession = sourceSessions[sessionIndex];
            if (childCounts[sessionIndex] > 0)
            {
                continue;
            }

            List<SourceSession> chainSessions = [];
            int currentIndex = sessionIndex;
            while (currentIndex >= 0)
            {
                SourceSession currentSession = sourceSessions[currentIndex];
                chainSessions.Add(currentSession);
                currentIndex = parentIndexes[currentIndex];
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
        if (parent.ResponseMessages.Length == 0)
        {
            return false;
        }

        int expectedPrefixCount = parent.RequestMessages.Length + parent.ResponseMessages.Length;
        if (child.RequestMessages.Length <= expectedPrefixCount)
        {
            return false;
        }

        for (int index = 0; index < parent.RequestMessages.Length; index++)
        {
            if (!AreEquivalent(parent.RequestMessages[index], child.RequestMessages[index]))
            {
                return false;
            }
        }

        for (int index = 0; index < parent.ResponseMessages.Length; index++)
        {
            if (!AreEquivalent(
                    parent.ResponseMessages[index],
                    child.RequestMessages[parent.RequestMessages.Length + index]))
            {
                return false;
            }
        }

        return IsToolRoundTrip(
            parent.ResponseToolCallIds,
            child.RequestMessages,
            expectedPrefixCount);
    }

    private static bool IsToolRoundTrip(
        IReadOnlyList<string> responseToolCallIds,
        MessageSignature[] childRequestMessages,
        int appendedMessageStartIndex)
    {
        if (responseToolCallIds.Count == 0)
        {
            return false;
        }

        HashSet<string> pendingToolCallIds = new(responseToolCallIds.Count, StringComparer.Ordinal);
        foreach (string toolCallId in responseToolCallIds)
        {
            if (string.IsNullOrWhiteSpace(toolCallId) || !pendingToolCallIds.Add(toolCallId))
            {
                return false;
            }
        }

        int appendedMessageCount = childRequestMessages.Length - appendedMessageStartIndex;
        if (appendedMessageCount != pendingToolCallIds.Count)
        {
            return false;
        }

        for (int index = appendedMessageStartIndex; index < childRequestMessages.Length; index++)
        {
            MessageSignature message = childRequestMessages[index];
            if (message.Role != LogChatRole.Tool
                || string.IsNullOrWhiteSpace(message.ToolCallId)
                || !pendingToolCallIds.Remove(message.ToolCallId))
            {
                return false;
            }
        }

        return pendingToolCallIds.Count == 0;
    }

    private static bool AreEquivalent(MessageSignature left, MessageSignature right)
    {
        return left.Fingerprint == right.Fingerprint;
    }

    private static MessageSignature[] BuildMessageSignatures(
        IReadOnlyList<LogChatMessage> messages,
        int startIndex,
        int count)
    {
        if (count == 0)
        {
            return [];
        }

        MessageSignature[] signatures = new MessageSignature[count];
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        for (int index = 0; index < count; index++)
        {
            LogChatMessage message = messages[startIndex + index];
            signatures[index] = CreateMessageSignature(message, hash);
        }

        return signatures;
    }

    private static MessageSignature CreateMessageSignature(
        LogChatMessage message,
        IncrementalHash hash)
    {
        AppendInt32(hash, (int) message.Role);
        AppendOrdinalIgnoreCaseString(hash, message.RawRole);
        AppendString(hash, message.Content);
        AppendString(hash, message.ReasoningContent);
        AppendString(hash, message.Name);
        AppendString(hash, message.ToolCallId);
        AppendInt32(hash, message.ToolCalls.Count);
        foreach (LogToolCall toolCall in message.ToolCalls)
        {
            AppendInt32(hash, toolCall.Index);
            AppendString(hash, toolCall.Id);
            AppendString(hash, toolCall.Type);
            AppendString(hash, toolCall.Name);
            AppendString(hash, toolCall.Arguments);
        }

        Span<byte> fingerprintBytes = stackalloc byte[32];
        if (!hash.TryGetHashAndReset(fingerprintBytes, out int bytesWritten)
            || bytesWritten != fingerprintBytes.Length)
        {
            throw new CryptographicException("无法计算日志消息指纹。");
        }

        var fingerprint = new MessageFingerprint(
            BinaryPrimitives.ReadUInt64LittleEndian(fingerprintBytes),
            BinaryPrimitives.ReadUInt64LittleEndian(fingerprintBytes[8..]),
            BinaryPrimitives.ReadUInt64LittleEndian(fingerprintBytes[16..]),
            BinaryPrimitives.ReadUInt64LittleEndian(fingerprintBytes[24..]));
        string toolCallId = message.Role == LogChatRole.Tool
            ? message.ToolCallId
            : string.Empty;
        return new MessageSignature(fingerprint, message.Role, toolCallId);
    }

    private static string[] GetResponseToolCallIds(
        IReadOnlyList<LogChatMessage> messages,
        int responseStartIndex)
    {
        int toolCallCount = 0;
        for (int index = responseStartIndex; index < messages.Count; index++)
        {
            toolCallCount += messages[index].ToolCalls.Count;
        }

        if (toolCallCount == 0)
        {
            return [];
        }

        string[] toolCallIds = new string[toolCallCount];
        int toolCallIndex = 0;
        for (int index = responseStartIndex; index < messages.Count; index++)
        {
            foreach (LogToolCall toolCall in messages[index].ToolCalls)
            {
                toolCallIds[toolCallIndex++] = toolCall.Id;
            }
        }

        return toolCallIds;
    }

    private static void AppendInt32(IncrementalHash hash, int value)
    {
        Span<byte> valueBytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(valueBytes, value);
        hash.AppendData(valueBytes);
    }

    private static void AppendString(IncrementalHash hash, string value)
    {
        AppendInt32(hash, value.Length);
        if (value.Length > 0)
        {
            hash.AppendData(MemoryMarshal.AsBytes(value.AsSpan()));
        }
    }

    private static void AppendOrdinalIgnoreCaseString(IncrementalHash hash, string value)
    {
        AppendInt32(hash, value.Length);
        Span<char> normalizedCharacters = stackalloc char[64];
        int offset = 0;
        while (offset < value.Length)
        {
            int chunkLength = Math.Min(normalizedCharacters.Length, value.Length - offset);
            for (int index = 0; index < chunkLength; index++)
            {
                normalizedCharacters[index] = char.ToUpperInvariant(value[offset + index]);
            }

            hash.AppendData(MemoryMarshal.AsBytes(normalizedCharacters[..chunkLength]));
            offset += chunkLength;
        }
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
        MessageSignature[] RequestMessages,
        MessageSignature[] ResponseMessages,
        IReadOnlyList<string> ResponseToolCallIds,
        bool HasResponse);

    private readonly record struct MessageSignature(
        MessageFingerprint Fingerprint,
        LogChatRole Role,
        string ToolCallId);

    private readonly record struct MessageFingerprint(
        ulong Part1,
        ulong Part2,
        ulong Part3,
        ulong Part4);

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
