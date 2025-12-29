using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;

/// <summary>
/// 表示资源内容
/// </summary>
public class StorageResourceItem : IStorageItem
{
    public required StorageFileRelativePath RelativePath { get; init; }

    public required StorageReferenceId ResourceId { get; init; }
}

// 也许资源内容是不需要的，直接从存储里面拿就可以了