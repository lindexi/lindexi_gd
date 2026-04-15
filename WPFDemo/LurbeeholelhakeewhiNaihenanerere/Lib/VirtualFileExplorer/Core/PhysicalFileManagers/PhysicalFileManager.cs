using System.IO;

namespace VirtualFileExplorer.Core.PhysicalFileManagers;

/// <summary>
/// 实际的物理地址
/// </summary>
public class PhysicalFileManager : VirtualFileManager
{
    private readonly string _rootPath;
    private readonly VirtualFolderInfo _rootFolder;

    public PhysicalFileManager(string rootPath, string? rootFolderName = null)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("根目录不能为空。", nameof(rootPath));
        }

        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
        var rootDirectoryInfo = new DirectoryInfo(_rootPath);
        _rootFolder = CreateFolderInfo(rootDirectoryInfo, null, rootFolderName ?? rootDirectoryInfo.Name);
        _rootFolder.CanCopy = false;
        _rootFolder.CanDelete = false;
        _rootFolder.CanMove = false;
        _rootFolder.CanRename = false;
    }

    public override VirtualFolderInfo RootFolder => _rootFolder;

    public override IReadOnlyList<VirtualFileInfo> GetFiles(VirtualFolderInfo folder)
    {
        var path = GetPhysicalPath(folder);
        if (!Directory.Exists(path))
        {
            return Array.Empty<VirtualFileInfo>();
        }

        return Directory.EnumerateFiles(path)
            .Select(file => AttachEntry<VirtualFileInfo>(new PhysicalFileInfo(new FileInfo(file), folder)))
            .OrderBy(file => file.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public override IReadOnlyList<VirtualFolderInfo> GetFolders(VirtualFolderInfo folder)
    {
        var path = GetPhysicalPath(folder);
        if (!Directory.Exists(path))
        {
            return Array.Empty<VirtualFolderInfo>();
        }

        return Directory.EnumerateDirectories(path)
            .Select(directory => CreateFolderInfo(new DirectoryInfo(directory), folder))
            .OrderBy(child => child.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public override VirtualFolderInfo ResolveFolder(string folderPath, VirtualFolderInfo? currentFolder = null)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("目标文件夹路径不能为空。", nameof(folderPath));
        }

        var basePath = currentFolder is null ? _rootPath : GetPhysicalPath(currentFolder);
        var candidatePath = Path.IsPathRooted(folderPath)
            ? folderPath
            : Path.Combine(basePath, folderPath);
        var fullPath = ValidateInsideRoot(candidatePath);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"未找到文件夹：{folderPath}");
        }

        return CreateFolderHierarchy(fullPath);
    }

    public override void RenameEntry(VirtualFileSystemEntry entry, string newName)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateEntryName(newName);

        if (ReferenceEquals(entry, RootFolder))
        {
            throw new InvalidOperationException("根目录不支持重命名。");
        }

        var sourcePath = GetPhysicalPath(entry);
        var parentFolder = entry.OwnerFolder ?? throw new InvalidOperationException("当前项缺少父目录。");
        var destinationPath = ValidateInsideRoot(Path.Combine(GetPhysicalPath(parentFolder), newName));
        EnsureDestinationNotExists(destinationPath);

        if (entry is VirtualFolderInfo)
        {
            Directory.Move(sourcePath, destinationPath);
        }
        else
        {
            File.Move(sourcePath, destinationPath);
        }

        UpdateEntry(entry, destinationPath, parentFolder);
    }

    public override void DeleteEntry(VirtualFileSystemEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (ReferenceEquals(entry, RootFolder))
        {
            throw new InvalidOperationException("根目录不支持删除。");
        }

        var path = GetPhysicalPath(entry);
        if (entry is VirtualFolderInfo)
        {
            Directory.Delete(path, true);
        }
        else
        {
            File.Delete(path);
        }
    }

    public override VirtualFileSystemEntry CopyEntry(VirtualFileSystemEntry entry, VirtualFolderInfo targetFolder)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(targetFolder);

        var sourcePath = GetPhysicalPath(entry);
        var targetFolderPath = GetPhysicalPath(targetFolder);
        var destinationPath = GetAvailableDestinationPath(targetFolderPath, entry.Name, entry is VirtualFolderInfo);

        if (entry is VirtualFolderInfo)
        {
            CopyDirectory(sourcePath, destinationPath);
            return CreateFolderInfo(new DirectoryInfo(destinationPath), targetFolder);
        }

        File.Copy(sourcePath, destinationPath);
        return AttachEntry<VirtualFileSystemEntry>(new PhysicalFileInfo(new FileInfo(destinationPath), targetFolder));
    }

    public override VirtualFileSystemEntry MoveEntry(VirtualFileSystemEntry entry, VirtualFolderInfo targetFolder)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(targetFolder);

        if (ReferenceEquals(entry, RootFolder))
        {
            throw new InvalidOperationException("根目录不支持移动。");
        }

        var sourcePath = GetPhysicalPath(entry);
        var targetFolderPath = GetPhysicalPath(targetFolder);
        var destinationPath = ValidateInsideRoot(Path.Combine(targetFolderPath, entry.Name));

        if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            return entry;
        }

        EnsureDestinationNotExists(destinationPath);

        if (entry is VirtualFolderInfo)
        {
            Directory.Move(sourcePath, destinationPath);
        }
        else
        {
            File.Move(sourcePath, destinationPath);
        }

        UpdateEntry(entry, destinationPath, targetFolder);
        return entry;
    }

    public override VirtualFolderInfo CreateFolder(VirtualFolderInfo parentFolder, string folderName)
    {
        ArgumentNullException.ThrowIfNull(parentFolder);
        ValidateEntryName(folderName);

        var parentPath = GetPhysicalPath(parentFolder);
        var folderPath = ValidateInsideRoot(Path.Combine(parentPath, folderName));
        EnsureDestinationNotExists(folderPath);

        var directoryInfo = Directory.CreateDirectory(folderPath);
        return CreateFolderInfo(directoryInfo, parentFolder);
    }

    private VirtualFolderInfo CreateFolderInfo(DirectoryInfo directoryInfo, VirtualFolderInfo? parentFolder, string? displayName = null)
    {
        directoryInfo.Refresh();
        var folder = AttachEntry(new VirtualFolderInfo(directoryInfo.FullName, displayName ?? directoryInfo.Name, parentFolder));
        folder.CreatedTime = directoryInfo.Exists ? directoryInfo.CreationTime : null;
        folder.LastWriteTime = directoryInfo.Exists ? directoryInfo.LastWriteTime : null;
        folder.LastAccessTime = directoryInfo.Exists ? directoryInfo.LastAccessTime : null;
        return folder;
    }

    private VirtualFolderInfo CreateFolderHierarchy(string fullPath)
    {
        fullPath = ValidateInsideRoot(fullPath);
        if (string.Equals(fullPath, _rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return RootFolder;
        }

        var relativePath = Path.GetRelativePath(_rootPath, fullPath);
        var parts = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        var currentFolder = RootFolder;
        var currentPath = _rootPath;
        foreach (var part in parts)
        {
            currentPath = Path.Combine(currentPath, part);
            currentFolder = CreateFolderInfo(new DirectoryInfo(currentPath), currentFolder);
        }

        return currentFolder;
    }

    private string GetPhysicalPath(VirtualFileSystemEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return ValidateInsideRoot(entry.Id);
    }

    private string ValidateInsideRoot(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var relativePath = Path.GetRelativePath(_rootPath, fullPath);
        if (relativePath == ".."
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException($"路径超出根目录范围：{path}");
        }

        return fullPath;
    }

    private static void ValidateEntryName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("名称不能为空。", nameof(newName));
        }

        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException($"名称包含无效字符：{newName}", nameof(newName));
        }
    }

    private static void EnsureDestinationNotExists(string path)
    {
        if (File.Exists(path) || Directory.Exists(path))
        {
            throw new IOException($"目标已存在：{path}");
        }
    }

    private string GetAvailableDestinationPath(string targetFolderPath, string entryName, bool isFolder)
    {
        var candidatePath = ValidateInsideRoot(Path.Combine(targetFolderPath, entryName));
        if (!File.Exists(candidatePath) && !Directory.Exists(candidatePath))
        {
            return candidatePath;
        }

        var baseName = isFolder ? entryName : Path.GetFileNameWithoutExtension(entryName);
        var extension = isFolder ? string.Empty : Path.GetExtension(entryName);
        for (var index = 2; index < int.MaxValue; index++)
        {
            var candidateName = $"{baseName} - 副本{(index == 2 ? string.Empty : $" ({index})")}{extension}";
            candidatePath = ValidateInsideRoot(Path.Combine(targetFolderPath, candidateName));
            if (!File.Exists(candidatePath) && !Directory.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        throw new IOException("无法生成可用的目标名称。");
    }

    private static void CopyDirectory(string sourcePath, string destinationPath)
    {
        var sourceDirectory = new DirectoryInfo(sourcePath);
        if (!sourceDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"未找到源目录：{sourcePath}");
        }

        Directory.CreateDirectory(destinationPath);

        foreach (var file in sourceDirectory.EnumerateFiles())
        {
            file.CopyTo(Path.Combine(destinationPath, file.Name));
        }

        foreach (var directory in sourceDirectory.EnumerateDirectories())
        {
            CopyDirectory(directory.FullName, Path.Combine(destinationPath, directory.Name));
        }
    }

    private static void UpdateEntry(VirtualFileSystemEntry entry, string fullPath, VirtualFolderInfo parentFolder)
    {
        entry.Id = fullPath;
        entry.Name = Path.GetFileName(fullPath);
        entry.OwnerFolder = parentFolder;

        if (entry is PhysicalFileInfo physicalFileInfo)
        {
            physicalFileInfo.UpdateFileInfo(new FileInfo(fullPath));
        }
        else if (entry is VirtualFileInfo virtualFileInfo)
        {
            virtualFileInfo.Extension = Path.GetExtension(fullPath);
        }

        if (entry is VirtualFolderInfo folderInfo)
        {
            var directoryInfo = new DirectoryInfo(fullPath);
            directoryInfo.Refresh();
            folderInfo.CreatedTime = directoryInfo.Exists ? directoryInfo.CreationTime : null;
            folderInfo.LastWriteTime = directoryInfo.Exists ? directoryInfo.LastWriteTime : null;
            folderInfo.LastAccessTime = directoryInfo.Exists ? directoryInfo.LastAccessTime : null;
    }
    }
}