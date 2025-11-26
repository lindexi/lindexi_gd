using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments;

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