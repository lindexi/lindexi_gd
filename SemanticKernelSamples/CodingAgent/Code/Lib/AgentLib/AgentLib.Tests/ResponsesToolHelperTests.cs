#pragma warning disable OPENAI001

using System.ClientModel.Primitives;
using System.Text.Json;
using AgentLib.Tools;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace AgentLib.Tests;

[TestClass]
public sealed class ResponsesToolHelperTests
{
    [TestMethod]
    public void ImageOutput_SerializesAsNativeContentArray()
    {
        var image = new DataContent(new byte[] { 1, 2, 3 }, "image/png");

        var output = ResponsesToolHelper.CreateOutput("image-call", image);
        using var json = JsonDocument.Parse(ModelReaderWriter.Write(output).ToString());

        Assert.AreEqual("input_image", json.RootElement.GetProperty("output")[0].GetProperty("type").GetString());
        Assert.AreEqual("data:image/png;base64,AQID", json.RootElement.GetProperty("output")[0].GetProperty("image_url").GetString());
        Assert.AreEqual("image-call", json.RootElement.GetProperty("call_id").GetString());
    }

    [TestMethod]
    public async Task InvokeAsync_UsesOriginalFunctionAndReturnsResult()
    {
        var function = AIFunctionFactory.Create((int value) => value + 1, "increment");
        var call = ResponseItem.CreateFunctionCallItem("call-1", "increment", BinaryData.FromString("{\"value\":4}"));

        var output = await ResponsesToolHelper.InvokeAsync(function, call);

        Assert.AreEqual("5", output.FunctionOutput);
    }

    [TestMethod]
    public async Task InvokeAsync_UsesExistingWorkspaceReadToolWithoutModification()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ResponsesToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "sample.txt");
        await File.WriteAllTextAsync(path, "first line\nexpected second line\nthird line");
        Console.WriteLine($"Tool fixture: {path}");
        var provider = new WorkspaceToolProvider { WorkspacePath = directory };
        var function = provider.CreateDefaultTools().OfType<AIFunction>()
            .Single(tool => tool.Name == nameof(WorkspaceToolProvider.ReadFileLines));
        var call = ResponseItem.CreateFunctionCallItem("read-call", function.Name, BinaryData.FromObjectAsJson(new
        {
            filePath = "sample.txt", startLine = 2, endLine = 2, includeLineNumbers = false
        }));

        var output = await ResponsesToolHelper.InvokeAsync(function, call);

        StringAssert.Contains(output.FunctionOutput, "expected second line");
        Assert.IsFalse(output.FunctionOutput.Contains("third line", StringComparison.Ordinal));
        Assert.AreEqual("read-call", output.CallId);
    }

    [TestMethod]
    public async Task InvokeAsync_PreservesStructuredReturnValue()
    {
        var function = AIFunctionFactory.Create((int value) => new { Value = value, IsEven = value % 2 == 0 }, "inspect");
        var call = ResponseItem.CreateFunctionCallItem("inspect-call", function.Name, BinaryData.FromString("{\"value\":4}"));

        var output = await ResponsesToolHelper.InvokeAsync(function, call);
        using var json = JsonDocument.Parse(output.FunctionOutput);

        Assert.AreEqual(JsonValueKind.Object, json.RootElement.ValueKind);
        Assert.AreEqual(4, json.RootElement.EnumerateObject().Single(property => property.Name.Equals("value", StringComparison.OrdinalIgnoreCase)).Value.GetInt32());
    }

    [TestMethod]
    public async Task InvokeAsync_PassesInvocationCancellationToken()
    {
        using var source = new CancellationTokenSource();
        CancellationToken observed = default;
        var function = AIFunctionFactory.Create((CancellationToken cancellationToken) =>
        {
            observed = cancellationToken;
            return "done";
        }, "observe_cancellation");
        var call = ResponseItem.CreateFunctionCallItem("token-call", function.Name, BinaryData.FromString("{}"));

        await ResponsesToolHelper.InvokeAsync(function, call, source.Token);

        Assert.AreEqual(source.Token, observed);
    }

    [TestMethod]
    public void StringOutput_IsNotJsonQuoted()
    {
        var output = ResponsesToolHelper.CreateOutput("text-call", "plain text");

        Assert.AreEqual("plain text", output.FunctionOutput);
    }

    [TestMethod]
    public void CreateTool_PreservesNameAndSchema()
    {
        var function = AIFunctionFactory.Create((int value) => value, "echo_number");

        using var json = JsonDocument.Parse(ModelReaderWriter.Write(ResponsesToolHelper.CreateTool(function)).ToString());

        Assert.AreEqual("echo_number", json.RootElement.GetProperty("name").GetString());
        Assert.IsTrue(json.RootElement.GetProperty("parameters").GetProperty("properties").TryGetProperty("value", out _));
    }
}
