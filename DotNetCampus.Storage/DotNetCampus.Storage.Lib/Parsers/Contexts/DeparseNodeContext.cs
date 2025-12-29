using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Parsers.Contexts;

public readonly record struct DeparseNodeContext()
{
    public string? NodeName { get; init; }
    public StorageNodeParserManager ParserManager => DocumentManager.ParserManager;

    /// <summary>
    /// 转换过程中，可能会涉及到递归转换，或者资源引用处理等，这些就需要用到文档管理器
    /// </summary>
    public required CompoundStorageDocumentManager DocumentManager { get; init; }

    internal List<PostAsyncNodeParserTaskInfo> PostNodeParserTaskList { get; } = [];
}

/// <summary>
/// 需要在节点解析后续处理中使用的信息
/// </summary>
/// <param name="Parser"></param>
/// <param name="Value"></param>
/// <param name="StorageNode"></param>
readonly record struct PostAsyncNodeParserTaskInfo(IAsyncPostNodeParser Parser, object Value, StorageNode StorageNode, bool IsDeparse);