using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;

/// <summary>
/// 表示文件为单位的存储信息
/// </summary>
public class StorageFileItem : IStorageItem
{
    public required StorageFileRelativePath RelativePath { get; init; }

    public required StorageNode RootStorageNode { get; init; }
}