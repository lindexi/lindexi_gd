namespace DotNetCampus.Storage.StorageFiles;

/// <summary>
/// 表示存储文件的相对路径
/// </summary>
public readonly record struct StorageFileRelativePath
{
    public StorageFileRelativePath(string relativePath)
    {
        RelativePath = relativePath.Replace('\\', '/'); // 决定用 / 解决 Linux 路径拼接问题
    }

    public string RelativePath { get; }

    public static implicit operator StorageFileRelativePath(string relativePath)
    {
        return new StorageFileRelativePath(relativePath);
    }

    public ReadOnlySpan<char> AsSpan() => RelativePath.AsSpan();

    /// <summary>
    /// 判断另一个路径字符串是否与当前路径相等（忽略大小写）
    /// </summary>
    /// <param name="otherPath"></param>
    /// <returns></returns>
    public bool Equals(string? otherPath) => string.Equals(RelativePath, otherPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 判断另一个路径是否与当前路径相等（忽略大小写）
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(StorageFileRelativePath? other)
    {
        return Equals(other?.RelativePath);
    }
}

//public class StorageFileRelativePathEqualityComparer : IEqualityComparer<StorageFileRelativePath>
//{
//    public bool Equals(StorageFileRelativePath x, StorageFileRelativePath y)
//    {
//        return x.RelativePath == y.RelativePath;
//    }

//    public int GetHashCode(StorageFileRelativePath obj)
//    {
//        return obj.RelativePath.GetHashCode();
//    }
//}