using System.IO;
using OpenAI.Responses;
using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat.Tests;

[TestClass]
public sealed class CoursewareSkillTests
{
    [TestMethod]
    public async Task RegistrationAdvertisesSkillWithoutInjectingDocumentsAsync()
    {
        var directory = Directory.CreateTempSubdirectory("CoursewareSkill-").FullName;
        var token = TestContext.CancellationToken;
        await File.WriteAllTextAsync(Path.Combine(directory, "SKILL.md"),
            "---\ndescription: Courseware skill summary\n---\nEntry body marker", token);
        Directory.CreateDirectory(Path.Combine(directory, "references"));
        await File.WriteAllTextAsync(Path.Combine(directory, "references", "example.md"), "Reference body marker", token);
        var tools = new Dictionary<string, ChatTool>();

        var catalog = await new CoursewareSkill(directory).RegisterAsync(tools, "Read skill", "Read failed", token);

        Assert.AreEqual((true, true, false, false), (tools.ContainsKey(CoursewareSkill.ToolName),
            catalog.Contains("Courseware skill summary"), catalog.Contains("Entry body marker"), catalog.Contains("Reference body marker")));
    }

    [TestMethod]
    [DataRow("SKILL.md")]
    [DataRow("references/renderer-behavior.md")]
    public async Task RegisteredToolLoadsRequestedBundledDocumentDirectlyAsync(string path)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Assets", "Skills", "courseware-slideml");
        var token = TestContext.CancellationToken;
        var expected = await File.ReadAllTextAsync(Path.Combine(directory, path), token);
        var tools = new Dictionary<string, ChatTool>();
        await new CoursewareSkill(directory).RegisterAsync(tools, "Read skill", "Read failed", token);
        var call = ResponseItem.CreateFunctionCallItem("call_skill", CoursewareSkill.ToolName, BinaryData.FromObjectAsJson(new { path }));

        var result = await tools[CoursewareSkill.ToolName].InvokeAsync(call, token);

        Assert.AreEqual((false, expected), (result.IsError, result.Output.FunctionOutput));
    }

    public TestContext TestContext { get; set; } = null!;
}
