using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments;

/// <summary>
/// 引用资源管理器
/// </summary>
public class ReferencedManager : IReferencedManager
{
    public ReferencedManager(IStorageFileManager storageFileManager)
    {
        StorageFileManager = storageFileManager;
    }

    public IStorageFileManager StorageFileManager { get; }

    private Dictionary<StorageReferenceId, ReferenceInfo> ReferenceInfoDictionary { get; } = [];

    public void Reset(IReadOnlyCollection<ReferenceInfo>? references = null)
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

    public ReferenceInfo AddLocalFile(FileInfo localFile)
    {
        IReferencedManager referencedManager = this;
        StorageFileRelativePath relativePath = Path.Join(referencedManager.ResourceFolderRelativePath.AsSpan(), localFile.Name);

        var localStorageFileInfo = new LocalStorageFileInfo()
        {
            FileInfo = localFile,
            RelativePath = relativePath
        };
        // 添加本地文件时，不用考虑重复的问题，因为刚好每次相同的文件都能覆盖。但是每次生成新的 Id 倒是有点亏。至少文件不会重复，只是 Id 内容重复而已
        StorageFileManager.AddFile(localStorageFileInfo);

        var referenceId = new StorageReferenceId(Guid.NewGuid().ToString("N"));
        var referenceInfo = new ReferenceInfo()
        {
            FilePath = relativePath,
            ReferenceId = referenceId,
            Counter = 1
        };
        return referenceInfo;
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