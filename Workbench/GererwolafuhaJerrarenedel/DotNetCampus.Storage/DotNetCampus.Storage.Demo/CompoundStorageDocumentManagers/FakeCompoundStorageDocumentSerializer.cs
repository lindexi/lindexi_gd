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

    protected override void AddResourceReference(StorageNode referenceStorageNode, IReferencedManager referencedManager)
    {
        List<ReferenceInfo>? referenceInfoList = null;
        var storageReferenceSaveInfo = Manager.ParseToValue<StorageReferenceSaveInfo>(referenceStorageNode);
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

    protected override StorageNode ReferencedManagerToReferenceStorageNode(IReferencedManager referencedManager)
    {
        var references = referencedManager.References;
        var list = new List<StorageRelationshipsSaveInfo>(references.Count);
        foreach (var referenceInfo in references)
        {
            list.Add(new StorageRelationshipsSaveInfo()
            {
                Id = referenceInfo.ReferenceId.ReferenceId,
                Target = referenceInfo.FilePath.RelativePath,
            });
        }
        var storageReferenceSaveInfo = new StorageReferenceSaveInfo()
        {
            Relationships = list
        };

        var storageNode = Manager.DeparseToStorageNode(storageReferenceSaveInfo);
        return storageNode;
    }
}