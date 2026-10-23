#pragma warning disable OPENAI001, SCME0001 // 使用 SDK 原生 Responses 及 JSON 扩展能力。

using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace AgentLib.Tools;

/// <summary>
/// 将已有函数工具接入 Responses，不负责工具注册、审批、历史管理或执行循环。
/// </summary>
public static class ResponsesToolHelper
{
    /// <summary>
    /// 从函数现有元数据创建 Responses 工具定义，保留原始参数 Schema。
    /// </summary>
    public static FunctionTool CreateTool(AIFunction function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return ResponseTool.CreateFunctionTool(function.Name,
            BinaryData.FromString(function.JsonSchema.GetRawText()), false, function.Description);
    }

    /// <summary>
    /// 使用模型提供的参数调用原函数，转换实际返回值；取消和异常直接传播。
    /// </summary>
    public static async Task<FunctionCallOutputResponseItem> InvokeAsync(
        AIFunction function, FunctionCallResponseItem call, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(call);
        cancellationToken.ThrowIfCancellationRequested();
        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(call.FunctionArguments.ToString());
        ArgumentNullException.ThrowIfNull(arguments);
        object? result = await function.InvokeAsync(new AIFunctionArguments(
            arguments.ToDictionary(pair => pair.Key, pair => (object?)pair.Value)), cancellationToken).ConfigureAwait(false);
        return CreateOutput(call.CallId, result, function.JsonSerializerOptions);
    }

    /// <summary>
    /// 转换工具结果。图片使用原生 input_image 内容，不将图片对象序列化成文本。
    /// </summary>
    public static FunctionCallOutputResponseItem CreateOutput(string callId, object? result,
        JsonSerializerOptions? serializerOptions = null)
    {
        if (result is DataContent image && image.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            var output = ResponseItem.CreateFunctionCallOutputItem(callId, string.Empty);
            // 2.14.0 的强类型 FunctionOutput 仍为字符串；Patch 是 SDK 提供的原生 JSON 扩展入口。
            output.Patch.Set("$.output"u8, BinaryData.FromObjectAsJson(new[]
            {
                new { type = "input_image", image_url = image.Uri.ToString() }
            }));
            return output;
        }
        string text = result switch
        {
            string value => value,
            TextContent value => value.Text,
            _ => JsonSerializer.Serialize(result, serializerOptions),
        };
        return ResponseItem.CreateFunctionCallOutputItem(callId, text);
    }
}
