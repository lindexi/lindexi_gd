using System.Text;

namespace VirtualFileExplorer;

/// <summary>
/// 文件夹链接
/// </summary>
/// 比如当前所在的是 C:\Abc\Def 的路径，那 RelativeFolderLink 就是由 C:\ Abc\ Def\ 三个部分组成的
public class RelativeFolderLink //: IEquatable<RelativeFolderLink>
{
    public RelativeFolderLink(VirtualFolderInfo currentFolder, IReadOnlyList<RelativeFolderLink> currentLinkList)
    {
        CurrentFolder = currentFolder;
        CurrentLinkList = currentLinkList;
    }

    public string Name => CurrentFolder.Name;

    /// <summary>
    /// 当前的文件夹，表示在这个 <see cref="RelativeFolderLink"/> 里面最里层的文件夹
    /// </summary>
    public VirtualFolderInfo CurrentFolder { get; }

    /// <summary>
    /// 文件夹链，从上到下。相对的路径下，最底层是当前文件夹的上一层。最顶层文件夹此属性为空列表
    /// 如果是正常的相对路径的最顶层，即相对于当前路径，则采用 `.` 表示 
    /// </summary>
    public IReadOnlyList<RelativeFolderLink> CurrentLinkList { get; }

    public override string ToString()
    {
        var result = new StringBuilder();

        result.Append($"{Name} > ");

        for (var i = 0; i < CurrentLinkList.Count; i++)
        {
            var folder = CurrentLinkList[i];
            result.Append($"{folder.Name}\\");
        }

        result.Append(Name);

        return result.ToString();
    }
}