namespace DotNetCampus.Storage.StorageFiles;

public interface IHashProvider
{
    ValueTask<HashResult> ComputeHashAsync(FileInfo file);
}