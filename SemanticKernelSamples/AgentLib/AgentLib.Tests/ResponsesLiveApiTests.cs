#pragma warning disable OPENAI001

using System.Text.Json;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using AgentLib.Model;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace AgentLib.Tests;

/// <summary>
/// 可选的真实 Responses 服务用法测试；密钥文件缺失时正常返回，不访问网络。
/// </summary>
[TestClass]
[TestCategory("LiveApi")]
[DoNotParallelize]
public sealed class ResponsesLiveApiTests
{
    private const string KeyFilePath = @"C:\lindexi\Work\Key\deepseek.md";

    [TestMethod]
    public async Task Conversation_UsesSystemInstructionsAsync()
    {
        if (!File.Exists(KeyFilePath)) return;

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var manager = await CreateManagerAsync(cancellation.Token);
        using var httpClient = manager.AgentApiEndpointManager.HttpClient;
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        context.UserChatMessage.AppendText("Reply with the verification code specified in your instructions.");
        await context.AppendMessagesToSessionAsync();
        string verificationCode = Guid.NewGuid().ToString("N");
        var request = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            Instructions = $"Reply with exactly this verification code and nothing else: {verificationCode}",
            MaxOutputTokenCount = 1024,
        };
        request.InputItems.Add(ResponseItem.CreateUserMessageItem(context.UserChatMessage.Content));

        using (context.StartChatting())
        {
            var response = (await client.CreateResponseAsync(request, cancellation.Token)).Value;
            context.AssistantChatMessage.ResponseInfo.AppendResponse(response);
        }

        Assert.AreEqual(verificationCode, context.AssistantChatMessage.Content.Trim());
    }

    [TestMethod]
    public async Task LocalTool_ExecutesAndContinuesResponseAsync()
    {
        if (!File.Exists(KeyFilePath)) return;

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var manager = await CreateManagerAsync(cancellation.Token);
        using var httpClient = manager.AgentApiEndpointManager.HttpClient;
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        var invocations = new List<string>();
        string toolValue = Guid.NewGuid().ToString("N");
        var function = AIFunctionFactory.Create((string name) =>
        {
            invocations.Add(name);
            return toolValue;
        }, "lookup_verification_code");
        var tools = new Dictionary<string, AIFunction> { [function.Name] = function };
        context.UserChatMessage.AppendText("Look up the verification code for sample using the local tool, then return only the code.");
        await context.AppendMessagesToSessionAsync();
        const string instructions = "Use the provided tool to look up the code for name sample. After receiving its result, reply with only the returned code, without quotes or formatting.";
        var request = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            Instructions = instructions,
            StoredOutputEnabled = true,
            ParallelToolCallsEnabled = false,
            MaxOutputTokenCount = 1024,
        };
        request.Tools.Add(ResponseTool.CreateFunctionTool(function.Name,
            BinaryData.FromString(function.JsonSchema.GetRawText()), false));
        request.InputItems.Add(ResponseItem.CreateUserMessageItem(context.UserChatMessage.Content));
        var info = context.AssistantChatMessage.ResponseInfo;

        using (context.StartChatting())
        {
            var first = (await client.CreateResponseAsync(request, cancellation.Token)).Value;
            info.AppendResponse(first);
            var call = first.OutputItems.OfType<FunctionCallResponseItem>().Single();
            var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(call.FunctionArguments.ToString());
            Assert.IsNotNull(arguments);
            var result = await tools[call.FunctionName].InvokeAsync(new AIFunctionArguments(
                arguments.ToDictionary(pair => pair.Key, pair => (object?)pair.Value)), cancellation.Token);
            var output = ResponseItem.CreateFunctionCallOutputItem(call.CallId, JsonSerializer.Serialize(result));
            info.AppendToolResult(output);
            var continuation = new CreateResponseOptions
            {
                Model = request.Model,
                Instructions = instructions,
                PreviousResponseId = first.Id,
                StoredOutputEnabled = true,
                MaxOutputTokenCount = 1024,
            };
            continuation.InputItems.Add(output);
            var second = (await client.CreateResponseAsync(continuation, cancellation.Token)).Value;
            info.AppendResponse(second);

            Assert.AreEqual(first.Id, second.PreviousResponseId);
            Assert.IsNotNull(first.Usage);
            Assert.IsNotNull(second.Usage);
            Assert.AreEqual((long)first.Usage.TotalTokenCount + second.Usage.TotalTokenCount,
                context.AssistantChatMessage.TotalUsageDetails?.TotalTokenCount);
            Assert.AreEqual((long)second.Usage.TotalTokenCount,
                context.AssistantChatMessage.CurrentUsageDetails?.TotalTokenCount);
        }

        CollectionAssert.AreEqual(new[] { "sample" }, invocations);
        var displayedTool = context.AssistantChatMessage.MessageItems.OfType<CopilotChatToolItem>().Single();
        Assert.AreEqual(JsonSerializer.Serialize(toolValue), displayedTool.OutputText);
        Assert.AreEqual(toolValue, context.AssistantChatMessage.Content.Trim());
    }

    private static async Task<CopilotChatManager> CreateManagerAsync(CancellationToken cancellationToken)
    {
        string key = (await File.ReadAllTextAsync(KeyFilePath, cancellationToken)).Trim();
        Assert.IsFalse(string.IsNullOrWhiteSpace(key));
        string json = JsonSerializer.Serialize(new
        {
            PrimaryModel = "deepseek-flash",
            OpenAIConfigurationList = new[]
            {
                new
                {
                    EndPoint = "https://api.deepseek.com",
                    Key = key,
                    ModelDefinitions = new[]
                    {
                        new { Provider = "deepseek", ModelName = "deepseek-flash", ModelId = "deepseek-flash" }
                    }
                }
            }
        });
        var manager = new CopilotChatManager(new EmptyCopilotChatLogger());
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString(json));
        return manager;
    }
}
