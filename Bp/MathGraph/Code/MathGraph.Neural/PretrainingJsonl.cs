using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MathGraphs.Neural;

/// <summary>JSONL 预训练加载预算；行长按 UTF-8 字节计，样本截断后追加 EOS。</summary>
public sealed record JsonlDatasetOptions(int MaxLineBytes = 131072, long MaxFileBytes = 67108864,
    int MaxSamples = 10000, int MaxSampleTokens = 1024, int MaxTotalTokens = 1000000);

public static class PretrainingJsonl
{
    /// <summary>读取 MiniMind 的每行 {text:string} 格式；空行跳过，错误附行号，调用方保留流所有权。</summary>
    public static async Task<TokenDataset> LoadAsync(Stream stream, MiniMindTextTokenizer tokenizer,
        JsonlDatasetOptions? options = null, CancellationToken cancellationToken = default, IProgress<DatasetProgress>? progress = null) =>
        await LoadCoreAsync(stream, tokenizer, options, false, cancellationToken, progress).ConfigureAwait(false);

    internal static async Task<TokenDataset> LoadCoreAsync(Stream stream, MiniMindTextTokenizer tokenizer,
        JsonlDatasetOptions? options, bool chat, CancellationToken cancellationToken, IProgress<DatasetProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(tokenizer);
        if (!stream.CanRead) throw new ArgumentException(nameof(stream));
        var limits = options ?? new JsonlDatasetOptions();
        if (limits.MaxLineBytes <= 0 || limits.MaxFileBytes <= 0 || limits.MaxSamples <= 0 ||
            limits.MaxSampleTokens < 2 || limits.MaxTotalTokens < 2)
            throw new ArgumentOutOfRangeException(nameof(options));
        cancellationToken.ThrowIfCancellationRequested();
        var samples = new List<IReadOnlyList<int>>();
        var masks = new List<IReadOnlyList<bool>>();
        using var line = new MemoryStream();
        var buffer = new byte[8192];
        var encoding = new UTF8Encoding(false, true);
        long totalBytes = 0;
        long lineNumber = 1;
        var totalTokens = 0;
        long consumed = 0;
        long? totalLength = stream.CanSeek ? stream.Length - stream.Position : null;
        var lastReport = System.Diagnostics.Stopwatch.GetTimestamp();
        Report(DatasetPreparationStage.Tokenizing);
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (read > limits.MaxFileBytes - totalBytes) throw Error("JsonlBudget");
            totalBytes += read;
            for (var i = 0; i < read; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                consumed++;
                if (buffer[i] == (byte)'\n')
                {
                    ProcessLine();
                    if (System.Diagnostics.Stopwatch.GetElapsedTime(lastReport).TotalSeconds >= 2)
                    {
                        Report(DatasetPreparationStage.Tokenizing);
                        lastReport = System.Diagnostics.Stopwatch.GetTimestamp();
                    }
                    line.SetLength(0);
                    lineNumber++;
                }
                else
                {
                    if (line.Length >= limits.MaxLineBytes) throw Error("JsonlBudget");
                    line.WriteByte(buffer[i]);
                }
            }
        }
        if (line.Length > 0) ProcessLine();
        cancellationToken.ThrowIfCancellationRequested();
        if (samples.Count == 0) throw Error("JsonlInvalid");
        Report(DatasetPreparationStage.BuildingDataset);
        var dataset = new TokenDataset(tokenizer.Identity, samples, limits.MaxTotalTokens, chat ? masks : null, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        Report(DatasetPreparationStage.Completed);
        return dataset;

        void Report(DatasetPreparationStage stage) => progress?.Report(new DatasetProgress(stage, consumed, totalLength, samples.Count, totalTokens));

        void ProcessLine()
        {
            string text;
            try { text = encoding.GetString(line.GetBuffer(), 0, (int)line.Length); }
            catch (DecoderFallbackException error) { throw Error("JsonlInvalid", error); }
            if (lineNumber == 1) text = text.TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(text)) return;
            if (samples.Count >= limits.MaxSamples) throw Error("JsonlBudget");
            if (chat)
            {
                try
                {
                    var sample = ChatJsonl.ParseSample(text, tokenizer, limits, cancellationToken);
                    if (sample[0].Count > limits.MaxTotalTokens - totalTokens) throw Error("JsonlBudget");
                    samples.Add(sample[0]);
                    masks.Add(sample.GetLabelMask(0) ?? throw Error("ChatJsonlInvalid"));
                    totalTokens += sample[0].Count;
                }
                catch (JsonException error) { throw Error("ChatJsonlInvalid", error); }
                catch (ArgumentOutOfRangeException error) { throw Error("JsonlBudget", error); }
                catch (ArgumentException error) { throw Error("ChatJsonlInvalid", error); }
                catch (InvalidOperationException error) { throw Error("ChatJsonlInvalid", error); }
                return;
            }
            string content;
            try
            {
                using var json = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 32 });
                if (json.RootElement.ValueKind != JsonValueKind.Object) throw Error("JsonlInvalid");
                var properties = json.RootElement.EnumerateObject().Where(p => p.NameEquals("text")).ToArray();
                if (properties.Length != 1 || properties[0].Value.ValueKind != JsonValueKind.String)
                    throw Error("JsonlInvalid");
                content = properties[0].Value.GetString() ?? throw Error("JsonlInvalid");
            }
            catch (JsonException error) { throw Error("JsonlInvalid", error); }
            IReadOnlyList<int> tokens;
            try { tokens = tokenizer.Encode(content, limits.MaxSampleTokens - 1); }
            catch (ArgumentOutOfRangeException error) { throw Error("JsonlBudget", error); }
            if (tokens.Count == 0) throw Error("JsonlInvalid");
            if (tokens.Count >= limits.MaxTotalTokens - totalTokens) throw Error("JsonlBudget");
            samples.Add(tokens.Append(tokenizer.EosTokenId).ToArray());
            totalTokens += tokens.Count + 1;
        }

        InvalidDataException Error(string key, Exception? inner = null) => new(
            string.Format(CultureInfo.CurrentCulture, Numeric.Message(chat && key == "JsonlInvalid" ? "ChatJsonlInvalid" : key), lineNumber), inner);
    }

    /// <summary>从文件读取有界 JSONL 数据集；本方法拥有并释放文件流。</summary>
    public static async Task<TokenDataset> LoadFileAsync(string path, MiniMindTextTokenizer tokenizer,
        JsonlDatasetOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await LoadAsync(stream, tokenizer, options, cancellationToken).ConfigureAwait(false);
    }
}
