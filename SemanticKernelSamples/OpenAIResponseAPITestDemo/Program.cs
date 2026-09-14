#pragma warning disable OPENAI001

using OpenAI.Responses;

const string ApiKeyEnvironmentVariable = "OPENAI_API_KEY";
const string ModelEnvironmentVariable = "OPENAI_MODEL";
const string DefaultModel = "gpt-5-mini";

string? apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine($"请先设置环境变量 {ApiKeyEnvironmentVariable}。");
    return 1;
}

string model = Environment.GetEnvironmentVariable(ModelEnvironmentVariable) ?? DefaultModel;
using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

Console.WriteLine($"正在使用模型 {model} 测试 OpenAI Responses API...\n");

var client = new ResponsesClient(apiKey);

var firstOptions = new CreateResponseOptions
{
    Model = model,
    Instructions = "你是一名简洁、准确的中文技术助手。",
    StoredOutputEnabled = true,
    MaxOutputTokenCount = 300
};
firstOptions.InputItems.Add(ResponseItem.CreateUserMessageItem(
    "请用三点说明 OpenAI Responses API 相比 Chat Completions API 的主要特点。"));

ResponseResult firstResponse = (await client.CreateResponseAsync(
    firstOptions,
    cancellationTokenSource.Token)).Value;

Console.WriteLine("第一轮回答：");
Console.WriteLine(firstResponse.GetOutputText());
Console.WriteLine($"\nResponse ID: {firstResponse.Id}");
Console.WriteLine($"Status: {firstResponse.Status}");

ResponseResult secondResponse = (await client.CreateResponseAsync(
    model,
    "请把刚才的三点压缩成一句话。",
    firstResponse.Id,
    cancellationTokenSource.Token)).Value;

Console.WriteLine("\n第二轮回答（通过 previous_response_id 延续上下文）：");
Console.WriteLine(secondResponse.GetOutputText());

return 0;
