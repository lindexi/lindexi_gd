using System.Globalization;
using System.Resources;
using System.Text.Json;
using System.Text.Json.Serialization;
using MathGraphs.Neural;

namespace FractalForge.Training;

/// <summary>不可变语料声明；Sha256 是原始文件内容身份，Identity 是预登记划分身份。</summary>
public sealed record ForgeCorpus(string Path, string Identity, string Sha256, int? ContentRole = null);

/// <summary>版本化单动作配对运行配置；所有训练和接受规则均由方向显式提供，不推断最优值。</summary>
public sealed record ForgeRunConfiguration
{
    public required string Semantics { get; init; }
    public required string Direction { get; init; }
    public required string RunId { get; init; }
    public required string OutputRoot { get; init; }
    public required string SourceArchive { get; init; }
    public required string SourceSha256 { get; init; }
    public required string TokenizerPath { get; init; }
    public required string TokenizerConfigPath { get; init; }
    public required string TokenizerIdentity { get; init; }
    public required ForgeCorpus Training { get; init; }
    public required ForgeCorpus Acceptance { get; init; }
    public required ForgeCorpus FinalTest { get; init; }
    public required JsonlDatasetOptions DataLimits { get; init; }
    public required ForgeAction Action { get; init; }
    public required string RuleVersion { get; init; }
    public required int MaxUpdates { get; init; }
    public required long MaxTokens { get; init; }
    public required long MaxPairTapeCells { get; init; }
    public required double MinimumGain { get; init; }
    public required AdamWOptions Optimizer { get; init; }
    public required ulong RandomState { get; init; }

    /// <summary>读取严格 JSON；拒绝额外/重复属性；相对文件路径相对于进程工作目录。</summary>
    public static async Task<ForgeRunConfiguration> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var bytes = await ForgeRunner.ReadBoundedAsync(path, 1048576, cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(bytes);
        Check(document.RootElement);
        return JsonSerializer.Deserialize<ForgeRunConfiguration>(bytes, Json) ?? throw RunErrors.Invalid();
        static void Check(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw RunErrors.Invalid();
                    Check(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
                foreach (var item in element.EnumerateArray()) Check(item);
        }
    }

    internal static readonly JsonSerializerOptions Json = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        MaxDepth = 64
    };

    internal string Fingerprint => ForgeArchive.Fingerprint(JsonSerializer.SerializeToUtf8Bytes(this, Json));
    internal string DirectoryPath => System.IO.Path.GetFullPath(System.IO.Path.Combine(OutputRoot, Direction, RunId));
    internal TrialProtocol Protocol => new(Direction + "/" + RunId, Training.Identity, Acceptance.Identity, RuleVersion,
        MaxUpdates, MaxTokens, MaxPairTapeCells, MinimumGain, Optimizer);

    internal void Validate(string expectedDirection)
    {
        if (Semantics != ForgeRunner.Semantics || Direction != expectedDirection || !Segment(Direction) || !Segment(RunId) ||
            string.IsNullOrWhiteSpace(OutputRoot) || string.IsNullOrWhiteSpace(SourceArchive) || !Hash(SourceSha256) ||
            string.IsNullOrWhiteSpace(TokenizerPath) || string.IsNullOrWhiteSpace(TokenizerConfigPath) || string.IsNullOrWhiteSpace(TokenizerIdentity) ||
            Training is null || Acceptance is null || FinalTest is null || Action is null || Optimizer is null || DataLimits is null ||
            string.IsNullOrWhiteSpace(RuleVersion) || MaxUpdates <= 0 || MaxTokens <= 0 || MaxPairTapeCells <= 0 || !double.IsFinite(MinimumGain))
            throw RunErrors.Invalid();
        var corpora = new[] { Training, Acceptance, FinalTest };
        if (corpora.Any(c => string.IsNullOrWhiteSpace(c.Path) || string.IsNullOrWhiteSpace(c.Identity) || !Hash(c.Sha256)) ||
            corpora.Select(c => c.Identity).Distinct(StringComparer.Ordinal).Count() != 3 ||
            corpora.Select(c => c.Sha256).Distinct(StringComparer.Ordinal).Count() != 3 ||
            corpora.Select(c => System.IO.Path.GetFullPath(c.Path)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3 ||
            DataLimits.MaxLineBytes <= 0 || DataLimits.MaxFileBytes <= 0 || DataLimits.MaxSamples <= 0 ||
            DataLimits.MaxSampleTokens < 2 || DataLimits.MaxTotalTokens < 2)
            throw RunErrors.Invalid();
        // Validate optimizer even when an invocation performs no updates.
        if (!double.IsFinite(Optimizer.LearningRate) || Optimizer.LearningRate <= 0 ||
            !double.IsFinite(Optimizer.Beta1) || Optimizer.Beta1 < 0 || Optimizer.Beta1 >= 1 ||
            !double.IsFinite(Optimizer.Beta2) || Optimizer.Beta2 < 0 || Optimizer.Beta2 >= 1 ||
            !double.IsFinite(Optimizer.Epsilon) || Optimizer.Epsilon <= 0 ||
            !double.IsFinite(Optimizer.WeightDecay) || Optimizer.WeightDecay < 0) throw RunErrors.Invalid();
    }

    private static bool Segment(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 80 &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_');
    internal static bool Hash(string? value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F');
}

internal static class RunErrors
{
    private static readonly ResourceManager Resources = new("FractalForge.Training.RunMessages", typeof(RunErrors).Assembly);
    internal static string Message(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    internal static InvalidDataException Invalid() => new(Message("Invalid"));
}
