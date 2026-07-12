using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodingwhejeedenochaDeelemjuher.Tests;

[TestClass]
public sealed class WorkspaceProjectCatalogTests
{
    [TestMethod]
    public void GetProjectsInSolution_ReturnsProjectFromSlnx()
    {
        using TestWorkspace workspace = new();
        WorkspaceProjectCatalog catalog = new(workspace.RootPath);

        JsonNode result = JsonNode.Parse(catalog.GetProjectsInSolution())!;
        JsonArray projects = result["projects"]!.AsArray();

        Assert.AreEqual("Sample.slnx", Path.GetFileName(result["solution"]!.GetValue<string>()));
        CollectionAssert.AreEqual(
            new[] { "SampleApp/SampleApp.csproj" },
            projects.Select(project => project!.GetValue<string>()).ToArray());
    }

    [TestMethod]
    public void GetFilesInProject_ReturnsSourceAndProjectFilesWithoutBuildArtifacts()
    {
        using TestWorkspace workspace = new();
        Directory.CreateDirectory(Path.Combine(workspace.ProjectDirectory, "obj"));
        File.WriteAllText(Path.Combine(workspace.ProjectDirectory, "obj", "Generated.cs"), "class Generated;");
        WorkspaceProjectCatalog catalog = new(workspace.RootPath);

        JsonNode result = JsonNode.Parse(catalog.GetFilesInProject("SampleApp/SampleApp.csproj"))!;
        string[] files = result["files"]!.AsArray().Select(file => file!.GetValue<string>()).ToArray();

        CollectionAssert.Contains(files, "SampleApp/Calculator.cs");
        CollectionAssert.Contains(files, "SampleApp/Program.cs");
        CollectionAssert.Contains(files, "SampleApp/SampleApp.csproj");
        Assert.IsFalse(files.Any(file => file.Contains("/obj/", StringComparison.OrdinalIgnoreCase)));
    }
}
