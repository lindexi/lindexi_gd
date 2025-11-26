using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Serialization;

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

    public required IReadOnlyStorageFileManager FileProvider { get; init; }
}