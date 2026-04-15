namespace VirtualFileExplorer.Core;

/// <summary>
/// 表示相对路径的文件夹链接
/// </summary>
/// 比如当前所在的是 C:\Abc\Def 的路径，那 RelativeFolderLink 就是由 C:\ Abc\ Def\ 三个部分组成的
public class RelativeFolderLink
{
    public RelativeFolderLink(string name, VirtualFolderInfo folder, bool isCurrent)
    {
        Name = name;
        Folder = folder;
        IsCurrent = isCurrent;
    }

    public string Name { get; }

    public VirtualFolderInfo Folder { get; }

    public bool IsCurrent { get; }

    public override string ToString()
    {
        return Name;
    }
}