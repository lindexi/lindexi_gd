using System.Runtime.Serialization;
using System.Text;

namespace VirtualFileExplorer;

/// <summary>
/// 虚拟的文件夹
/// </summary>
/// 比如当前所在的是 C:\Abc\Def 的路径，那 FolderLink 就是由 C:\ Abc\ Def\ 三个部分组成的
public class VirtualFolderInfo : NotifyObject
{
    public VirtualFolderInfo(string id, string name, VirtualFolderInfo? upperLevelFolder)
    {
        _id = id;
        _name = name;
        UpperLevelFolder = upperLevelFolder;
    }
    
    /// <summary>
    /// 上一级的文件夹，可能为空，表示顶层
    /// </summary>
    public VirtualFolderInfo? UpperLevelFolder { get; }

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
}