using System.Diagnostics;
using System.Security.Principal;

namespace XiaoXiIme.SandboxRunner;

internal static class VmDeployment
{
    internal static int Run(string package, string output)
    {
        Directory.CreateDirectory(output);
        const string target = @"C:\lindexi\IME";
        using var log = new StreamWriter(Path.Combine(output, "deployment.log")) { AutoFlush = true };
        var stage = "preflight";
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                return 2;
            }
            using var identity = WindowsIdentity.GetCurrent();
            Record($"Machine={Environment.MachineName}; User={identity.Name}; Target={target}");
            if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            {
                throw new UnauthorizedAccessException("Deployment requires an elevated Windows session.");
            }

            var oldLauncher = Path.Combine(target, "XiaoXiIme.exe");
            var newLauncher = Path.Combine(package, "XiaoXiIme.exe");
            if (!File.Exists(oldLauncher) || !File.Exists(newLauncher) ||
                !File.Exists(Path.Combine(package, "native", "ime", "win-x64", "XiaoXiIme.ime")) ||
                !File.Exists(Path.Combine(package, "native", "ime", "win-x86", "XiaoXiIme.ime")))
            {
                throw new InvalidOperationException("The existing launcher or required new package files are missing.");
            }
            for (var directory = new DirectoryInfo(target); directory is not null; directory = directory.Parent)
            {
                if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new IOException($"Deployment destination traverses a reparse point: {directory.FullName}");
                }
            }
            var relative = Path.GetRelativePath(target, package);
            if (!Path.IsPathRooted(relative) && !relative.StartsWith("..\\", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The deployment payload must be outside the directory being cleared.");
            }
            var payload = new[] { "Dictionaries", "dotnet", "Lib", "native", "Tests" };
            foreach (var name in payload)
            {
                if (!Directory.Exists(Path.Combine(package, name)))
                {
                    throw new DirectoryNotFoundException($"Missing package directory: {name}");
                }
            }

            stage = "uninstall";
            RunCommand(oldLauncher, stage);
            stage = "clear";
            Record($"Clearing {target}");
            foreach (var entry in new DirectoryInfo(target).EnumerateFileSystemInfos())
            {
                DeleteEntry(entry);
            }

            stage = "copy";
            Record($"Copying published package to {target}");
            foreach (var name in payload)
            {
                CopyDirectory(Path.Combine(package, name), Path.Combine(target, name));
            }
            foreach (var file in Directory.EnumerateFiles(package))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
            }

            stage = "install";
            RunCommand(oldLauncher, stage);
            Record("Deployment complete; installation retained.");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Record($"Failed at {stage}: {exception.Message}");
            return 1;
        }

        void Record(string message)
        {
            log.WriteLine($"{DateTimeOffset.UtcNow:O} {message}");
            Console.WriteLine(message);
        }

        void RunCommand(string executable, string command)
        {
            Record($"Running {command}: {executable}");
            var info = new ProcessStartInfo(executable)
            {
                WorkingDirectory = target,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            info.ArgumentList.Add(command);
            using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {command}.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (!process.WaitForExit(90_000))
            {
                throw new InvalidOperationException($"{command} did not exit within 90 seconds.");
            }
            Record($"{command} exit code: {process.ExitCode}");
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"{command} failed.");
            }
        }
    }

    internal static async Task<int> CheckAsync(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "deployment-check.log")) { AutoFlush = true };
        const string executable = @"C:\lindexi\IME\XiaoXiIme.exe";
        log.WriteLine($"Machine={Environment.MachineName}; Launcher={File.Exists(executable)}; x64={File.Exists(Path.Combine(Environment.SystemDirectory, "XiaoXiIme.ime"))}; x86={File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "XiaoXiIme.ime"))}");
        using var client = new XiaoXiIme.ImeIpc.XiaoXiImeIpcClient();
        try
        {
            var status = await client.WaitUntilReadyAsync();
            log.WriteLine($"ServiceReady={status.IsRunning}; Scheme={status.DictionaryInputScheme}");
            return status.IsRunning ? 0 : 1;
        }
        catch (TimeoutException exception)
        {
            log.WriteLine($"ServiceReady=False; {exception.Message}");
            return 1;
        }
    }

    private static void DeleteEntry(FileSystemInfo entry)
    {
        if (entry is DirectoryInfo directory && !directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            foreach (var child in directory.EnumerateFileSystemInfos())
            {
                DeleteEntry(child);
            }
        }
        if (!entry.Attributes.HasFlag(FileAttributes.ReparsePoint) && entry.Attributes.HasFlag(FileAttributes.ReadOnly))
        {
            entry.Attributes &= ~FileAttributes.ReadOnly;
        }
        entry.Delete();
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
