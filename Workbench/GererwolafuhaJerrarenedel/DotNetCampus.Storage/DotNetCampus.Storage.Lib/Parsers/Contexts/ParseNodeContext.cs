namespace DotNetCampus.Storage.Parsers.Contexts;

public readonly record struct ParseNodeContext
{
    public StorageNodeParserManager ParserManager => DocumentManager.ParserManager;

    /// <summary>
    /// 转换过程中，可能会涉及到递归转换，或者资源引用处理等，这些就需要用到文档管理器
    /// </summary>
    public required CompoundStorageDocumentManager DocumentManager { get; init; }
}