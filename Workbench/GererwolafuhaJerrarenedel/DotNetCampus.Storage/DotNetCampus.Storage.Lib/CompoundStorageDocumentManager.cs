using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Parsers;
using DotNetCampus.Storage.SaveInfos;
using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage;

/// <summary>
/// 复合文档管理器
/// </summary>
/// 这是这个程序集的入口
public class CompoundStorageDocumentManager
{
    ///// <summary>
    ///// 工作路径。每个文档实例必须使用不同的工作路径
    ///// </summary>
    //public required DirectoryInfo WorkingDirectoryInfo { get; init; }

    /// <summary>
    /// 文件管理
    /// </summary>
    public required IStorageFileManager StorageFileManager { get; init; }

    /// <summary>
    /// 提供 <see cref="SaveInfo"/> 和 <see cref="StorageNode"/> 的转换管理器
    /// </summary>
    public StorageNodeParserManager ParserManager { get; init; } = new StorageNodeParserManager();

    /// <summary>
    /// 引用管理器。不同文档之间禁止复用
    /// </summary>
    public required IReferencedFileManager ReferencedFileManager { get; init; }

    /// <summary>
    /// 存储模型到复合文档的转换器
    /// </summary>
    public required IStorageModelToCompoundDocumentConverter StorageModelToCompoundDocumentConverter { get; init; }
}


public class CompoundStorageDocumentManagerBuilder
{

}

public class CompoundStorageDocumentManagerProvider
{
    internal CompoundStorageDocumentManager? Manager { get; set; }

    public CompoundStorageDocumentManager GetManager()
    {
        if (Manager == null)
        {
            throw new InvalidOperationException($"Manager is null");
        }

        return Manager;
    }
}