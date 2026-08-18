using System.Text.Json.Serialization;

namespace WinRemoteShell.Shared;

public sealed record ExecRequest(IReadOnlyList<string> Arguments, int? TimeoutSeconds);

public sealed record ChangeDirectoryRequest(string Path);

public sealed record WorkingDirectoryResponse(string Path);

public sealed record DirectoryListingResponse(string Path, IReadOnlyList<RemoteDirectoryEntry> Entries);

public sealed record RemoteDirectoryEntry(
    string Name,
    string FullPath,
    bool IsDirectory,
    long? Length,
    DateTime CreationTimeUtc,
    DateTime LastWriteTimeUtc);

public sealed record ProcessListResponse(IReadOnlyList<RemoteProcessInfo> Processes);

public sealed record RemoteProcessInfo(
    int Id,
    string Name,
    string? FilePath,
    DateTime? StartTimeUtc,
    long? WorkingSetBytes,
    long? PrivateMemoryBytes,
    int? ThreadCount);

public sealed record KillProcessesRequest(int? ProcessId, string? ProcessName, bool KillTree);

public sealed record KillProcessesResponse(IReadOnlyList<KillProcessResult> Processes);

public sealed record KillProcessResult(int Id, string Name, bool Killed, string? Error);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ExecRequest))]
[JsonSerializable(typeof(ChangeDirectoryRequest))]
[JsonSerializable(typeof(WorkingDirectoryResponse))]
[JsonSerializable(typeof(DirectoryListingResponse))]
[JsonSerializable(typeof(ProcessListResponse))]
[JsonSerializable(typeof(KillProcessesRequest))]
[JsonSerializable(typeof(KillProcessesResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
