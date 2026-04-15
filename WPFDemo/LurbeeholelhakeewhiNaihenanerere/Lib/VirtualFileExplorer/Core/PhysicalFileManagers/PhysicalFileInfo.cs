using System.IO;

using VirtualFileExplorer.Core;

namespace VirtualFileExplorer.Core.PhysicalFileManagers;

/// <summary>
/// 物理的文件信息
/// </summary>
public class PhysicalFileInfo : VirtualFileInfo
{
    public PhysicalFileInfo(FileInfo fileInfo, VirtualFolderInfo ownerFolder) : base(fileInfo.FullName, fileInfo.Name, ownerFolder)
    {
        UpdateFileInfo(fileInfo);
    }

    public FileInfo FileInfo { get; private set; }

    internal void UpdateFileInfo(FileInfo fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        fileInfo.Refresh();
        FileInfo = fileInfo;
        Id = fileInfo.FullName;
        Name = fileInfo.Name;
        Extension = fileInfo.Extension;
        Length = fileInfo.Exists ? fileInfo.Length : null;
        CreatedTime = fileInfo.Exists ? fileInfo.CreationTime : null;
        LastWriteTime = fileInfo.Exists ? fileInfo.LastWriteTime : null;
        LastAccessTime = fileInfo.Exists ? fileInfo.LastAccessTime : null;
    }
}