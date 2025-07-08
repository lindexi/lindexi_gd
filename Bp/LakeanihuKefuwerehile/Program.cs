// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.AI;

AIFunction function = AIFunctionFactory.Create(() =>
{

});

ChatOptions options = new() { Tools = [function] };

var functionInvokingChatClient = new FunctionInvokingChatClient(new FakeChatClient());

functionInvokingChatClient.AllowConcurrentInvocation = true;

 await foreach (var chatResponseUpdate in functionInvokingChatClient.GetStreamingResponseAsync("Hello", options))
 {
     //FunctionCallContent f = new FunctionCallContent()
     Console.Write(chatResponseUpdate);
 }

Console.WriteLine("Hello, World!");

class FakeChatClient : IChatClient
{
    public void Dispose()
    {
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        yield break;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }
}