using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
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
/// 由于有部分属性必须业务注入具体实现，因此设计为抽象类
public abstract class CompoundStorageDocumentManager
{
    // ~~也许带状态的，放在 CompoundStorageDocument 更佳，跟随文档内容走。缺点是没有方法可以重新更新~~

    /// <summary>
    /// 文件管理。带状态
    /// </summary>
    public virtual IStorageFileManager StorageFileManager => _storageFileManager ??= new LocalStorageFileManager();
    private IStorageFileManager? _storageFileManager;

    /// <summary>
    /// 引用管理器。不同文档之间禁止复用。带状态
    /// </summary>
    public virtual IReferencedManager ReferencedManager => _referencedManager ??= new ReferencedManager(StorageFileManager);

    private IReferencedManager? _referencedManager;

    /// <summary>
    /// 提供 <see cref="SaveInfo"/> 和 <see cref="StorageNode"/> 的转换管理器
    /// </summary>
    /// <remarks>
    /// 转换器比较通用，建议全局复用一个实例即可。转换器必须经过业务初始化，才能将业务的存储源代码生成器的内容注入。请使用源代码生成器生成的 StorageNodeParserManagerCollection.RegisterSaveInfoNodeParser 代码进行注册
    /// </remarks>
    public virtual StorageNodeParserManager ParserManager => _parserManager ??= new StorageNodeParserManager();
    private StorageNodeParserManager? _parserManager;

    /// <summary>
    /// 存储模型到复合文档的转换器。必须业务注入具体实现
    /// </summary>
    /// StorageModel (SaveInfo们) -> CompoundStorageDocument (StorageNode们 + 资源)
    public abstract IStorageModelToCompoundDocumentConverter StorageModelToCompoundDocumentConverter { get; }

    /// <summary>
    /// 复合存储文档和松散文件之间进行转换。必须业务注入具体实现
    /// CompoundStorageDocument (内存里) -> 松散文件夹（.xml 磁盘文件）
    /// </summary>
    public abstract ICompoundStorageDocumentSerializer CompoundStorageDocumentSerializer { get; }
}