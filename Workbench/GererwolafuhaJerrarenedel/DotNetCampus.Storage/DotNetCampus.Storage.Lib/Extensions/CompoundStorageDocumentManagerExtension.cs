using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageModels;
using DotNetCampus.Storage.Serialization;

namespace DotNetCampus.Storage;

public static class CompoundStorageDocumentManagerExtension
{
    public static Task<CompoundStorageDocument> ReadFromOpcFileAsync(this CompoundStorageDocumentManager manager, FileInfo opcFile)
    {
        var opcSerializer = new OpcSerializer(manager);
        return opcSerializer.ReadFromOpcFileAsync(opcFile);
    }

    public static async Task<T?> ReadStorageModelFromOpcFile<T>(this CompoundStorageDocumentManager manager, FileInfo opcFile)
        where T : StorageModel
    {
        var compoundStorageDocument = await manager.ReadFromOpcFileAsync(opcFile);
        var converter = manager.StorageModelToCompoundDocumentConverter;
        var storageModel = converter.ToStorageModel(compoundStorageDocument);

        return storageModel as T;
    }

    public static Task SaveToOpcFileAsync(this CompoundStorageDocumentManager manager, CompoundStorageDocument document, FileInfo opcOutputFile)
    {
        var opcSerializer = new OpcSerializer(manager);
        return opcSerializer.SaveToOpcFileAsync(document, opcOutputFile);
    }

    public static Task SaveToOpcFileAsync(this CompoundStorageDocumentManager manager, StorageModel storageModel,
        FileInfo opcOutputFile)
    {
        var converter = manager.StorageModelToCompoundDocumentConverter;
        var compoundStorageDocument = converter.ToCompoundDocument(storageModel);
        return manager.SaveToOpcFileAsync(compoundStorageDocument, opcOutputFile);
    }
}