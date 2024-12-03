
using System.Net;
using Microsoft.ML.OnnxRuntimeGenAI;

using System.Text;

var folder = @"C:\lindexi\Phi3\directml-int4-awq-block-128\";
if (!Directory.Exists(folder))
{
    folder = Path.GetFullPath(".");
}

var model = new Model(folder);
var tokenizer = new Tokenizer(model);

var semaphoreSlim = new SemaphoreSlim(initialCount: 1, maxCount: 1);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5017");

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapPost("/Chat", async (ChatRequest request, HttpContext context) =>
{
    var response = context.Response;
    response.StatusCode = (int) HttpStatusCode.OK;
    await response.StartAsync();
    await semaphoreSlim.WaitAsync();

    try
    {
        var streamWriter = new StreamWriter(response.Body);

        var prompt = request.Prompt;

        var generatorParams = new GeneratorParams(model);

        var sequences = tokenizer.Encode(prompt);

        generatorParams.SetSearchOption("max_length", 1024);
        generatorParams.SetInputSequences(sequences);
        generatorParams.TryGraphCaptureWithMaxBatchSize(1);

        using var tokenizerStream = tokenizer.CreateStream();
        using var generator = new Generator(model, generatorParams);

        var stringBuilder = new StringBuilder();

        while (!generator.IsDone())
        {
            generator.ComputeLogits();
            generator.GenerateNextToken();

            // 每次只会添加一个 Token 值
            // 需要调用 tokenizerStream 的解码将其转为人类可读的文本
            // 由于不是每一个 Token 都对应一个词，因此需要根据 tokenizerStream 压入进行转换，而不是直接调用 tokenizer.Decode 方法，或者调用 tokenizer.Decode 方法，每次都全部转换

            var text = Decode();

            // 有些时候这个 decodeText 是一个空文本，有些时候是一个单词
            // 空文本的可能原因是需要多个 token 才能组成一个单词
            // 在 tokenizerStream 底层已经处理了这样的情况，会在需要多个 Token 才能组成一个单词的情况下，自动合并，在多个 Token 中间的 Token 都返回空字符串，最后一个 Token 才返回组成的单词
            if (!string.IsNullOrEmpty(text))
            {
                stringBuilder.Append(text);
            }

            await streamWriter.WriteAsync(text);
            await streamWriter.FlushAsync();


            string? Decode()
            {
                // 这里的 tokenSequences 就是在输入的 sequences 后面添加 Token 内容
                ReadOnlySpan<int> tokenSequences = generator.GetSequence(0);
                // 取最后一个进行解码为文本
                var decodeText = tokenizerStream.Decode(tokenSequences[^1]);

                //// 当前全部的文本
                //var allText = tokenizer.Decode(tokenSequences);

                return decodeText;
            }
        }
    }
    finally
    {
        semaphoreSlim.Release();
        await response.CompleteAsync();
    }
});

app.Run();

record ChatRequest(string Prompt)
{
}