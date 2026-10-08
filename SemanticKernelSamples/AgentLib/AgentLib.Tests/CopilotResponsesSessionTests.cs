using AgentLib.Logging;
using AgentLib.Model;
using OpenAI.Responses;
using System.ClientModel.Primitives;

#pragma warning disable OPENAI001, SCME0001

namespace AgentLib.Tests;

[TestClass]
public sealed class CopilotResponsesSessionTests
{
    [TestMethod]
    public void SessionShouldReuseResponsesState()
    {
        var session = new CopilotChatSession();

        Assert.AreSame(session.ResponsesSession, session.ResponsesSession);
    }

    [TestMethod]
    public void DisplayMessagesShouldNotPopulateNativeHistory()
    {
        var session = new CopilotChatSession();
        session.ChatMessages.Add(CopilotChatMessage.CreateUser("display only"));

        Assert.HasCount(0, session.ResponsesSession.Items);
    }

    [TestMethod]
    public void AppendResponseShouldRetainNativeObject()
    {
        var session = new CopilotResponsesSession();
        var item = ResponseItem.CreateAssistantMessageItem("answer");
        var response = new ResponseResult();
        response.OutputItems.Add(item);

        session.AppendResponse(response);

        Assert.AreSame(item, session.Items.Single());
    }

    [TestMethod]
    public async Task StorageShouldPreserveNativeItemsAndImageOutputPatch()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"responses-store-{Guid.NewGuid():N}");
        Console.WriteLine(directory);
        var store = new FileCopilotChatSessionStore(directory);
        var session = new CopilotChatSession();
        session.ResponsesSession.AppendItem(ResponseItem.CreateUserMessageItem("input"));
        session.ResponsesSession.AppendItem(ResponseItem.CreateReasoningItem("reasoning"));
        session.ResponsesSession.AppendItem(ResponseItem.CreateFunctionCallItem("call_image", "load_image", BinaryData.FromString("{}")));
        var output = ResponseItem.CreateFunctionCallOutputItem("call_image", string.Empty);
        output.Patch.Set("$.output"u8, BinaryData.FromString("""
            [{"type":"input_image","image_url":"data:image/png;base64,AQID"}]
            """));
        session.ResponsesSession.AppendItem(output);
        var expected = session.ResponsesSession.Items.Select(item => ModelReaderWriter.Write(item).ToString()).ToArray();

        await store.SaveSessionAsync(session, null);
        var data = await store.LoadSessionAsync(session.SessionId);
        var restored = new CopilotChatSession(data.SessionId, data.StartedTime) { ResponsesSession = data.ResponsesSession };

        CopilotResponsesSession restoredState = restored.ResponsesSession
            ?? throw new InvalidOperationException("Restored state missing.");
        CollectionAssert.AreEqual(expected, restoredState.Items
            .Select(item => ModelReaderWriter.Write(item).ToString()).ToArray());
    }

    [TestMethod]
    public async Task SavingChatSessionShouldNotCreateResponsesState()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"responses-store-{Guid.NewGuid():N}");
        Console.WriteLine(directory);
        var session = new CopilotChatSession();
        var store = new FileCopilotChatSessionStore(directory);

        await store.SaveSessionAsync(session, null);

        Assert.IsNull(session.ExistingResponsesSession);
    }
}
