using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments;

public interface IReferencedManager
{
    /// <summary>
    /// 关联当前引用文件的文件管理器
    /// </summary>
    IStorageFileManager StorageFileManager { get; }

    void Rest(IReadOnlyCollection<ReferenceInfo>? references = null);

    IReadOnlyCollection<ReferenceInfo> References { get; }

    ReferenceInfo? GetReferenceInfo(StorageReferenceId referenceId);

    void AddReference(ReferenceInfo referenceInfo);

    ReferenceInfo AddReferenceFile(StorageReferenceId referenceId, IReadOnlyStorageFileInfo fileInfo);

    /// <summary>
    /// 加上引用计数
    /// </summary>
    /// <param name="referenceId"></param>
    void AddReferenceCount(StorageReferenceId referenceId);

    /// <summary>
    /// 减去引用计数
    /// </summary>
    /// <param name="referenceId"></param>
    void SubtractReferenceCount(StorageReferenceId referenceId);

    /// <summary>
    /// 将引用计数为 0 的资源进行清理
    /// </summary>
    void PruneReference();
}


/// <summary>
/// 引用资源的标识符
/// </summary>
/// <param name="ReferenceId"></param>
public readonly record struct StorageReferenceId(string ReferenceId);

public class ReferenceInfo
{
    public required StorageReferenceId ReferenceId { get; init; }

    public required StorageFileRelativePath FilePath { get; init; }

    /// <summary>
    /// 被引用次数
    /// </summary>
    public int Counter { get; internal set; }
}

public class StorageReferenceNotFoundException : Exception
{
    public StorageReferenceNotFoundException(StorageReferenceId referenceId, IReferencedManager referencedManager)
    : base($"Storage reference not found: {referenceId}")
    {
        ReferencedManager = referencedManager;
    }

    public IReferencedManager ReferencedManager { get; }
}

public class ReferencedManager : IReferencedManager
{
    public ReferencedManager(IStorageFileManager storageFileManager)
    {
        StorageFileManager = storageFileManager;
    }

    public IStorageFileManager StorageFileManager { get; }

    private Dictionary<StorageReferenceId, ReferenceInfo> ReferenceInfoDictionary { get; } = [];

    public void Rest(IReadOnlyCollection<ReferenceInfo>? references = null)
    {
        ReferenceInfoDictionary.Clear();

        if (references != null)
        {
            foreach (var referenceInfo in references)
            {
                ReferenceInfoDictionary.Add(referenceInfo.ReferenceId, referenceInfo);
            }
        }
    }

    public IReadOnlyCollection<ReferenceInfo> References => ReferenceInfoDictionary.Values;

    public ReferenceInfo? GetReferenceInfo(StorageReferenceId referenceId)
    {
        return ReferenceInfoDictionary.GetValueOrDefault(referenceId);
    }

    public void AddReference(ReferenceInfo referenceInfo)
    {
        ReferenceInfoDictionary[referenceInfo.ReferenceId] = referenceInfo;
    }

    public ReferenceInfo AddReferenceFile(StorageReferenceId referenceId, IReadOnlyStorageFileInfo fileInfo)
    {
        var referenceInfo = new ReferenceInfo()
        {
            Counter = 1,
            ReferenceId = referenceId,
            FilePath = fileInfo.RelativePath
        };

        StorageFileManager.AddFile(fileInfo);
        AddReference(referenceInfo);
        return referenceInfo;
    }

    public void AddReferenceCount(StorageReferenceId referenceId)
    {
        if (ReferenceInfoDictionary.TryGetValue(referenceId, out var referenceInfo))
        {
            referenceInfo.Counter++;
        }
        else
        {
            throw new StorageReferenceNotFoundException(referenceId, this);
        }
    }

    public void SubtractReferenceCount(StorageReferenceId referenceId)
    {
        if (ReferenceInfoDictionary.TryGetValue(referenceId, out var referenceInfo))
        {
            referenceInfo.Counter--;
        }
        else
        {
            throw new StorageReferenceNotFoundException(referenceId, this);
        }
    }

    public void PruneReference()
    {
        var toRemoveList = new List<ReferenceInfo>();
        foreach (var referenceInfo in ReferenceInfoDictionary.Values)
        {
            if (referenceInfo.Counter <= 0)
            {
                toRemoveList.Add(referenceInfo);
            }
        }

        foreach (var referenceInfo in toRemoveList)
        {
            ReferenceInfoDictionary.Remove(referenceInfo.ReferenceId);
            StorageFileManager.RemoveFile(referenceInfo.FilePath);
        }
    }
}