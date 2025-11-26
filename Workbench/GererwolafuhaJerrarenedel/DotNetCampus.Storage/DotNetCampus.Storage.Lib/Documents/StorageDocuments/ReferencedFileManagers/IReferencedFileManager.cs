using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments;

public interface IReferencedFileManager
{
    /// <summary>
    /// 关联当前引用文件的文件管理器
    /// </summary>
    IStorageFileManager StorageFileManager { get; }


}

internal class EmptyReferencedFileManager : IReferencedFileManager
{
    public IStorageFileManager StorageFileManager { get; set; }
}