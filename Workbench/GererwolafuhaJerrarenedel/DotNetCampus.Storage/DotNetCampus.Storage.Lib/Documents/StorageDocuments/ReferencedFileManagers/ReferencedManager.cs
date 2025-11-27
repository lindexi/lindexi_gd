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

    private ReferenceInfoDictionary ReferenceInfoDictionary { get; } = new ReferenceInfoDictionary();

    public void Reset(IReadOnlyCollection<ReferenceInfo>? references = null)
    {
        ReferenceInfoDictionary.Clear();

        if (references != null)
        {
            ReferenceInfoDictionary.AddRange(references);
        }
    }

    public IReadOnlyList<ReferenceInfo> References => ReferenceInfoDictionary.References;

    public ReferenceInfo? GetReferenceInfo(StorageReferenceId referenceId)
    {
        return ReferenceInfoDictionary.GetReferenceInfo(referenceId);
    }

    public ReferenceInfo AddLocalFile(FileInfo localFile)
    {
        IReferencedManager referencedManager = this;

        LocalStorageFileInfo? existFile = StorageFileManager.FileList.OfType<LocalStorageFileInfo>()
            .FirstOrDefault(t => t.FileInfo.FullName == localFile.FullName);

        // 存在本地文件了，说不定已经存在引用了
        if (existFile is not null)
        {
            ReferenceInfo? existReferenceInfo = ReferenceInfoDictionary.GetReferenceInfo(existFile.RelativePath);
            if (existReferenceInfo is not null)
            {
                existReferenceInfo.Counter++;
                return existReferenceInfo;
            }
            else
            {
                // 没有引用信息，创建一个新的引用信息
                return AddReferenceFile(new StorageReferenceId(Guid.NewGuid().ToString("N")), existFile);
            }
        }

        // 相对路径不应该直接取文件名，因为可能会是两个不同的文件夹包含相同的文件名，导致冲突
        StorageFileRelativePath relativePath = Path.Join(referencedManager.ResourceFolderRelativePath.AsSpan(), localFile.Name);
        var localStorageFileInfo = new LocalStorageFileInfo()
        {
            FileInfo = localFile,
            RelativePath = relativePath
        };
        StorageFileManager.AddFile(localStorageFileInfo);

        var referenceId = new StorageReferenceId(Guid.NewGuid().ToString("N"));
        var referenceInfo = new ReferenceInfo()
        {
            FilePath = relativePath,
            ReferenceId = referenceId,
            Counter = 1
        };
        AddReference(referenceInfo);
        return referenceInfo;
    }

    public void AddReference(ReferenceInfo referenceInfo)
    {
        ReferenceInfoDictionary.Add(referenceInfo);
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
        if (ReferenceInfoDictionary.GetReferenceInfo(referenceId) is {} referenceInfo)
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
        if (ReferenceInfoDictionary.GetReferenceInfo(referenceId) is { } referenceInfo)
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
        foreach (var referenceInfo in ReferenceInfoDictionary.References)
        {
            if (referenceInfo.Counter <= 0)
            {
                toRemoveList.Add(referenceInfo);
            }
        }

        foreach (var referenceInfo in toRemoveList)
        {
            ReferenceInfoDictionary.Remove(referenceInfo);
            StorageFileManager.RemoveFile(referenceInfo.FilePath);
        }
    }
}