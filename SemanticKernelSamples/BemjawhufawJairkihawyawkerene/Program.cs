using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML.OnnxRuntimeGenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var modelPath = @"C:\lindexi\Phi3\directml-int4-awq-block-128";

// create kernel
var builder = Kernel.CreateBuilder();
//builder.Services.AddSingleton<IChatCompletionService>(new Phi3ChatCompletionService(modelPath));
builder.AddOnnxRuntimeGenAIChatCompletion(modelPath);
var kernel = builder.Build();

// create chat
var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();

// run chat
while (true)
{
    Console.Write("Q: ");
    var userQ = Console.ReadLine();
    if (string.IsNullOrEmpty(userQ))
    {
        break;
    }
    history.AddUserMessage(userQ);

    Console.Write($"Phi3: ");
    var response = "";
    var result = chat.GetStreamingChatMessageContentsAsync(history);
    await foreach (var message in result)
    {
        Console.Write(message.Content);
        response += message.Content;
    }
    history.AddAssistantMessage(response);
    Console.WriteLine("");
}


class Phi3ChatCompletionService : IChatCompletionService
{
    public Phi3ChatCompletionService(string modelPath)
    {
        var model = new Model(modelPath);
        var tokenizer = new Tokenizer(model);

        Model = model;
        Tokenizer = tokenizer;
    }

    public IReadOnlyDictionary<string, object?> Attributes { get; set; } = new Dictionary<string, object?>();
    public Model Model { get; }
    public Tokenizer Tokenizer { get; }

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null, Kernel? kernel = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }
}