using DotNetCampus.Storage.Documents.StorageDocuments;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;
using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Serialization;

/// <summary>
/// 使用 Open Packaging Conventions(OPC) 格式的序列化器
/// </summary>
/// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/opc/open-packaging-conventions-overview
/// > The ECMA-376 OpenXML, 1st Edition, Part 2: Open Packaging Conventions (OPC) can be more easily understood through an analogy with real world filing systems. 
public class OpcSerializer : CompoundStorageDocumentSerializer
{
    public OpcSerializer(CompoundStorageDocumentManager manager) : base(manager)
    {
    }

    public async Task<CompoundStorageDocument> ReadFromOpcFileAsync(FileInfo opcFile)
    {
        await using var fileStream = opcFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);

        var fileList = new List<IReadOnlyStorageFileInfo>(zipArchive.Entries.Count);

        foreach (ZipArchiveEntry zipArchiveEntry in zipArchive.Entries)
        {
            var opcStorageFileInfo = new OpcStorageFileInfo(zipArchiveEntry);
            fileList.Add(opcStorageFileInfo);
        }

        var fileProvider = new SerializationStorageFileProvider(fileList);

        var compoundStorageDocument = await ToCompoundStorageDocument(fileProvider);
        return compoundStorageDocument;
    }
}

public class CompoundStorageDocumentSerializer
{
    public CompoundStorageDocumentSerializer(CompoundStorageDocumentManager manager)
    {
        Manager = manager;
    }

    public CompoundStorageDocumentManager Manager { get; }

    protected async Task<CompoundStorageDocument> ToCompoundStorageDocument(
        SerializationStorageFileProvider fileProvider)
    {
        var fileFilter = new OpcSerializationFileFilter();
        var classificationResult = fileFilter.Filter(fileProvider);

        var compoundStorageDocument = await ToCompoundStorageDocument(classificationResult);
        return compoundStorageDocument;
    }

    /// <summary>
    /// 从过滤之后的结果转换为复合存储文档
    /// </summary>
    /// <param name="classificationResult"></param>
    /// <returns></returns>
    protected async Task<CompoundStorageDocument> ToCompoundStorageDocument(OpcSerializationFileClassificationResult classificationResult)
    {
        var storageNodeSerializer = Manager.DefaultStorageNodeSerializer;

        foreach (var fileInfo in classificationResult.ReferenceResourceManagerFiles)
        {
            var storageNode = await storageNodeSerializer.DeserializeAsync(fileInfo);

            // 在 IReferencedFileManager 执行更新
        }

        // 可以将 StorageFileItem 改名为 StorageNodeItem
        var storageFileItemList = new List<StorageFileItem>();

        foreach (var fileInfo in classificationResult.DocumentFiles)
        {
            var storageNode = await storageNodeSerializer.DeserializeAsync(fileInfo);

            storageFileItemList.Add(new StorageFileItem()
            {
                RootStorageNode = storageNode,
                RelativePath = fileInfo.RelativePath.RelativePath
            });
        }

        var storageResourceItemList = new List<StorageResourceItem>();
        var referencedFileManager = Manager.ReferencedFileManager;

        foreach (var fileInfo in classificationResult.ResourceFiles)
        {
            // 考虑将资源存起来
            storageResourceItemList.Add(new StorageResourceItem()
            {
                RelativePath = fileInfo.RelativePath.RelativePath,
                ResourceId = fileInfo.RelativePath.RelativePath // 先用路径作为 ResourceId，后续可以改进
            });
        }

        // 似乎 CompoundStorageDocument 有些废，没有什么用处，毕竟存放的东西本身也在污染 CompoundStorageDocumentManager 管理器
        var storageItemList =
            new List<IStorageItem>(storageFileItemList.Count + storageResourceItemList.Count);
        storageItemList.AddRange(storageFileItemList);
        storageItemList.AddRange(storageResourceItemList);

        var compoundStorageDocument = new CompoundStorageDocument(storageItemList, Manager);
        return compoundStorageDocument;
    }
}

internal class OpcStorageFileInfo : IReadOnlyStorageFileInfo
{
    public OpcStorageFileInfo(ZipArchiveEntry zipArchiveEntry)
    {
        _zipArchiveEntry = zipArchiveEntry;
        RelativePath = zipArchiveEntry.FullName;
    }

    private readonly ZipArchiveEntry _zipArchiveEntry;

    public StorageFileRelativePath RelativePath { get; init; }

    public Stream OpenRead()
    {
        return _zipArchiveEntry.Open();
    }
}

/// <summary>
/// 存放序列化之后的文件信息
/// </summary>
/// <param name="FileList"></param>
public record SerializationStorageFileProvider(IReadOnlyCollection<IReadOnlyStorageFileInfo> FileList) : IReadOnlyStorageFileManager
{
    // 设计上应该是从 IStorageFileManager 源到复合存储文档之间的转换
    // 这个过程需要关注的是哪个文件到哪个文件之间的映射关系
    // 在 CompoundStorageDocumentManager 将一批 XML 相关的转换器放在一起。同时开放用户的配置逻辑
    // 如此可以搞定从 CompoundStorageDocumentManager 转换序列化的本地文件到存储文档的过程
    // 以及从存储文档转换到内存模型的过程。存储文档本身需要注入文档的文件到对应哪个存储模型的关系
}

/// <summary>
/// 文件过滤器，用来鉴别文件类型
/// </summary>
public interface ISerializationFileFilter
{
    OpcSerializationFileClassificationResult Filter(SerializationStorageFileProvider fileProvider);
}

public class OpcSerializationFileFilter : ISerializationFileFilter
{
    public const string DefaultReferenceFileName = "Reference.xml";
    public const string ContentTypesFileName = "[Content_Types].xml";

    public OpcSerializationFileClassificationResult Filter(SerializationStorageFileProvider fileProvider)
    {
        var fileList = fileProvider.FileList;

        var referenceFile = fileList.FirstOrDefault(t => string.Equals(t.RelativePath.RelativePath, "Reference.xml", StringComparison.OrdinalIgnoreCase));

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
            if (IsDocumentFile(fileInfo))
            {
                documentFile.Add(fileInfo);
            }
            else
            {
                resourceFile.Add(fileInfo);
            }
        }

        return new OpcSerializationFileClassificationResult()
        {
            ResourceFiles = resourceFile,
            DocumentFiles = documentFile,
            ReferenceResourceManagerFiles = referenceResourceManagerFiles,
            FileProvider = fileProvider,
        };

        static bool IsDocumentFile(IReadOnlyStorageFileInfo fileInfo)
        {
            // 后缀是 xml 的，但是不是 Reference.xml 和 [Content_Types].xml 文件的，就是文档文件
            var relativePath = fileInfo.RelativePath;
            var extension = Path.GetExtension(relativePath.AsSpan());
            if (!extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(relativePath.RelativePath, DefaultReferenceFileName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(relativePath.RelativePath, ContentTypesFileName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}

/// <summary>
/// 文件分类的结果
/// </summary>
public record OpcSerializationFileClassificationResult
{
    /// <summary>
    /// 引用资源记录的文件列表
    /// </summary>
    public required IReadOnlyList<IReadOnlyStorageFileInfo> ReferenceResourceManagerFiles { get; init; }

    /// <summary>
    /// 文档文件的列表
    /// </summary>
    public required IReadOnlyList<IReadOnlyStorageFileInfo> DocumentFiles { get; init; }

    /// <summary>
    /// 资源文件的列表
    /// </summary>
    public required IReadOnlyList<IReadOnlyStorageFileInfo> ResourceFiles { get; init; }

    public required SerializationStorageFileProvider FileProvider { get; init; }
}