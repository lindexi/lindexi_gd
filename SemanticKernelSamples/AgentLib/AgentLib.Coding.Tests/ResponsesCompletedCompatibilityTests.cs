using System.ClientModel.Primitives;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class ResponsesCompletedCompatibilityTests
{
    [TestMethod]
    public void CompletedResponseWithNullAnnotationsShouldReproduceSdkReaderFailure()
    {
        var json = BinaryData.FromString("""
            {"type":"response.completed","sequence_number":0,"response":{"id":"resp_test","status":"completed","output":[{"type":"message","id":"msg_test","role":"assistant","status":"completed","content":[{"type":"output_text","text":"answer","annotations":null}]}]}}
            """);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ModelReaderWriter.Read<StreamingResponseUpdate>(json));
    }

    [TestMethod]
    public void CompletedResponseWithEmptyAnnotationsShouldBeReadable()
    {
        var json = BinaryData.FromString("""
            {"type":"response.completed","sequence_number":0,"response":{"id":"resp_test","status":"completed","output":[{"type":"message","id":"msg_test","role":"assistant","status":"completed","content":[{"type":"output_text","text":"answer","annotations":[]}]}]}}
            """);

        var update = (StreamingResponseCompletedUpdate?)ModelReaderWriter.Read<StreamingResponseUpdate>(json)
            ?? throw new InvalidOperationException("Response missing.");

        Assert.AreEqual("answer", update.Response.GetOutputText());
    }
}
