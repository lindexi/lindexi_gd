using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Parsers;
using DotNetCampus.Storage.SaveInfos;
using DotNetCampus.Storage.Serialization;
using DotNetCampus.Storage.Serialization.XmlSerialization;
using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.CompoundStorageDocumentManagers;

public class CompoundStorageDocumentManagerBuilder
{
    public CompoundStorageDocumentManagerProvider ManagerProvider { get; } =
        new CompoundStorageDocumentManagerProvider();

    /// <summary>
    /// 文件管理
    /// </summary>
    public IStorageFileManager? StorageFileManager { get; set; }

    /// <summary>
    /// 使用本地文件管理器
    /// </summary>
    /// <param name="workingDirectoryInfo">工作路径，为空将使用系统临时文件夹</param>
    /// <returns></returns>
    public CompoundStorageDocumentManagerBuilder UseLocalFileManager(DirectoryInfo? workingDirectoryInfo = null)
    {
        StorageFileManager = new LocalStorageFileManager(workingDirectoryInfo);
        return this;
    }

    /// <summary>
    /// 提供 <see cref="SaveInfo"/> 和 <see cref="StorageNode"/> 的转换管理器
    /// </summary>
    public StorageNodeParserManager? ParserManager { get; set; }

    public CompoundStorageDocumentManagerBuilder UseParserManager(Action<StorageNodeParserManager> configureAction)
    {
        _parserManagerConfigureAction = configureAction;
        return this;
    }

    private Action<StorageNodeParserManager>? _parserManagerConfigureAction;

    /// <summary>
    /// 引用管理器。不同文档之间禁止复用
    /// </summary>
    public IReferencedManager? ReferencedFileManager { get; set; }

    /// <summary>
    /// 存储模型到复合文档的转换器
    /// </summary>
    public IStorageModelToCompoundDocumentConverter? StorageModelToCompoundDocumentConverter { get; set; }

    ///// <summary>
    ///// 默认的存储节点序列化器
    ///// </summary>
    //public IStorageNodeSerializer? DefaultStorageNodeSerializer { get; set; }

    public ICompoundStorageDocumentSerializer? CompoundStorageDocumentSerializer { get; set; }

    public CompoundStorageDocumentManagerBuilder UseStorageModelToCompoundDocumentConverter(
        Func<CompoundStorageDocumentManagerProvider, IStorageModelToCompoundDocumentConverter> creator)
    {
        var converter = creator(ManagerProvider);
        StorageModelToCompoundDocumentConverter = converter;
        return this;
    }

    public CompoundStorageDocumentManager Build()
    {
        var storageFileManager = StorageFileManager
                                 ?? new LocalStorageFileManager();
        var parserManager = ParserManager
                            ?? new StorageNodeParserManager();

        _parserManagerConfigureAction?.Invoke(parserManager);

        var referencedFileManager = ReferencedFileManager
                                    ?? new ReferencedManager(storageFileManager);

        var storageModelToCompoundDocumentConverter = StorageModelToCompoundDocumentConverter
                                                      ?? new EmptyStorageModelToCompoundDocumentConverter(
                                                          ManagerProvider);

        var compoundStorageDocumentSerializer = CompoundStorageDocumentSerializer ?? throw new InvalidOperationException();

        var manager = new CompoundStorageDocumentManager()
        {
            ReferencedManager = referencedFileManager,
            StorageFileManager = storageFileManager,
            StorageModelToCompoundDocumentConverter = storageModelToCompoundDocumentConverter,
            ParserManager = parserManager,
            CompoundStorageDocumentSerializer = compoundStorageDocumentSerializer,
            //DefaultStorageNodeSerializer = defaultStorageNodeSerializer
        };
        ManagerProvider.Manager = manager;
        return manager;
    }
}