using DotNetCampus.Storage.CompoundStorageDocumentManagers;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;
using DotNetCampus.Storage.Documents.StorageModels;
using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Documents.Converters;

internal class EmptyStorageModelToCompoundDocumentConverter : StorageModelToCompoundDocumentConverter
{
    public EmptyStorageModelToCompoundDocumentConverter(CompoundStorageDocumentManagerProvider provider) : base(provider)
    {
    }

    public override StorageModel ToStorageModel(CompoundStorageDocument document)
    {
        return new EmptyStorageModel();
    }

    public override CompoundStorageDocument ToCompoundDocument(StorageModel model)
    {
        return new CompoundStorageDocument(new List<IStorageItem>(), Manager.ReferencedManager);
    }
}