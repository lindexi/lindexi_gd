using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Serialization.XmlSerialization;
using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Serialization;

/// <summary>
/// 复合文档序列化器，用于在复合存储文档和松散文件之间进行转换
/// </summary>
public interface ICompoundStorageDocumentSerializer
{
    /// <summary>
    /// 存储节点序列化器
    /// </summary>
    /// StorageNode -> IStorageFileInfo
    /// 由于不同的对接存储方式不同，必定要让 <see cref="IStorageNodeSerializer"/> 跟着 <see cref="ICompoundStorageDocumentSerializer"/> 配置
    IStorageNodeSerializer StorageNodeSerializer { get; }

    /// <summary>
    /// 从松散文件管理器转换为复合存储文档
    /// </summary>
    /// <param name="fileProvider"></param>
    /// <returns></returns>
    Task<CompoundStorageDocument> DeserializeToCompoundStorageDocumentAsync(IReadOnlyStorageFileManager fileProvider);

    /// <summary>
    /// 从复合文档里面转换为一个纯粹的存放文件信息的管理器，这个文件管理器只存放当前文档用到的文件信息
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    Task<IReadOnlyStorageFileManager> SerializeToStorageFileManagerAsync(CompoundStorageDocument document);
}