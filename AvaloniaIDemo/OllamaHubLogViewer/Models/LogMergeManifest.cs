using System;

namespace OllamaHubLogViewer.Models;

internal sealed record LogMergeManifest(
    int FormatVersion,
    string SourceRootFingerprint,
    string MergedSessionId,
    string TerminalSourceDirectoryName,
    string RequestSha256,
    string ResponseSha256,
    DateTimeOffset GeneratedAtUtc,
    string[] SourceDirectoryNames);

internal sealed record LogMergeIndex(
    int FormatVersion,
    string SourceRootFingerprint,
    DateTimeOffset GeneratedAtUtc,
    LogMergeIndexEntry[] Sessions);

internal sealed record LogMergeIndexEntry(
    string MergedSessionDirectoryName,
    string TerminalSourceDirectoryName,
    DateTimeOffset SortTimestamp,
    string[] SourceDirectoryNames);
