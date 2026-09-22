using AgentLib;

using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class CodingWorkspaceControllerTests
{
    [TestMethod(DisplayName = "有效目录应规范化为下一轮工作路径")]
    public async Task ChangeWorkspaceAsync_WhenDirectoryExists_SetsNextRunPath()
    {
        string workspacePath = CreateTestDirectory();
        var controller = CreateController();

        WorkspaceChangeResult result = await controller.ChangeWorkspaceAsync(Path.Join(workspacePath, "."));

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(Path.GetFullPath(workspacePath), controller.NextRunWorkspacePath);
        Assert.AreEqual(controller.NextRunWorkspacePath, controller.WorkspaceInput);
    }

    [TestMethod(DisplayName = "不存在目录应失败且保留下一轮路径")]
    public async Task ChangeWorkspaceAsync_WhenDirectoryDoesNotExist_KeepsNextRunPath()
    {
        string currentPath = CreateTestDirectory();
        string missingPath = Path.Join(CreateTestDirectory(), "missing");
        var controller = CreateController();
        await controller.ChangeWorkspaceAsync(currentPath);

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() => controller.ChangeWorkspaceAsync(missingPath));

        Assert.AreEqual(Path.GetFullPath(currentPath), controller.NextRunWorkspacePath);
        StringAssert.Contains(controller.StatusText, "不存在");
    }

    [TestMethod(DisplayName = "禁用路径校验时应允许不存在的目录")]
    public async Task ChangeWorkspaceAsync_WhenValidationIsDisabled_AllowsMissingDirectory()
    {
        string missingPath = Path.Join(CreateTestDirectory(), "missing");
        var controller = CreateController();

        await controller.ChangeWorkspaceAsync(missingPath, validatePath: false);

        Assert.AreEqual(Path.GetFullPath(missingPath), controller.NextRunWorkspacePath);
        Assert.AreEqual(controller.NextRunWorkspacePath, controller.WorkspaceInput);
    }

    [TestMethod(DisplayName = "相同规范化路径不应标记为变化")]
    public async Task ChangeWorkspaceAsync_WhenNormalizedPathIsUnchanged_ReturnsUnchanged()
    {
        string workspacePath = CreateTestDirectory();
        var controller = CreateController();
        await controller.ChangeWorkspaceAsync(workspacePath);

        WorkspaceChangeResult result = await controller.ChangeWorkspaceAsync(Path.Join(workspacePath, "."));

        Assert.IsFalse(result.Changed);
    }

    [TestMethod(DisplayName = "空白路径应清除下一轮工作路径")]
    public async Task ChangeWorkspaceAsync_WhenPathIsBlank_ClearsNextRunPath()
    {
        var controller = CreateController();
        await controller.ChangeWorkspaceAsync(CreateTestDirectory());

        WorkspaceChangeResult result = await controller.ChangeWorkspaceAsync("   ");

        Assert.IsTrue(result.Changed);
        Assert.IsNull(controller.NextRunWorkspacePath);
        Assert.AreEqual(string.Empty, controller.WorkspaceInput);
    }

    [TestMethod(DisplayName = "Windows 路径比较应忽略大小写")]
    public async Task ChangeWorkspaceAsync_WithWindowsComparer_DoesNotRepeatForCaseDifference()
    {
        string workspacePath = CreateTestDirectory();
        var controller = new CodingWorkspaceController(new ImmediateMainThreadDispatcher(), StringComparer.OrdinalIgnoreCase);
        await controller.ChangeWorkspaceAsync(workspacePath);

        WorkspaceChangeResult result = await controller.ChangeWorkspaceAsync(workspacePath.ToUpperInvariant());

        Assert.IsFalse(result.Changed);
    }

    private static CodingWorkspaceController CreateController() =>
        new(new ImmediateMainThreadDispatcher(), StringComparer.Ordinal);

    private static string CreateTestDirectory()
    {
        string path = Path.Join(Path.GetTempPath(), $"CodingChatRoom.Workspace.{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class ImmediateMainThreadDispatcher : IMainThreadDispatcher
    {
        public Task InvokeAsync(Func<Task> action) => action();

        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();

        public bool CheckAccess() => true;
    }
}
