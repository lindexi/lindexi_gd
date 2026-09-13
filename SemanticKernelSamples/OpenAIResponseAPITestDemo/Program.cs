using System.Text.Json;
using OpenAI;
using OpenAI.Responses;

const string ApiKeyEnvironmentVariable = "OPENAI_API_KEY";
const string ModelEnvironmentVariable = "OPENAI_MODEL";
const string DefaultModel = "gpt-4.1-mini";

string? apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine($"请先设置环境变量 {ApiKeyEnvironmentVariable}。");
    return 1;
}

string model = Environment.GetEnvironmentVariable(ModelEnvironmentVariable) ?? DefaultModel;
string demo = args.FirstOrDefault()?.ToLowerInvariant() ?? "basic";

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

ResponsesClient client = new OpenAIClient(apiKey).GetResponsesClient();

try
{
    switch (demo)
    {
        case "basic":
            await RunBasicAsync(client, model, cancellationTokenSource.Token);
            break;
        case "stream":
            await RunStreamingAsync(client, model, cancellationTokenSource.Token);
            break;
        case "json":
            await RunStructuredOutputAsync(client, model, cancellationTokenSource.Token);
            break;
        case "tool":
            await RunFunctionToolAsync(client, model, cancellationTokenSource.Token);
            break;
        default:
            PrintCommandUsage();
            return 2;
    }
}
catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
{
    Console.Error.WriteLine("请求已取消。");
    return 130;
}

return 0;

static async Task RunBasicAsync(
    ResponsesClient client,
    string model,
    CancellationToken cancellationToken)
{
    CreateResponseOptions options = new(model, [
        ResponseItem.CreateDeveloperMessageItem("你是一个回答简洁的 .NET 助手。"),
        ResponseItem.CreateUserMessageItem("用三点介绍 OpenAI Responses API。")
    ])
    {
        StoredOutputEnabled = false,
        MaxOutputTokenCount = 500
    };

    ResponseResult response = (await client.CreateResponseAsync(options, cancellationToken)).Value;

    Console.WriteLine(response.GetOutputText());
    PrintUsage(response);
}

static async Task RunStreamingAsync(
    ResponsesClient client,
    string model,
    CancellationToken cancellationToken)
{
    CreateResponseOptions options = new(model, [
        ResponseItem.CreateUserMessageItem("写一段不超过 100 字的 C# async/await 最佳实践说明。")
    ])
    {
        StoredOutputEnabled = false,
        MaxOutputTokenCount = 300
    };

    await foreach (StreamingResponseUpdate update in client.CreateResponseStreamingAsync(options, cancellationToken))
    {
        if (update is StreamingResponseOutputTextDeltaUpdate textDelta)
        {
            Console.Write(textDelta.Delta);
        }
    }

    Console.WriteLine();
}

static async Task RunStructuredOutputAsync(
    ResponsesClient client,
    string model,
    CancellationToken cancellationToken)
{
    BinaryData schema = BinaryData.FromObjectAsJson(new
    {
        type = "object",
        properties = new
        {
            title = new { type = "string" },
            steps = new
            {
                type = "array",
                items = new { type = "string" }
            }
        },
        required = new[] { "title", "steps" },
        additionalProperties = false
    });

    CreateResponseOptions options = new(model, [
        ResponseItem.CreateUserMessageItem("给出三个创建 .NET 控制台项目的步骤。")
    ])
    {
        StoredOutputEnabled = false,
        TextOptions = new ResponseTextOptions
        {
            TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                "dotnet_console_guide",
                schema,
                "创建 .NET 控制台项目的简短指南。",
                jsonSchemaIsStrict: true)
        }
    };

    ResponseResult response = (await client.CreateResponseAsync(options, cancellationToken)).Value;
    Guide? guide = JsonSerializer.Deserialize<Guide>(response.GetOutputText());

    if (guide is null)
    {
        throw new InvalidOperationException("模型返回的结构化结果无法反序列化。");
    }

    Console.WriteLine(guide.Title);
    foreach (string step in guide.Steps)
    {
        Console.WriteLine($"- {step}");
    }
}

static async Task RunFunctionToolAsync(
    ResponsesClient client,
    string model,
    CancellationToken cancellationToken)
{
    FunctionTool weatherTool = new(
        "get_weather",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                city = new
                {
                    type = "string",
                    description = "城市名称"
                }
            },
            required = new[] { "city" },
            additionalProperties = false
        }),
        strictModeEnabled: true)
    {
        FunctionDescription = "获取指定城市的演示天气数据。"
    };

    CreateResponseOptions firstRequest = new(model, [
        ResponseItem.CreateUserMessageItem("北京今天天气如何？")
    ])
    {
        StoredOutputEnabled = false,
        ToolChoice = ResponseToolChoice.CreateAutoChoice()
    };
    firstRequest.Tools.Add(weatherTool);

    ResponseResult firstResponse = (await client.CreateResponseAsync(firstRequest, cancellationToken)).Value;
    FunctionCallResponseItem? functionCall = firstResponse.OutputItems
        .OfType<FunctionCallResponseItem>()
        .FirstOrDefault();

    if (functionCall is null)
    {
        Console.WriteLine(firstResponse.GetOutputText());
        return;
    }

    WeatherArguments? arguments = JsonSerializer.Deserialize<WeatherArguments>(
        functionCall.FunctionArguments.ToString());
    string city = arguments?.City ?? "未知城市";
    string toolResult = JsonSerializer.Serialize(new
    {
        city,
        temperatureCelsius = 24,
        condition = "晴",
        source = "本地演示数据"
    });

    List<ResponseItem> followUpItems = [.. firstResponse.OutputItems];
    followUpItems.Add(ResponseItem.CreateFunctionCallOutputItem(functionCall.CallId, toolResult));

    CreateResponseOptions secondRequest = new(model, followUpItems)
    {
        StoredOutputEnabled = false
    };
    secondRequest.Tools.Add(weatherTool);

    ResponseResult finalResponse = (await client.CreateResponseAsync(secondRequest, cancellationToken)).Value;
    Console.WriteLine(finalResponse.GetOutputText());
}

static void PrintUsage(ResponseResult response)
{
    Console.WriteLine();
    Console.WriteLine($"Response ID: {response.Id}");
    Console.WriteLine(
        $"Tokens: input={response.Usage.InputTokenCount}, " +
        $"output={response.Usage.OutputTokenCount}, total={response.Usage.TotalTokenCount}");
}

static void PrintCommandUsage()
{
    Console.WriteLine("用法: dotnet run -- [basic|stream|json|tool]");
    Console.WriteLine($"环境变量: {ApiKeyEnvironmentVariable}=必填, {ModelEnvironmentVariable}=可选（默认 {DefaultModel}）");
}

internal sealed record Guide(string Title, IReadOnlyList<string> Steps);

internal sealed record WeatherArguments(string City);
