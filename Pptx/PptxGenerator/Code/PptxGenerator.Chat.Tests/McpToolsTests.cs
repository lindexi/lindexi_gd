using System.ClientModel.Primitives;
using System.Text.Json;
using DotNetCampus.ModelContextProtocol.Protocol.Messages;
using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat.Tests;

[TestClass]
public sealed class McpToolsTests
{
    [TestMethod]
    public void ScreenshotIsAResponsesImagePart()
    {
        var result = new CallToolResult
        {
            Content = [new ImageContentBlock { Data = "AQID", MimeType = "image/png" }],
        };

        var converted = McpTools.ConvertResult("call_image", result);
        using var json = JsonDocument.Parse(ModelReaderWriter.Write(converted.Output));
        var image = json.RootElement.GetProperty("output")[0];

        Assert.AreEqual(("input_image", "data:image/png;base64,AQID"),
            (image.GetProperty("type").GetString(), image.GetProperty("image_url").GetString()));
    }

    [TestMethod]
    public void ImageOnlyResultKeepsPreviewWithoutEmptyJson()
    {
        var result = new CallToolResult
        {
            IsError = false,
            Content = [new ImageContentBlock { Data = "AQID", MimeType = "image/png" }],
        };

        var converted = McpTools.ConvertResult("call_image", result);

        Assert.AreEqual((string.Empty, new ToolImage("image/png", "AQID")),
            (converted.DisplayText, converted.Images?.Single()));
    }

    [TestMethod]
    public void ToolErrorsRetainTheirTextAndErrorFlag()
    {
        var output = McpTools.ConvertResult("call_error", CallToolResult.FromError("Page unavailable"));
        using var json = JsonDocument.Parse(output.Output.FunctionOutput);
        var root = json.RootElement;

        Assert.AreEqual((true, "Page unavailable"),
            (root.GetProperty("isError").GetBoolean(), root.GetProperty("content")[0].GetProperty("text").GetString()));
    }
}
