using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Storage.CompoundStorageDocumentManagers;
using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageModels;
using DotNetCampus.Storage.Parsers;
using DotNetCampus.Storage.SaveInfos;
using DotNetCampus.Storage.Serialization;
using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage;

/// <summary>
/// 复合文档管理器
/// </summary>
/// 这是这个程序集的入口
public class CompoundStorageDocumentManager
{
    // ~~也许带状态的，放在 CompoundStorageDocument 更佳，跟随文档内容走。缺点是没有方法可以重新更新~~

    /// <summary>
    /// 文件管理。带状态
    /// </summary>
    public required IStorageFileManager StorageFileManager { get; init; }

    /// <summary>
    /// 引用管理器。不同文档之间禁止复用。带状态
    /// </summary>
    public required IReferencedFileManager ReferencedFileManager { get; init; }

    /// <summary>
    /// 提供 <see cref="SaveInfo"/> 和 <see cref="StorageNode"/> 的转换管理器
    /// </summary>
    public StorageNodeParserManager ParserManager { get; init; } = new StorageNodeParserManager();

    /// <summary>
    /// 存储模型到复合文档的转换器
    /// </summary>
    /// StorageNode - StorageModel -> CompoundStorageDocument - -.xml 文件（或 .json 等格式）
    public required IStorageModelToCompoundDocumentConverter StorageModelToCompoundDocumentConverter { get; init; }

    /// <summary>
    /// 默认的存储节点序列化器
    /// </summary>
    /// SaveInfo -> StorageNode
    public required IStorageNodeSerializer DefaultStorageNodeSerializer { get; init; }

    public static CompoundStorageDocumentManagerBuilder CreateBuilder()
    {
        return new CompoundStorageDocumentManagerBuilder();
    }
}

public static class CompoundStorageDocumentManagerExtension
{
    public static Task<CompoundStorageDocument> ReadFromOpcFileAsync(this CompoundStorageDocumentManager manager, FileInfo opcFile)
    {
        var opcSerializer = new OpcSerializer(manager);
        return opcSerializer.ReadFromOpcFileAsync(opcFile);
    }

    public static async Task<T?> ReadStorageModelFromOpcFile<T>(this CompoundStorageDocumentManager manager, FileInfo opcFile)
        where T : StorageModel
    {
        var compoundStorageDocument = await manager.ReadFromOpcFileAsync(opcFile);
        var converter = manager.StorageModelToCompoundDocumentConverter;
        var storageModel = converter.ToStorageModel(compoundStorageDocument);

        return storageModel as T;
    }
}