namespace DotNetCampus.Storage.StorageFiles;

public readonly record struct HashCache()
{
    public required HashResult HashResult { get; init; }
    public required DateTime LastWriteTime { get; init; }
    public required long FileSize { get; init; }
}