namespace DotNetCampus.Storage.Documents.StorageDocuments;

/// <summary>
/// 引用资源的标识符
/// </summary>
/// <param name="ReferenceId"></param>
public readonly record struct StorageReferenceId(string ReferenceId)
{
    public static StorageReferenceId CreateNewReferenceId()
        // 理论上 Guid 不会重复，且这是在相同一台机器上创建的 Guid 内容
        => new StorageReferenceId(Guid.NewGuid().ToString("N"));
}