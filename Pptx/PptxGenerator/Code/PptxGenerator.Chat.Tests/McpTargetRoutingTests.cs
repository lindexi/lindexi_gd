using System.Text.Json;
using DotNetCampus.ModelContextProtocol.Protocol.Messages;
using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat.Tests;

[TestClass]
public sealed class McpTargetRoutingTests
{
    private static Uri Address => new("http://localhost:7916/mcp");

    [TestMethod]
    [DataRow("vendor_find_shell_targets", "find_shell")]
    [DataRow("vendor_BIND_SHELL_target", "bind_shell")]
    public void DiscoversToolByNameFragment(string name, string fragment)
    {
        var schema = JsonSerializer.SerializeToElement(new { type = "object" });
        var tools = new[] { new Tool { Name = "get_slide_info", InputSchema = schema }, new Tool { Name = name, InputSchema = schema } };

        Assert.AreEqual(name, McpTargetRouting.FindTool(tools, fragment)?.Name);
    }

    [TestMethod]
    public async Task UnboundCallReturnsGuidanceWithoutForwardingAsync()
    {
        var routing = new McpTargetRouting();
        var result = await routing.InvokeAsync(Address, "RenderSlideML", true, JsonSerializer.SerializeToElement(new { }),
            (_, _, _) => throw new AssertFailedException("Unexpected MCP call."), CancellationToken.None);

        Assert.IsTrue(result.IsError == true && result.Content.OfType<TextContentBlock>().Single().Text.Contains(McpTargetRouting.BindTool));
    }

    [TestMethod]
    public async Task ListedTargetCanBeBoundAndUsedBySubsequentCallsAsync()
    {
        var routing = new McpTargetRouting();
        await routing.InvokeAsync(Address, McpTargetRouting.ListTool, false, JsonSerializer.SerializeToElement(new { }),
            (_, args, _) =>
            {
                Assert.AreEqual("All", args?.GetProperty("searchingMode").GetString());
                return Task.FromResult(CallToolResult.FromResult("{\"Shells\":[{\"Id\":\"123\"}]}"));
            }, CancellationToken.None);
        await routing.InvokeAsync(Address, McpTargetRouting.BindTool, false, JsonSerializer.SerializeToElement(new { targetId = "123" }),
            (_, args, _) =>
            {
                Assert.AreEqual("123", args?.GetProperty("shellId").GetString());
                return Task.FromResult(CallToolResult.FromResult("OK"));
            }, CancellationToken.None);

        string? target = null;
        await routing.InvokeAsync(Address, "get_slide_info", true, JsonSerializer.SerializeToElement(new { shellId = "456" }),
            (_, args, _) =>
            {
                target = args?.GetProperty("shellId").GetString();
                return Task.FromResult(CallToolResult.FromResult("OK"));
            }, CancellationToken.None);

        Assert.AreEqual("123", target);
    }

    [TestMethod]
    public async Task RestoredBindingRoutesWithoutListingAgainAsync()
    {
        var routing = new McpTargetRouting();
        routing.Restore(new McpTargetBinding(Address.AbsoluteUri, "123"));

        var result = await routing.InvokeAsync(Address, "get_slide_info", true, JsonSerializer.SerializeToElement(new { }),
            (_, args, _) => Task.FromResult(CallToolResult.FromResult(args?.GetProperty("shellId").GetString() ?? "")),
            CancellationToken.None);

        Assert.AreEqual("123", result.Content.OfType<TextContentBlock>().Single().Text);
    }
}
