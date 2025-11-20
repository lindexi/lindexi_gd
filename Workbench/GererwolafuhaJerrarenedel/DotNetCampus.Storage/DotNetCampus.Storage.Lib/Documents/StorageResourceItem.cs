namespace DotNetCampus.Storage.Lib.Documents;

/// <summary>
/// 表示资源内容
/// </summary>
public class StorageResourceItem : IStorageItem
{
    public required string RelativePath { get; init; }

    public required string ResourceId { get; init; }
}