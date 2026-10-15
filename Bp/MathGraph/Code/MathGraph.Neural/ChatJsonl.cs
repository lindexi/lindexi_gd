using System.Text.Json;

namespace MathGraphs.Neural;

public static class ChatJsonl
{
    /// <summary>加载基础 MiniMind conversations JSONL，生成 assistant 监督掩码；不关闭调用方流。</summary>
    public static Task<TokenDataset> LoadAsync(Stream stream, MiniMindTextTokenizer tokenizer,
        JsonlDatasetOptions? options = null, CancellationToken cancellationToken = default, IProgress<DatasetProgress>? progress = null) =>
        PretrainingJsonl.LoadCoreAsync(stream, tokenizer, options, true, cancellationToken, progress);

    /// <summary>从文件加载有界聊天数据；工具调用及未知消息字段明确拒绝。</summary>
    public static async Task<TokenDataset> LoadFileAsync(string path, MiniMindTextTokenizer tokenizer,
        JsonlDatasetOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await LoadAsync(stream, tokenizer, options, cancellationToken).ConfigureAwait(false);
    }

    internal static TokenDataset ParseSample(string text, MiniMindTextTokenizer tokenizer,
        JsonlDatasetOptions limits, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 32 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException();
        var rootNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
            if (!rootNames.Add(property.Name) || property.Name is "tools" or "tool_calls") throw new JsonException();
        if (!root.TryGetProperty("conversations", out var conversation) || conversation.ValueKind != JsonValueKind.Array)
            throw new JsonException();
        var messages = new List<SupervisedChatMessage>();
        foreach (var item in conversation.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.ValueKind != JsonValueKind.Object) throw new JsonException();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in item.EnumerateObject())
                if (!names.Add(property.Name) || property.Name is not ("role" or "content" or "reasoning_content"))
                    throw new JsonException();
            if (!item.TryGetProperty("role", out var role) || role.ValueKind != JsonValueKind.String ||
                !item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.String)
                throw new JsonException();
            string? reasoning = null;
            if (item.TryGetProperty("reasoning_content", out var value))
            {
                if (value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)) throw new JsonException();
                reasoning = value.GetString();
            }
            messages.Add(new SupervisedChatMessage(role.GetString() ?? throw new JsonException(),
                content.GetString() ?? throw new JsonException(), reasoning));
        }
        return tokenizer.CreateChatDataset(new[] { messages }, limits.MaxSampleTokens, limits.MaxTotalTokens, cancellationToken);
    }
}
