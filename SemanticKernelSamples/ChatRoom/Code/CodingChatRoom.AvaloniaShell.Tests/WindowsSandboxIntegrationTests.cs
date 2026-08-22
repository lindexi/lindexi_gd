using AgentLib.Coding.Sandboxes;
using CodingChatRoom.AvaloniaShell.Infrastructure;
using CodingChatRoom.AvaloniaShell.Services;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class WindowsSandboxIntegrationTests
{
    [TestMethod(DisplayName = "AppData 配置的真实 Windows 沙箱应完成推送执行和拉取")]
    [Timeout(120000)]
    public async Task ConfiguredWindowsSandboxShouldPushExecuteAndPull()
    {
        IntegrationContext? context = await TryCreateIntegrationContextAsync();
        if (context is null)
        {
            return;
        }

        string outputDirectory = Path.Combine(context.WorkspacePath, "sandbox-output");
        object? result = await context.Tool.InvokeAsync
        (
            new AIFunctionArguments
            {
                ["sourceDirectory"] = context.SourceDirectory,
                ["executableRelativePath"] = "WindowsSandboxProbe.exe",
                ["outputRelativePath"] = "success.marker",
                ["localOutputDirectory"] = outputDirectory,
                ["timeoutSeconds"] = 60,
            }
        );
        string resultText = result?.ToString() ?? string.Empty;

        Assert.IsFalse(resultText.Contains("沙箱执行失败", StringComparison.Ordinal), resultText);
        string markerFile = Directory
            .EnumerateFiles(outputDirectory, "success.marker", SearchOption.AllDirectories)
            .Single();
        Assert.AreEqual
        (
            "windows-sandbox-integration-success",
            (await File.ReadAllTextAsync(markerFile)).Trim()
        );
    }

    [TestMethod(DisplayName = "真实 Windows 沙箱远端启动失败应返回具体错误")]
    [Timeout(120000)]
    public async Task ConfiguredWindowsSandboxWhenExecutableIsMissingShouldReturnDetailedError()
    {
        IntegrationContext? context = await TryCreateIntegrationContextAsync();
        if (context is null)
        {
            return;
        }

        object? result = await context.Tool.InvokeAsync
        (
            new AIFunctionArguments
            {
                ["sourceDirectory"] = context.SourceDirectory,
                ["executableRelativePath"] = "missing-executable.exe",
                ["timeoutSeconds"] = 60,
            }
        );
        string resultText = result?.ToString() ?? string.Empty;

        StringAssert.Contains(resultText, "沙箱执行失败");
        StringAssert.Contains(resultText, "WinRemoteShell 远端执行失败");
        StringAssert.Contains(resultText, "Win32Exception");
    }

    private static async Task<IntegrationContext?> TryCreateIntegrationContextAsync()
    {
        CodingChatRoomPaths paths = CodingChatRoomPaths.CreateForCurrentUser();
        if (!paths.ShellSettingsFile.Exists)
        {
            return null;
        }

        CodingChatShellSettings settings;
        try
        {
            settings = await new CodingChatSettingsService(paths).LoadShellSettingsAsync();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        if (!settings.IsWindowsSandboxEnabled
            || string.IsNullOrWhiteSpace(settings.WindowsSandboxToolPath)
            || string.IsNullOrWhiteSpace(settings.WindowsSandboxServerAddress))
        {
            return null;
        }

        string probeArtifactsDirectory = Path.GetFullPath
        (
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Code", "WindowsSandboxProbe", "artifacts")
        );
        if (!File.Exists(Path.Combine(probeArtifactsDirectory, "WindowsSandboxProbe.exe")))
        {
            Assert.Fail($"未找到已构建的真实沙箱探针：{probeArtifactsDirectory}");
        }

        string workspacePath = Path.Join
        (
            Path.GetTempPath(),
            "CodingChatRoom.AvaloniaShell.Tests",
            Guid.NewGuid().ToString("N")
        );
        string sourceDirectory = Path.Combine(workspacePath, "sandbox-probe");
        Directory.CreateDirectory(sourceDirectory);
        foreach (string artifactFile in Directory.EnumerateFiles(probeArtifactsDirectory))
        {
            File.Copy(artifactFile, Path.Combine(sourceDirectory, Path.GetFileName(artifactFile)));
        }

        var toolSource = new WindowsSandboxToolSource
        (
            settings.WindowsSandboxToolPath,
            settings.WindowsSandboxServerAddress
        );
        AIFunction tool = toolSource.CreateTools(workspacePath).OfType<AIFunction>().Single();
        return new IntegrationContext(workspacePath, sourceDirectory, tool);
    }

    private sealed record IntegrationContext(string WorkspacePath, string SourceDirectory, AIFunction Tool);
}
