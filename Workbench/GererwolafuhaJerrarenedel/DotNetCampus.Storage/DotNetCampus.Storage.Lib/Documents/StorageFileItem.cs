namespace DotNetCampus.Storage.Lib;

/// <summary>
/// 表示文件为单位的存储信息
/// </summary>
public class StorageFileItem : IStorageItem
{
    public required string RelativePath { get; init; }

    public required StorageNode RootStorageNode { get; init; }
}