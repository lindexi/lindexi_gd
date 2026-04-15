using System.Runtime.Serialization;

namespace VirtualFileExplorer.Core;

/// <summary>
/// 虚拟的文件夹
/// </summary>
/// 比如当前所在的是 C:\Abc\Def 的路径，那 FolderLink 就是由 C:\ Abc\ Def\ 三个部分组成的
public class VirtualFolderInfo : VirtualFileSystemEntry
{
    public VirtualFolderInfo(string id, string name, VirtualFolderInfo? upperLevelFolder)
        : base(id, name, upperLevelFolder)
    {
    }

    /// <summary>
    /// 上一级的文件夹，可能为空，表示顶层
    /// </summary>
    public VirtualFolderInfo? UpperLevelFolder => OwnerFolder;

    public override VirtualFileSystemEntryType EntryType => VirtualFileSystemEntryType.Folder;

    public override string DisplayType => "文件夹";

    public IReadOnlyList<RelativeFolderLink> GetBreadcrumbs()
    {
        var stack = new Stack<VirtualFolderInfo>();
        for (VirtualFolderInfo? folder = this; folder is not null; folder = folder.UpperLevelFolder)
        {
            stack.Push(folder);
        }

        var result = new List<RelativeFolderLink>(stack.Count);
        while (stack.Count > 0)
        {
            var folder = stack.Pop();
            result.Add(new RelativeFolderLink(folder.Name, folder, stack.Count == 0));
        }

        return result;
    }
}