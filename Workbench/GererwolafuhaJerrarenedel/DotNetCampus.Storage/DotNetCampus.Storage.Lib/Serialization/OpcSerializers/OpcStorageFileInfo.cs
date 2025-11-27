using System.IO.Compression;
using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Serialization;

internal class OpcStorageFileInfo : IReadOnlyStorageFileInfo
{
    public OpcStorageFileInfo(ZipArchiveEntry zipArchiveEntry)
    {
        _zipArchiveEntry = zipArchiveEntry;
        RelativePath = zipArchiveEntry.FullName;
    }

    private readonly ZipArchiveEntry _zipArchiveEntry;

    public StorageFileRelativePath RelativePath { get; init; }

    public Stream OpenRead()
    {
        return _zipArchiveEntry.Open();
    }
}