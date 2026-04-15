using System.Runtime.Serialization;

namespace VirtualFileExplorer.Core;

/// <summary>
/// 表示一个虚拟的文件信息
/// </summary>
public class VirtualFileInfo : NotifyObject
{
    public VirtualFileInfo(string id, string name, VirtualFolderInfo ownerFolder)
    {
        _id = id;
        _name = name;
        OwnerFolder = ownerFolder;
    }

    private string _id;
    private string _name;

    [DataMember(Name = "id")]
    public string Id
    {
        get => _id;
        set
        {
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

    /// <summary>
    /// 用于表示在哪个文件夹下
    /// </summary>
    public VirtualFolderInfo OwnerFolder { get; }
}