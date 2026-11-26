using System.Text.Json;
using DotNetCampus.ModelContextProtocol.Protocol.Messages;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Rendering;

namespace PptxGenerator.Wpf.Tests.Rendering;

[TestClass]
public sealed class SlideMlMcpToolTests
{
    [TestMethod]
    [DataRow("Register_SlideML_Resource", true)]
    [DataRow("registerslidemlresource", true)]
    [DataRow("Register_SlideML_Resource_Unsafe", false)]
    [DataRow("export_slide", false)]
    [DataRow("export_courseware_markdown", true)]
    [DataRow("export_courseware_markdown_extra", false)]
    public void DiscoveryOnlyExposesAllowedNames(string name, bool expected)
    {
        var tools = SwitchableSlideMlRenderPipeline.CreateSlideMlTools([CreateTool(name)],
            (_, _, _) => Task.FromResult(CallToolResult.Empty));

        Assert.AreEqual(expected, tools.Any());
    }

    [TestMethod]
    public void DiscoveryExposesMarkdownAndResourceToolsTogether()
    {
        var functions = SwitchableSlideMlRenderPipeline.CreateSlideMlTools(
            [CreateTool("export_courseware_markdown"), CreateTool("Register_SlideML_Resource")],
            (_, _, _) => Task.FromResult(CallToolResult.Empty));

        CollectionAssert.AreEquivalent(new[] { "export_courseware_markdown", "Register_SlideML_Resource" },
            functions.Select(function => function.Name).ToArray());
    }

    [TestMethod]
    [DataRow("Register_SlideML_Resource")]
    [DataRow("export_courseware_markdown")]
    public void DiscoveryPreservesServerSchema(string name)
    {
        var tool = CreateTool(name);
        var functions = SwitchableSlideMlRenderPipeline.CreateSlideMlTools([tool],
            (_, _, _) => Task.FromResult(CallToolResult.Empty));

        Assert.AreEqual(tool.InputSchema.GetRawText(), functions.Single().JsonSchema.GetRawText());
    }

    [TestMethod]
    [DataRow("Register_SlideML_Resource")]
    [DataRow("export_courseware_markdown")]
    public async Task InvocationForwardsArgumentsAndReturnsStructuredResult(string toolName)
    {
        var tool = CreateTool(toolName);
        var function = SwitchableSlideMlRenderPipeline.CreateSlideMlTools([tool],
            (name, arguments, _) => Task.FromResult(new CallToolResult
            {
                StructuredContent = JsonSerializer.SerializeToElement(new { name, arguments }),
            })).Single();

        var result = (JsonElement)(await function.InvokeAsync(new AIFunctionArguments
        {
            ["input"] = new[] { "example.png" },
        }))!;

        var expected = JsonSerializer.Serialize(new { name = toolName, arguments = new { input = new[] { "example.png" } } });
        Assert.AreEqual(expected, result.GetRawText());
    }

    private static Tool CreateTool(string name) => new()
    {
        Name = name,
        InputSchema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new { input = new { type = "array", items = new { type = "string" } } },
        }),
    };
}
