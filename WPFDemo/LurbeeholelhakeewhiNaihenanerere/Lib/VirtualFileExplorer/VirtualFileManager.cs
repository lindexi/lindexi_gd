namespace VirtualFileExplorer;

public abstract class VirtualFileManager
{
    public abstract IReadOnlyList<VirtualFileInfo> GetFiles(VirtualFolderInfo folder);
    public abstract IReadOnlyList<VirtualFolderInfo> GetFolders(VirtualFolderInfo folder);

    // 获取相对路径等逻辑
}