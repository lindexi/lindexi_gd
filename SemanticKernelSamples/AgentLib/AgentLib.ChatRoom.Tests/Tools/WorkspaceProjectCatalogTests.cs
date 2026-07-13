using System.Text.Json;

using AgentLib.ChatRoom.Tools;

namespace AgentLib.ChatRoom.Tests.Tools;

/// <summary>
/// <see cref="WorkspaceProjectCatalog"/> 的单元测试。
/// </summary>
[TestClass]
public sealed class WorkspaceProjectCatalogTests
{
    [TestMethod(DisplayName = "读取 slnx 时返回其中的项目路径")]
    public void GetProjectsInSolution_WithSlnx_ReturnsProjectPath()
    {
        string workspacePath = CreateWorkspace();
        string projectDirectory = Directory.CreateDirectory(Path.Join(workspacePath, "Source", "Demo")).FullName;
        File.WriteAllText(Path.Join(projectDirectory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(
            Path.Join(workspacePath, "Demo.slnx"),
            "<Solution><Project Path=\"Source/Demo/Demo.csproj\" /></Solution>");
        var catalog = new WorkspaceProjectCatalog(workspacePath);

        string json = catalog.GetProjectsInSolution();

        using JsonDocument document = JsonDocument.Parse(json);
        string? projectPath = document.RootElement.GetProperty("projects")[0].GetString();
        Assert.AreEqual("Source/Demo/Demo.csproj", projectPath);
    }

    [TestMethod(DisplayName = "枚举 SDK 项目时返回源文件")]
    public void GetFilesInProject_WithDefaultItems_ReturnsSourceFile()
    {
        string workspacePath = CreateWorkspace();
        File.WriteAllText(Path.Join(workspacePath, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(Path.Join(workspacePath, "Program.cs"), "internal static class Program { }");
        var catalog = new WorkspaceProjectCatalog(workspacePath);

        string json = catalog.GetFilesInProject("Demo.csproj");

        using JsonDocument document = JsonDocument.Parse(json);
        string[] files = document.RootElement.GetProperty("files")
            .EnumerateArray()
            .Select(element => element.GetString()!)
            .ToArray();
        CollectionAssert.Contains(files, "Program.cs");
    }

    [TestMethod(DisplayName = "枚举项目文件时忽略 bin 目录")]
    public void GetFilesInProject_WithBinDirectory_ExcludesGeneratedFile()
    {
        string workspacePath = CreateWorkspace();
        File.WriteAllText(Path.Join(workspacePath, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        string binPath = Directory.CreateDirectory(Path.Join(workspacePath, "bin")).FullName;
        File.WriteAllText(Path.Join(binPath, "Generated.cs"), "internal static class Generated { }");
        var catalog = new WorkspaceProjectCatalog(workspacePath);

        string json = catalog.GetFilesInProject("Demo.csproj");

        using JsonDocument document = JsonDocument.Parse(json);
        bool containsGeneratedFile = document.RootElement.GetProperty("files")
            .EnumerateArray()
            .Any(element => element.GetString() == "bin/Generated.cs");
        Assert.IsFalse(containsGeneratedFile);
    }

    [TestMethod(DisplayName = "创建目录查询器时拒绝空工作区路径")]
    public void Constructor_WithEmptyWorkspacePath_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new WorkspaceProjectCatalog(" "));
    }

    [TestMethod(DisplayName = "项目路径逃逸工作区时拒绝访问")]
    public void GetFilesInProject_WithParentTraversal_ThrowsUnauthorizedAccessException()
    {
        string workspacePath = CreateWorkspace();
        var catalog = new WorkspaceProjectCatalog(workspacePath);

        Assert.ThrowsExactly<UnauthorizedAccessException>(() => catalog.GetFilesInProject("../Outside.csproj"));
    }

    [TestMethod(DisplayName = "项目目录结果不暴露工作区绝对路径")]
    public void GetProjectsInSolution_ReturnsRelativeWorkspaceMarker()
    {
        string workspacePath = CreateWorkspace();
        File.WriteAllText(Path.Join(workspacePath, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var catalog = new WorkspaceProjectCatalog(workspacePath);

        string json = catalog.GetProjectsInSolution();

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.AreEqual(".", document.RootElement.GetProperty("workspace").GetString());
        Assert.IsFalse(json.Contains(workspacePath, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod(DisplayName = "项目 Include 指向工作区外文件时忽略该文件")]
    public void GetFilesInProject_WithExternalInclude_ExcludesExternalFile()
    {
        string workspacePath = CreateWorkspace();
        string outsidePath = Path.Join(Path.GetDirectoryName(workspacePath)!, "Outside.cs");
        File.WriteAllText(outsidePath, "internal static class Outside { }");
        File.WriteAllText(
            Path.Join(workspacePath, "Demo.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><EnableDefaultItems>false</EnableDefaultItems></PropertyGroup><ItemGroup><Compile Include=\"../Outside.cs\" /></ItemGroup></Project>");
        var catalog = new WorkspaceProjectCatalog(workspacePath);

        string json = catalog.GetFilesInProject("Demo.csproj");

        Assert.IsFalse(json.Contains("Outside.cs", StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateWorkspace()
    {
        string workspacePath = Path.Join(Path.GetTempPath(), "AgentLib.ChatRoom.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspacePath);
        return workspacePath;
    }
}
