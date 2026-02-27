using System.IO;

namespace SimpleWrite.Foundation.Primitive;

/// <summary>
/// 表示一个文件路径
/// </summary>
/// <param name="Path"></param>
/// 为什么不直接用 FileInfo 呢？因为构建 FileInfo 需要消耗太多资源
public readonly record struct FilePath(string Path)
{
    public static implicit operator FilePath(FileInfo fileInfo) =>
        new FilePath(fileInfo.FullName);
    public static implicit operator FileInfo(FilePath filePath) => 
        new FileInfo(filePath.Path);

    public static implicit operator string(FilePath filePath) => filePath.Path;
}