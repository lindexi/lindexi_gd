using System.Runtime.Serialization;
using System.Text;

namespace VirtualFileExplorer;

/// <summary>
/// 虚拟的文件夹
/// </summary>
/// 比如当前所在的是 C:\Abc\Def 的路径，那 FolderLink 就是由 C:\ Abc\ Def\ 三个部分组成的
public class VirtualFolderInfo : NotifyObject
{
    public VirtualFolderInfo(string id, string name, IReadOnlyList<VirtualFolderInfo> currentLinkList)
    {
        _id = id;
        _name = name;
        CurrentLinkList = currentLinkList;
    }

    /// <summary>
    /// 文件夹链，从上到下。相对的路径下，最底层是当前文件夹的上一层。最顶层文件夹此属性为空列表
    /// 如果是正常的相对路径的最顶层，即相对于当前路径，则采用 `.` 表示 
    /// </summary>
    public IReadOnlyList<VirtualFolderInfo> CurrentLinkList { get; }

    private string _id;
    private string _name;

    [DataMember(Name = "id")]
    public string Id
    {
        get => _id;
        set
        {
            // id 可能是空，对于相对路径来说，这个 id 可能没有意义
            if (value == _id) return;
            _id = value;
            OnPropertyChanged();
        }
    }

    [DataMember(Name = "name")]
    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public override string ToString()
    {
        var result = new StringBuilder();

        //result.Append($"{Name} > ");

        for (var i = 0; i < CurrentLinkList.Count; i++)
        {
            var folder = CurrentLinkList[i];
            result.Append($"{folder.Name}\\");
        }

        result.Append(Name);
        result.Append(@"\");

        return result.ToString();
    }
}