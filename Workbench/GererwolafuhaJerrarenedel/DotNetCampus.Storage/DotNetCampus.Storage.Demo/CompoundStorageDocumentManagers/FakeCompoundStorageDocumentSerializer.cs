using DotNetCampus.Storage.Demo.SaveInfos;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Serialization;
using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Demo.CompoundStorageDocumentManagers;

public class FakeCompoundStorageDocumentSerializer : CompoundStorageDocumentSerializer
{
    public FakeCompoundStorageDocumentSerializer(CompoundStorageDocumentManager manager) : base(manager)
    {
    }

    protected override async Task AddResourceReferenceAsync(StorageNode referenceStorageNode, IReferencedManager referencedManager)
    {
        List<ReferenceInfo>? referenceInfoList = null;
        var storageReferenceSaveInfo = await Manager.ParseToValueAsync<StorageReferenceSaveInfo>(referenceStorageNode);
        if (storageReferenceSaveInfo.Relationships is { } list)
        {
            referenceInfoList = new List<ReferenceInfo>(list.Count);

            foreach (StorageRelationshipsSaveInfo relationshipsSaveInfo in list)
            {
                var id = relationshipsSaveInfo.Id;
                var target = relationshipsSaveInfo.Target;

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(target))
                {
                    continue;
                }

                referenceInfoList.Add(new ReferenceInfo()
                {
                    ReferenceId = new StorageReferenceId(id),
                    FilePath = new StorageFileRelativePath(target)
                });
            }
        }

        referencedManager.Reset(referenceInfoList);
    }

    protected override async Task<StorageNode?> CreateReferenceStorageNodeAsync(IReferencedManager referencedManager)
    {
        var references = referencedManager.References;
        var list = new List<StorageRelationshipsSaveInfo>(references.Count);
        foreach (var referenceInfo in references)
        {
            string? hash = null;
            var storageFileInfo = referencedManager.StorageFileManager.GetFile(referenceInfo.FilePath);
            if (storageFileInfo is LocalStorageFileInfo localStorageFileInfo)
            {
                var hashResult = await Manager.HashProvider.GetFileHashAsync(localStorageFileInfo);
                hash = hashResult.HashValue;
            }
            else
            {
                if (storageFileInfo != null)
                {
                    await using var readStream = storageFileInfo.OpenRead();
                    var hashResult = await Manager.HashProvider.ComputeHashAsync(readStream);
                    hash = hashResult.HashValue;
                }
            }

            list.Add(new StorageRelationshipsSaveInfo()
            {
                Id = referenceInfo.ReferenceId.ReferenceId,
                Target = referenceInfo.FilePath.RelativePath,
                Hash = hash,
            });
        }
        var storageReferenceSaveInfo = new StorageReferenceSaveInfo()
        {
            Relationships = list
        };

        var storageNode = await Manager.DeparseToStorageNodeAsync(storageReferenceSaveInfo);
        return storageNode;
    }
}