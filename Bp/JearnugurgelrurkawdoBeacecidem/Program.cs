
using System.Net;
using Microsoft.ML.OnnxRuntimeGenAI;

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;

var folder = @"C:\lindexi\Phi3\directml-int4-awq-block-128\";
if (!Directory.Exists(folder))
{
    folder = Path.GetFullPath(".");
}

Model model = new Model(folder);

var semaphoreSlim = new SemaphoreSlim(initialCount: 1, maxCount: 1);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5017");
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSimpleConsole());
// Add services to the container.

var logFile = "ChatLog.txt";
var chatSessionFolder = "ChatSession";
Directory.CreateDirectory(chatSessionFolder);

var app = builder.Build();

// Configure the HTTP request pipeline.

//Task.Run(async () =>
//{
//    var httpClient = new HttpClient();
//    var text = await httpClient.GetStringAsync("http://127.0.0.1:5017/Status");
//    var response = await httpClient.PostAsync("http://127.0.0.1:5017/Chat", JsonContent.Create(new ChatRequest("测试")));
//});

int current = 0;
int total = 0;

app.MapGet("/Status", () => $"Current={current};Total={total}");

app.MapPost("/Chat", async (ChatRequest request, HttpContext context, [FromServices] ILogger<ChatSessionLogInfo> logger) =>
{
    var response = context.Response;
    response.StatusCode = (int) HttpStatusCode.OK;
    await response.StartAsync();
    await semaphoreSlim.WaitAsync();

    Interlocked.Increment(ref current);
    Interlocked.Increment(ref total);
    var sessionName = $"{DateTime.Now:yyyy-MM-dd_HHmmssfff}";

    try
    {
        var headerDictionary = context.Request.Headers;
        string traceId = string.Empty;
        if (headerDictionary.TryGetValue("X-APM-TraceId", out StringValues traceIdValue))
        {
            traceId = traceIdValue.ToString();
        }

        var streamWriter = new StreamWriter(response.Body);

        var prompt = request.Prompt;

        logger.LogInformation($"Session={sessionName};TraceId={traceId}\r\nPrompt={request.Prompt}");

        var generatorParams = new GeneratorParams(model);

        using var tokenizer = new Tokenizer(model);
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

        var responseText = stringBuilder.ToString();
        var contents = $"""
                        Session={sessionName}
                        TraceId={traceId}
                        ----------------
                        Request: 
                        {prompt}
                        ----------------
                        Response:
                        {responseText}
                        =================

                        """;
        await File.AppendAllTextAsync
        (
            logFile,
            contents
        );

        logger.LogInformation(contents);

        var chatSessionLogInfo = new ChatSessionLogInfo(prompt, responseText);
        var chatSessionLogInfoJson = JsonSerializer.Serialize(chatSessionLogInfo, new JsonSerializerOptions()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        var sessionLogFile = Path.Join(chatSessionFolder, $"{sessionName}_{Path.GetRandomFileName()}.txt");
        await File.WriteAllTextAsync(sessionLogFile, chatSessionLogInfoJson);
    }
    finally
    {
        semaphoreSlim.Release();
        await response.CompleteAsync();
        Interlocked.Decrement(ref current);
    }
});

app.Run();

record ChatRequest(string Prompt)
{
}

record ChatSessionLogInfo(string Request, string Response)
{
}