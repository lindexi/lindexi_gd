using System.IO;

using VirtualFileExplorer.Core;

namespace VirtualFileExplorer.Core.PhysicalFileManagers;

/// <summary>
/// 实际的物理地址
/// </summary>
public class PhysicalFileManager : VirtualFileManager
{
    public override IReadOnlyList<VirtualFileInfo> GetFiles(VirtualFolderInfo folder)
    {
        var result = new List<VirtualFileInfo>();
        var path = GetPhysicalPath(folder);
        if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                result.Add(new VirtualFileInfo(file, fileInfo.Name, folder));
            }
        }
        return result;
    }
    public override IReadOnlyList<VirtualFolderInfo> GetFolders(VirtualFolderInfo folder)
    {
        var result = new List<VirtualFolderInfo>();
        var path = GetPhysicalPath(folder);
        if (Directory.Exists(path))
        {
            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                var directoryInfo = new DirectoryInfo(directory);
                result.Add(new VirtualFolderInfo(directory, directoryInfo.Name, folder));
            }
        }
        return result;
    }
    private string GetPhysicalPath(VirtualFolderInfo folder)
    {
        // 这里可以根据实际情况进行路径转换
        return folder.Id;
    }
}