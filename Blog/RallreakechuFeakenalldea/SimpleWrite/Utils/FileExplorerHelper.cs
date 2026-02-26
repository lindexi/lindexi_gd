using System;
using System.Diagnostics;
using System.IO;

namespace SimpleWrite.Utils;

internal static class FileExplorerHelper
{
    /// <summary>
    /// 在系统文件资源管理器中打开文件所在位置。
    /// </summary>
    /// <param name="fileInfo">要定位的文件。</param>
    /// <returns>是否已成功发起打开请求。</returns>
    public static bool TryOpenInFileExplorer(FileInfo fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        if (!fileInfo.Exists)
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{fileInfo.FullName}\"")
            {
                UseShellExecute = true
            });
            return true;
        }

        if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo("open", $"-R \"{fileInfo.FullName}\"")
            {
                UseShellExecute = false
            });
            return true;
        }

        if (OperatingSystem.IsLinux() && fileInfo.DirectoryName is { Length: > 0 } directory)
        {
            Process.Start(new ProcessStartInfo("xdg-open", $"\"{directory}\"")
            {
                UseShellExecute = false
            });
            return true;
        }

        return false;
    }
}
