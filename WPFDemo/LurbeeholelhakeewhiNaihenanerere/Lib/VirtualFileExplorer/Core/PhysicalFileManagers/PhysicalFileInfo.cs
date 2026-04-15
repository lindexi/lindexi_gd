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
        FileInfo = fileInfo;
    }

    public FileInfo FileInfo { get; }
}