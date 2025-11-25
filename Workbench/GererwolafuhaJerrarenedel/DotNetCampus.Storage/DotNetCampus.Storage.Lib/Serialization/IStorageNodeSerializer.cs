using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Serialization;

/// <summary>
/// 存储节点序列化器
/// </summary>
public interface IStorageNodeSerializer
{
    /// <summary>
    /// 从节点序列化到文件
    /// </summary>
    /// <param name="node"></param>
    /// <param name="outputFile"></param>
    /// <returns></returns>
    Task SerializeAsync(StorageNode node, IStorageFileInfo outputFile);

    /// <summary>
    /// 从文件反序列化到节点
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    Task<StorageNode> DeserializeAsync(IReadOnlyStorageFileInfo file);
}