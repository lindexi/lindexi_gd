namespace DotNetCampus.Storage.StorageFiles;

public interface IHashProvider
{
    ValueTask<HashResult> ComputeHashAsync(Stream inputStream);

    async ValueTask<HashResult> ComputeHashAsync(FileInfo file)
    {
        await using var fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        return await ComputeHashAsync(fileStream);
    }

    /// <summary>
    /// 获取文件哈希值
    /// </summary>
    /// <param name="localStorageFileInfo"></param>
    /// <returns></returns>
    async ValueTask<HashResult> GetFileHashAsync(LocalStorageFileInfo localStorageFileInfo)
    {
        var fileInfo = localStorageFileInfo.FileInfo;
        fileInfo.Refresh(); // 更新文件信息

        var lastWriteTime = fileInfo.LastWriteTime;

        if (localStorageFileInfo.HashCache is {} hashCache)
        {
            if (lastWriteTime == hashCache.LastWriteTime && fileInfo.Length == hashCache.FileSize)
            {
                // 最后写入时间和文件大小都没有变化，说明文件没有变化，可以直接使用缓存的哈希值
                // 尽管这不是完全安全，但是为了性能考虑
                return hashCache.HashResult;
            }
        }

        var hashResult = await ComputeHashAsync(fileInfo);
        localStorageFileInfo.HashCache = new HashCache
        {
            HashResult = hashResult,
            LastWriteTime = lastWriteTime,
            FileSize = fileInfo.Length
        };

        return hashResult;
    }
}