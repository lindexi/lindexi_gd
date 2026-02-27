using System.IO;

namespace SimpleWrite.Foundation.Primitive;

/// <summary>
/// 表示一个文件夹路径
/// </summary>
/// <param name="Path"></param>
/// 为什么不直接用 DirectoryInfo 呢？因为构建 DirectoryInfo 需要消耗太多资源
public readonly record struct DirectoryPath(string Path)
{
    public static implicit operator DirectoryPath(DirectoryInfo directoryInfo) =>
        new DirectoryPath(directoryInfo.FullName);

    public static implicit operator DirectoryInfo(DirectoryPath directoryPath) => 
        new DirectoryInfo(directoryPath.Path);

    public static implicit operator string(DirectoryPath directoryPath) => directoryPath.Path;
}