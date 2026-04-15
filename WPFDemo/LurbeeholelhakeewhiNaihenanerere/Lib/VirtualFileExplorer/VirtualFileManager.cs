using System.IO;

namespace VirtualFileExplorer;

public abstract class VirtualFileManager
{
    public VirtualFileManager()
    {
      
    }

    public abstract IReadOnlyList<VirtualFileInfo> GetFiles(VirtualFolderInfo folder);
    public abstract IReadOnlyList<VirtualFolderInfo> GetFolders(VirtualFolderInfo folder);
}