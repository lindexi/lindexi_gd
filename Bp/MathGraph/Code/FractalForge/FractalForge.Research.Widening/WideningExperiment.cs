using System.Resources;
using System.Text.Json;
using System.Text.Json.Serialization;
using FractalForge.Core;
using FractalForge.Training;
using MathGraphs.Neural;

namespace FractalForge.Research.Widening;

/// <summary>方向版本及分支与公共运行配置一起固定；W0 不应用 Run.Action。</summary>
public sealed record WideningExperiment(string Semantics, string Branch, ForgeRunConfiguration Run)
{
    public const string Version = "fractalforge.widening.v1";
}

/// <summary>准备时显式选择已有源档案或四点初始化；不修改已有文本绑定。</summary>
public sealed record WideningPreparation(ForgeRunConfiguration Run, string? SourceArchive, int Seed, bool Feedback,
    string Target, double UnequalAlpha);

internal static class WideningJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        WriteIndented = true
    };
    internal static string Message(string key) => new ResourceManager("FractalForge.Research.Widening.Messages", typeof(WideningExperiment).Assembly).GetString(key) ?? key;
    internal static InvalidDataException Invalid() => new(Message("Invalid"));
    internal static T Parse<T>(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        Check(document.RootElement);
        return JsonSerializer.Deserialize<T>(bytes, Options) ?? throw Invalid();
        static void Check(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw Invalid();
                    Check(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
                foreach (var item in element.EnumerateArray()) Check(item);
        }
    }
}

/// <summary>独立克隆策略与合法四点源模型准备。</summary>
public static class WideningStrategy
{
    /// <summary>返回独立快照或公共克隆候选；W3 因公共配对事务无自定义候选能力明确拒绝。</summary>
    public static ForgeModel CreateCandidate(ForgeModel source, string branch, string target, double alpha = .3,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();
        return branch switch
        {
            "W0" => ForgeArchive.Import(ForgeArchive.Export(source), source.Network.Budget),
            "W1" => source.Apply(new("clone-widen", target, Alpha: .5), cancellationToken),
            "W2" when double.IsFinite(alpha) && alpha > 0 && alpha < 1 && alpha != .5 =>
                source.Apply(new("clone-widen", target, Alpha: alpha), cancellationToken),
            "W3" => throw new NotSupportedException(WideningJson.Message("W3Unavailable")),
            _ => throw WideningJson.Invalid()
        };
    }

    /// <summary>读取真实词表，绑定四点源或验证已有绑定，保存源档案及 W0/W1/W2 版本化配置；不读取最终测试集。</summary>
    public static async Task PrepareAsync(WideningPreparation preparation, string directory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var run = preparation.Run;
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(run.TokenizerPath, run.TokenizerConfigPath,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var source = preparation.SourceArchive is null
            ? ForgeModel.Create(ForgeTopology.CreateDiamond(preparation.Seed, preparation.Feedback))
            : ForgeArchive.Import(await File.ReadAllBytesAsync(preparation.SourceArchive, cancellationToken).ConfigureAwait(false));
        if (source.Network.TextBindings is null)
            source.Network.BindText(new TokenBindings(tokenizer.Identity, ["I"], ["O"],
                Enumerable.Range(0, tokenizer.VocabularySize).Select(i => (IReadOnlyList<double>)new[] { (double)i / tokenizer.VocabularySize }).ToArray(), seed: preparation.Seed));
        var bindings = source.Network.TextBindings ?? throw WideningJson.Invalid();
        if (bindings.TokenizerIdentity != tokenizer.Identity || bindings.VocabularySize != tokenizer.VocabularySize)
            throw WideningJson.Invalid();
        // Validate both actual layouts before publishing any experiment.
        _ = CreateCandidate(source, "W1", preparation.Target, cancellationToken: cancellationToken);
        _ = CreateCandidate(source, "W2", preparation.Target, preparation.UnequalAlpha, cancellationToken);
        Directory.CreateDirectory(directory);
        var bytes = ForgeArchive.Export(source);
        var sourcePath = Path.GetFullPath(Path.Combine(directory, ForgeArchive.Fingerprint(bytes) + ".forge"));
        await SaveNewAsync(sourcePath, bytes, cancellationToken).ConfigureAwait(false);
        foreach (var branch in new[] { "W0", "W1", "W2" })
        {
            cancellationToken.ThrowIfCancellationRequested();
            var configuration = run with
            {
                Semantics = ForgeRunner.Semantics, Direction = "Widening", RunId = run.RunId + "-" + branch,
                SourceArchive = sourcePath, SourceSha256 = ForgeArchive.Fingerprint(bytes), TokenizerIdentity = tokenizer.Identity,
                TokenizerPath = Path.GetFullPath(run.TokenizerPath), TokenizerConfigPath = Path.GetFullPath(run.TokenizerConfigPath),
                OutputRoot = Path.GetFullPath(run.OutputRoot),
                Training = run.Training with { Path = Path.GetFullPath(run.Training.Path) },
                Acceptance = run.Acceptance with { Path = Path.GetFullPath(run.Acceptance.Path) },
                FinalTest = run.FinalTest with { Path = Path.GetFullPath(run.FinalTest.Path) },
                Action = new("clone-widen", preparation.Target, Alpha: branch == "W2" ? preparation.UnequalAlpha : .5)
            };
            await SaveNewAsync(Path.Combine(directory, branch + ".json"), JsonSerializer.SerializeToUtf8Bytes(
                new WideningExperiment(WideningExperiment.Version, branch, configuration), WideningJson.Options), cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task SaveNewAsync(string path, byte[] bytes, CancellationToken ct)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous);
        await stream.WriteAsync(bytes, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }
}
