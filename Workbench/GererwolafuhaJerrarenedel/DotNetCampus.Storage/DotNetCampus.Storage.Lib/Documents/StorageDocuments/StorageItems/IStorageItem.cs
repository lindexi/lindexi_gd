using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;

public interface IStorageItem
{
    StorageFileRelativePath RelativePath { get; }
}