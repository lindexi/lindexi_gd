using System.IO;

namespace PptxGenerator.Rendering.Materials;

/// <summary>
/// 表示文件路径的轻量只读结构体，保留文件语义的同时避免 <see cref="FileInfo"/> 的对象分配。
/// 支持从 <see cref="FileInfo"/> 和 <see cref="string"/> 隐式转换。
/// </summary>
/// <param name="Path">文件的本地路径。</param>
public readonly record struct SlideMlFilePath(string Path)
{
    /// <summary>
    /// 从 <see cref="FileInfo"/> 隐式转换为 <see cref="SlideMlFilePath"/>。
    /// </summary>
    /// <param name="fileInfo">文件信息。</param>
    /// <returns>包含文件路径的 <see cref="SlideMlFilePath"/> 实例。</returns>
    public static implicit operator SlideMlFilePath(FileInfo fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);
        return new SlideMlFilePath(fileInfo.FullName);
    }

    /// <summary>
    /// 从文件路径字符串隐式转换为 <see cref="SlideMlFilePath"/>。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <returns>包含文件路径的 <see cref="SlideMlFilePath"/> 实例。</returns>
    public static implicit operator SlideMlFilePath(string path)
    {
        return new SlideMlFilePath(path);
    }

    /// <summary>
    /// 返回文件路径字符串。
    /// </summary>
    /// <returns>文件路径。</returns>
    public override string ToString() => Path;
}
