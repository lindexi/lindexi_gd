using System.Collections.Frozen;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;
using DotNetCampus.Storage.Serialization.XmlSerialization;
using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Serialization;

/// <summary>
/// 复合文档序列化器，用于在复合存储文档和松散文件之间进行转换
/// </summary>
public abstract class CompoundStorageDocumentSerializer : ICompoundStorageDocumentSerializer
{
    public CompoundStorageDocumentSerializer(CompoundStorageDocumentManager manager)
    {
        Manager = manager;
    }

    /// <summary>
    /// 存储节点序列化器
    /// </summary>
    /// StorageNode -> IStorageFileInfo
    /// 由于不同的对接存储方式不同，必定要让 <see cref="IStorageNodeSerializer"/> 跟着 <see cref="ICompoundStorageDocumentSerializer"/> 配置
    public virtual IStorageNodeSerializer StorageNodeSerializer => new StorageXmlSerializer();

    public CompoundStorageDocumentManager Manager { get; }

    public virtual async Task<CompoundStorageDocument> DeserializeToCompoundStorageDocumentAsync(
        IReadOnlyStorageFileManager fileProvider)
    {
        var classificationResult = Filter(fileProvider);

        var compoundStorageDocument = await ToCompoundStorageDocument(classificationResult);
        return compoundStorageDocument;
    }

    public const string DefaultReferenceFileName = "Reference.xml";
    public const string ContentTypesFileName = "[Content_Types].xml";

    protected virtual StorageFileClassificationResult Filter(IReadOnlyStorageFileManager fileProvider)
    {
        var fileList = fileProvider.FileList;

        var referenceFile = fileList.FirstOrDefault(t => string.Equals(t.RelativePath.RelativePath, DefaultReferenceFileName, StringComparison.OrdinalIgnoreCase));

        IReadOnlyList<IReadOnlyStorageFileInfo> referenceResourceManagerFiles;
        if (referenceFile is not null)
        {
            referenceResourceManagerFiles = [referenceFile];
        }
        else
        {
            referenceResourceManagerFiles = [];
        }

        var documentFile = new List<IReadOnlyStorageFileInfo>();
        var resourceFile = new List<IReadOnlyStorageFileInfo>();

        foreach (var fileInfo in fileList)
        {
            var relativePath = fileInfo.RelativePath;
            var extension = Path.GetExtension(relativePath.AsSpan());
            if (extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
            {
                // 后缀是 xml 的，但是不是 Reference.xml 和 [Content_Types].xml 文件的，就是文档文件
                if (string.Equals(relativePath.RelativePath, DefaultReferenceFileName, StringComparison.OrdinalIgnoreCase))
                {
                   continue;
                }

                if (string.Equals(relativePath.RelativePath, ContentTypesFileName, StringComparison.OrdinalIgnoreCase))
                {
                   continue;
                }

                documentFile.Add(fileInfo);
            }
            else
            {
                resourceFile.Add(fileInfo);
            }
        }

        return new StorageFileClassificationResult()
        {
            ResourceFiles = resourceFile,
            DocumentFiles = documentFile,
            ReferenceResourceManagerFiles = referenceResourceManagerFiles,
            FileProvider = fileProvider,
        };
    }

    /// <summary>
    /// 从 <paramref name="referenceStorageNode"/> 存储引用资源的 <see cref="StorageNode"/> 读取数据，写入到引用管理器中
    /// </summary>
    /// <param name="referenceStorageNode"></param>
    /// <param name="referencedManager"></param>
    protected abstract Task AddResourceReferenceAsync(StorageNode referenceStorageNode, IReferencedManager referencedManager);

    /// <summary>
    /// 从过滤之后的结果转换为复合存储文档
    /// </summary>
    /// <param name="classificationResult"></param>
    /// <returns></returns>
    protected virtual async Task<CompoundStorageDocument> ToCompoundStorageDocument(StorageFileClassificationResult classificationResult)
    {
        IReferencedManager referencedManager = Manager.ReferencedManager;
        var fileManager = Manager.StorageFileManager;

        var storageNodeSerializer = StorageNodeSerializer;

        foreach (var fileInfo in classificationResult.ReferenceResourceManagerFiles)
        {
            StorageNode storageNode = await storageNodeSerializer.DeserializeAsync(fileInfo);
            await AddResourceReferenceAsync(storageNode, referencedManager);
        }

        var storageNodeItemList = new List<StorageNodeItem>();

        foreach (var fileInfo in classificationResult.DocumentFiles)
        {
            var storageNode = await storageNodeSerializer.DeserializeAsync(fileInfo);

            storageNodeItemList.Add(new StorageNodeItem()
            {
                RootStorageNode = storageNode,
                RelativePath = fileInfo.RelativePath.RelativePath
            });
        }

        var referenceInfoDictionary = referencedManager.References.ToFrozenDictionary(t => t.FilePath);

        var storageResourceItemList = new List<StorageResourceItem>();

        foreach (var fileInfo in classificationResult.ResourceFiles)
        {
            if (referenceInfoDictionary.TryGetValue(fileInfo.RelativePath, out var referenceInfo))
            {
                // 这是有记录的资源
                storageResourceItemList.Add(new StorageResourceItem()
                {
                    RelativePath = fileInfo.RelativePath,
                    ResourceId = referenceInfo.ReferenceId
                });

                // 可以尝试确保资源存放在本地文件管理器中

                await fileManager.ToLocalStorageFileInfoAsync(fileInfo);
            }
        }

        // 似乎 CompoundStorageDocument 有些废，没有什么用处，毕竟存放的东西本身也在污染 CompoundStorageDocumentManager 管理器
        var storageItemList =
            new List<IStorageItem>(storageNodeItemList.Count + storageResourceItemList.Count);
        storageItemList.AddRange(storageNodeItemList);
        storageItemList.AddRange(storageResourceItemList);

        var compoundStorageDocument = new CompoundStorageDocument(storageItemList, referencedManager);
        return compoundStorageDocument;
    }

    /// <summary>
    /// 从复合文档里面转换为一个纯粹的存放文件信息的管理器，这个文件管理器只存放当前文档用到的文件信息
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public virtual async Task<IReadOnlyStorageFileManager> SerializeToStorageFileManagerAsync(CompoundStorageDocument document)
    {
        var cleanStorageFileManager = new CleanStorageFileManager(document.StorageFileManager);
        var storageNodeSerializer = StorageNodeSerializer;

        foreach (var storageItem in document.StorageItemList)
        {
            if (storageItem is StorageNodeItem storageNodeItem)
            {
                var fileInfo = cleanStorageFileManager.CreateFile(storageItem.RelativePath);

                await storageNodeSerializer.SerializeAsync(storageNodeItem.RootStorageNode, fileInfo);
            }
            else if (storageItem is StorageResourceItem storageResourceItem)
            {
                var fileInfo = document.StorageFileManager.GetFile(storageResourceItem.RelativePath);
                if (fileInfo is not null)
                {
                    cleanStorageFileManager.AddFile(fileInfo);
                }
            }
        }

        // 添加引用文件
        var referenceStorageNode = await CreateReferenceStorageNodeAsync(document.ReferencedManager);
        if (referenceStorageNode != null)
        {
            var fileInfo = cleanStorageFileManager.CreateFile(DefaultReferenceFileName);
            await storageNodeSerializer.SerializeAsync(referenceStorageNode, fileInfo);
        }

        return cleanStorageFileManager;
    }

    /// <summary>
    /// 从引用建立对应的存储节点
    /// </summary>
    /// <param name="referencedManager"></param>
    /// <returns></returns>
    protected abstract Task<StorageNode?> CreateReferenceStorageNodeAsync(IReferencedManager referencedManager);
}