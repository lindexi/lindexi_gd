using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;

/// <summary>
/// 存储项。分为资源和 <see cref="StorageNode"/> 项
/// </summary>
public interface IStorageItem
{
    /// <summary>
    /// 存储项应该相对的路径
    /// </summary>
    StorageFileRelativePath RelativePath { get; }
}