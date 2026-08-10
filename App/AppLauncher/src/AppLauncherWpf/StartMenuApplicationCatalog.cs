using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

namespace AppLauncherWpf;

internal static class StartMenuApplicationCatalog
{
    public static Task<IReadOnlyList<ApplicationEntry>> GetApplicationsAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource<IReadOnlyList<ApplicationEntry>> completionSource = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Thread discoveryThread = new(() =>
        {
            try
            {
                completionSource.SetResult(GetApplications(cancellationToken));
            }
            catch (OperationCanceledException)
            {
                completionSource.SetCanceled(cancellationToken);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or COMException)
            {
                completionSource.SetException(exception);
            }
        })
        {
            IsBackground = true,
            Name = "Application discovery"
        };
        discoveryThread.SetApartmentState(ApartmentState.STA);
        discoveryThread.Start();

        return completionSource.Task;
    }

    private static IReadOnlyList<ApplicationEntry> GetApplications(CancellationToken cancellationToken)
    {
        List<ApplicationEntry> discoveredApplications = [];
        discoveredApplications.AddRange(GetShellApplications());
        discoveredApplications.AddRange(GetStartMenuApplications(cancellationToken));
        discoveredApplications.AddRange(GetAppsFolderApplications(cancellationToken));

        return discoveredApplications
            .GroupBy(application => application.EntryPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .GroupBy(application => application.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(group => group
                .OrderByDescending(application => GetSourcePriority(application.Type))
                .ThenBy(application => application.EntryPath, StringComparer.OrdinalIgnoreCase)
                .First())
            .OrderBy(application => application.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<ApplicationEntry> GetStartMenuApplications(CancellationToken cancellationToken)
    {
        string[] startMenuDirectories =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
        ];

        foreach (string startMenuDirectory in startMenuDirectories.Where(Directory.Exists))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (string shortcutPath in EnumerateShortcuts(startMenuDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string name = Path.GetFileNameWithoutExtension(shortcutPath);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string? executablePath = ShellLinkResolver.TryResolveTargetPath(shortcutPath);
                if (executablePath is not null && !File.Exists(executablePath) && !Directory.Exists(executablePath))
                {
                    continue;
                }

                yield return new ApplicationEntry(
                    ApplicationType.Win32Desktop,
                    name,
                    shortcutPath,
                    "开始菜单",
                    executablePath);
            }
        }
    }

    private static IEnumerable<ApplicationEntry> GetAppsFolderApplications(CancellationToken cancellationToken)
    {
        Type? shellType = Type.GetTypeFromProgID("Shell.Application");
        if (shellType is null)
        {
            yield break;
        }

        object? shell = null;
        object? appsFolder = null;
        object? items = null;

        try
        {
            shell = Activator.CreateInstance(shellType);
            dynamic dynamicShell = shell!;
            appsFolder = dynamicShell.NameSpace("shell:AppsFolder");
            if (appsFolder is null)
            {
                yield break;
            }

            dynamic dynamicAppsFolder = appsFolder;
            items = dynamicAppsFolder.Items();

            foreach (object item in (IEnumerable)items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    dynamic dynamicItem = item;
                    string? name = dynamicItem.Name as string;
                    string? applicationUserModelId = dynamicItem.ExtendedProperty("System.AppUserModel.ID") as string;

                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(applicationUserModelId))
                    {
                        yield return new ApplicationEntry(
                            ApplicationType.PackagedApplication,
                            name,
                            $"shell:AppsFolder\\{applicationUserModelId}",
                            "已安装应用");
                    }
                }
                finally
                {
                    Marshal.FinalReleaseComObject(item);
                }
            }
        }
        finally
        {
            ReleaseComObject(items);
            ReleaseComObject(appsFolder);
            ReleaseComObject(shell);
        }
    }

    private static IEnumerable<ApplicationEntry> GetShellApplications()
    {
        yield return new ApplicationEntry(
            ApplicationType.Shell,
            "设置",
            "ms-settings:",
            "Windows 系统",
            Aliases: ["系统设置", "Windows 设置", "Settings"]);
        yield return new ApplicationEntry(
            ApplicationType.Shell,
            "控制面板",
            "shell:ControlPanelFolder",
            "Windows 系统",
            Aliases: ["Control Panel"]);
    }

    private static IEnumerable<string> EnumerateShortcuts(string startMenuDirectory)
    {
        Stack<string> pendingDirectories = new();
        pendingDirectories.Push(startMenuDirectory);

        while (pendingDirectories.TryPop(out string? directory))
        {
            string[] files;
            string[] directories;

            try
            {
                files = Directory.GetFiles(directory, "*.lnk");
                directories = Directory.GetDirectories(directory);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (string file in files.Order(StringComparer.OrdinalIgnoreCase))
            {
                yield return file;
            }

            foreach (string childDirectory in directories.OrderDescending(StringComparer.OrdinalIgnoreCase))
            {
                pendingDirectories.Push(childDirectory);
            }
        }
    }

    private static int GetSourcePriority(ApplicationType applicationType) => applicationType switch
    {
        ApplicationType.Shell => 3,
        ApplicationType.Win32Desktop => 2,
        ApplicationType.PackagedApplication => 1,
        _ => 0
    };

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}
