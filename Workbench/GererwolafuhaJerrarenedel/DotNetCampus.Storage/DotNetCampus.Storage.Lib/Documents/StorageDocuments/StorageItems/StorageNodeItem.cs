using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;

/// <summary>
/// 表示 <see cref="StorageNode"/> 为单位的存储信息
/// </summary>
/// 此项作用是如 Presentation 的 StorageNode 应该存放到 Presentation.xml 里面，如此的表示方式。由于决定让 SaveInfo 先到 StorageNode 再到文件的存储方式，于是就存在如此的一个中间状态： 一份文档包含多个 SaveInfo 对象，每个 SaveInfo 都应该被转换为一个顶层的 StorageNode 对象，每个 StorageNode 对象最终都会落到不同的文件里面。于是就需要有一个结构包含 StorageNode 和它应该落到的文件路径
/// 刚好，对于文档来说，除了存储数据之外，还应该有资源数据。那就是 <see cref="StorageResourceItem"/> 对象了
public class StorageNodeItem : IStorageItem
{
    public required StorageFileRelativePath RelativePath { get; init; }

    public required StorageNode RootStorageNode { get; init; }
}