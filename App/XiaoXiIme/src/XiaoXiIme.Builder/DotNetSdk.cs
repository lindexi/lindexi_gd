using System.ComponentModel;
using System.Diagnostics;

namespace XiaoXiIme.Builder;

internal sealed class DotNetSdk(string command, string workingDirectory)
{
    internal string Command { get; } = command;
    internal bool IsLocal => Path.IsPathFullyQualified(Command);

    internal static DotNetSdk Resolve(DirectoryInfo solutionDirectory)
    {
        var workspace = solutionDirectory.Parent;
        if (workspace is null)
        {
            return new DotNetSdk("dotnet", solutionDirectory.FullName);
        }

        var sdkDirectory = Path.Combine(workspace.FullName, "DotNetSDK");
        try
        {
            if ((File.GetAttributes(sdkDirectory) & FileAttributes.Directory) == 0)
            {
                throw new IOException($"本地 SDK 路径不是目录：{sdkDirectory}。");
            }
        }
        catch (FileNotFoundException)
        {
            return new DotNetSdk("dotnet", solutionDirectory.FullName);
        }
        catch (DirectoryNotFoundException)
        {
            return new DotNetSdk("dotnet", solutionDirectory.FullName);
        }

        var executable = Path.Combine(sdkDirectory, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        if (!File.Exists(executable))
        {
            throw new FileNotFoundException($"本地 SDK 缺少 dotnet 可执行文件：{executable}。", executable);
        }
        return new DotNetSdk(executable, solutionDirectory.FullName);
    }

    internal ProcessStartInfo CreateStartInfo()
    {
        var info = new ProcessStartInfo(Command) { UseShellExecute = false, WorkingDirectory = workingDirectory };
        if (IsLocal)
        {
            var sdkDirectory = Path.GetDirectoryName(Command)!;
            // A Builder launched by another SDK can inherit that SDK's MSBuild and host paths.
            foreach (var name in new[] { "MSBUILD_EXE_PATH", "MSBuildExtensionsPath", "MSBuildSDKsPath",
                "DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR", "DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR", "DOTNET_MSBUILD_SDK_RESOLVER_SDKS_VER" })
            {
                info.Environment.Remove(name);
            }
            info.Environment["DOTNET_HOST_PATH"] = Command;
            info.Environment["DOTNET_ROOT"] = sdkDirectory;
            info.Environment["DOTNET_ROOT_X64"] = sdkDirectory;
            info.Environment["NUGET_PACKAGES"] = Path.Combine(sdkDirectory, "nuget-packages");
        }
        return info;
    }

    internal async Task ValidateLocalAsync()
    {
        var info = CreateStartInfo();
        info.ArgumentList.Add("--version");
        try
        {
            using var process = Process.Start(info) ?? throw new InvalidOperationException($"无法启动本地 SDK 可执行文件：{Command}。");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                await process.WaitForExitAsync().ConfigureAwait(false);
                throw new InvalidOperationException($"本地 SDK 可执行文件未在规定时间内响应：{Command}。");
            }
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"本地 SDK 可执行文件未通过版本检查：{Command}（退出代码 {process.ExitCode}）。");
            }
        }
        catch (Win32Exception error)
        {
            throw new InvalidOperationException($"无法启动本地 SDK 可执行文件：{Command}。", error);
        }
    }

    internal void LogSelection() => Console.WriteLine(IsLocal
        ? $"[Builder][SelectSdk] 使用本地 SDK：{Command}"
        : $"[Builder][SelectSdk] 使用系统 SDK：{Command}");
}
