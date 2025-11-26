using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments;

public class ReferenceInfo
{
    public required StorageReferenceId ReferenceId { get; init; }

    public required StorageFileRelativePath FilePath { get; init; }

    /// <summary>
    /// 被引用次数
    /// </summary>
    public int Counter { get; internal set; }
}