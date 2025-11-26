using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;
using DotNetCampus.Storage.Documents.StorageModels;

namespace DotNetCampus.Storage.Documents.Converters;

/// <summary>
/// 模型和复合文档之间的转换器
/// </summary>
public interface IStorageModelToCompoundDocumentConverter
{
    StorageModel ToStorageModel(CompoundStorageDocument document);

    CompoundStorageDocument ToCompoundDocument(StorageModel model);
}