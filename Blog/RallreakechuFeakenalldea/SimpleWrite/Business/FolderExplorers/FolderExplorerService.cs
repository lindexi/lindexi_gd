using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleWrite.Business.FolderExplorers;

internal class FolderExplorerService
{
    public Task<FolderTreeEntry> BuildTreeAsync(DirectoryInfo rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        return Task.Run(() => BuildTree(rootDirectory));
    }

    private FolderTreeEntry BuildTree(DirectoryInfo directoryInfo)
    {
        var children = new List<FolderTreeEntry>();

        foreach (var directory in EnumerateDirectories(directoryInfo).OrderBy(temp => temp.Name, StringComparer.OrdinalIgnoreCase))
        {
            children.Add(BuildTree(directory));
        }

        foreach (var file in EnumerateFiles(directoryInfo).OrderBy(temp => temp.Name, StringComparer.OrdinalIgnoreCase))
        {
            children.Add(new FolderTreeEntry(file.Name, file.FullName, false, []));
        }

        return new FolderTreeEntry(directoryInfo.Name, directoryInfo.FullName, true, children);
    }

    private static IEnumerable<DirectoryInfo> EnumerateDirectories(DirectoryInfo directoryInfo)
    {
        try
        {
            return directoryInfo.EnumerateDirectories();
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directoryInfo)
    {
        try
        {
            return directoryInfo.EnumerateFiles();
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }
}

internal readonly record struct FolderTreeEntry(string Name, string FullPath, bool IsDirectory, IReadOnlyList<FolderTreeEntry> Children);
