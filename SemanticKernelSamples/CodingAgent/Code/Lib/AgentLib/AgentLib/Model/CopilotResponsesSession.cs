using System.Collections.ObjectModel;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace AgentLib.Model;

/// <summary>
/// 持续维护一个会话的原生 Responses 历史，不从展示消息重建上下文。
/// </summary>
public sealed class CopilotResponsesSession
{
    /// <summary>创建尚无原生历史的 Responses 会话。</summary>
    public CopilotResponsesSession()
    {
    }

    private readonly List<ResponseItem> _items = new();

    /// <summary>获取有序原生历史；调用方不得修改已提交的 SDK 对象。</summary>
    public IReadOnlyList<ResponseItem> Items => _items;

    /// <summary>追加本次运行的原生输入或真实工具输出，不转换既有历史。</summary>
    public void AppendItem(ResponseItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items.Add(item);
    }

    /// <summary>用服务端压缩返回的完整原生输出替换请求历史。</summary>
    public void ApplyCompaction(IReadOnlyList<ResponseItem> output)
    {
        ArgumentNullException.ThrowIfNull(output);
        _items.Clear();
        _items.AddRange(output);
    }

    /// <summary>按服务返回顺序追加响应的原生输出。</summary>
    public void AppendResponse(ResponseResult response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _items.AddRange(response.OutputItems);
    }
}
