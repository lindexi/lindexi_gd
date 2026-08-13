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

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ExecRequest))]
[JsonSerializable(typeof(ChangeDirectoryRequest))]
[JsonSerializable(typeof(WorkingDirectoryResponse))]
[JsonSerializable(typeof(DirectoryListingResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
