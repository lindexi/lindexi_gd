using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML.OnnxRuntimeGenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using System.Text;

var modelPath = @"C:\lindexi\Phi3\directml-int4-awq-block-128";

// create kernel
var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton<IChatCompletionService>(new Phi3ChatCompletionService(modelPath));
//builder.AddOnnxRuntimeGenAIChatCompletion(modelPath);
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

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null, Kernel? kernel = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var stringBuilder = new StringBuilder();
        foreach (ChatMessageContent chatMessageContent in  chatHistory)
        {
            stringBuilder.Append($"<|{chatMessageContent.Role}|>\n{chatMessageContent.Content}");
        }
        stringBuilder.Append("<|end|>\n<|assistant|>");

        var prompt = stringBuilder.ToString();

        var generatorParams = new GeneratorParams(Model);

        var sequences = Tokenizer.Encode(prompt);

        generatorParams.SetSearchOption("max_length", 1024);
        generatorParams.SetInputSequences(sequences);
        generatorParams.TryGraphCaptureWithMaxBatchSize(1);

        using var tokenizerStream = Tokenizer.CreateStream();
        using var generator = new Generator(Model, generatorParams);

        while (!generator.IsDone())
        {
            var result = await Task.Run(() =>
            {
                generator.ComputeLogits();
                generator.GenerateNextToken();

                // 这里的 tokenSequences 就是在输入的 sequences 后面添加 Token 内容

                // 取最后一个进行解码为文本
                var lastToken = generator.GetSequence(0)[^1];
                var decodeText = tokenizerStream.Decode(lastToken);

                // 有些时候这个 decodeText 是一个空文本，有些时候是一个单词
                // 空文本的可能原因是需要多个 token 才能组成一个单词
                // 在 tokenizerStream 底层已经处理了这样的情况，会在需要多个 Token 才能组成一个单词的情况下，自动合并，在多个 Token 中间的 Token 都返回空字符串，最后一个 Token 才返回组成的单词
                if (!string.IsNullOrEmpty(decodeText))
                {
                    return decodeText;
                }

                return null;
            });

            if (!string.IsNullOrEmpty(result))
            {
                yield return new StreamingChatMessageContent(AuthorRole.Assistant, result);
            }
        }
    }
}