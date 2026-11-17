using System.Resources;
using System.Text.Json;
using FractalForge.Core;
using FractalForge.Training;
using MathGraphs.Neural;

namespace FractalForge.Research.Feedback;

public enum FeedbackTopology { Local, CrossLayer }
public enum FeedbackStateRule { F1ZeroReset, F2EquivalentRestore, F3SeenPrefix }

/// <summary>共同前缀重放结果；成本是两侧合计的内部步数，不含后续生成。</summary>
public sealed record FeedbackWarmup(GenerationSession Keep, GenerationSession Candidate, long ReplayInternalSteps);

/// <summary>方向专有控制变量和受限状态接续；不复用任何旧执行布局状态。</summary>
public static class FeedbackExperiment
{
    private static readonly ResourceManager Resources = new("FractalForge.Research.Feedback.Messages", typeof(FeedbackExperiment).Assembly);
    internal static string Message(string key) => Resources.GetString(key) ?? throw new InvalidOperationException(key);

    /// <summary>创建固定四点反馈基线；输入编码和读出绑定完全由调用方给定。</summary>
    public static ForgeModel CreateBaseline(TokenBindings bindings, int seed, bool feedback = true)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        var model = ForgeModel.Create(ForgeTopology.CreateDiamond(seed, feedback));
        model.Network.BindText(bindings);
        return model;
    }

    /// <summary>局部为 A 自反馈，跨层为 O→I；其他计算、读出和已有反馈保持不变。</summary>
    public static ForgeAction CreateAction(FeedbackTopology topology, int delay, double weight)
    {
        if (!Enum.IsDefined(topology) || delay <= 0 || !double.IsFinite(weight))
            throw new ArgumentException(Message("InvalidAction"));
        return topology == FeedbackTopology.Local
            ? new("add-feedback", "A", Destination: "A", Delay: delay, Weight: weight)
            : new("add-feedback", "O", Destination: "I", Delay: delay, Weight: weight);
    }

    /// <summary>F1 受限全零重置：创建全新执行器并消费当前提示，不保留原会话的任何历史。</summary>
    public static GenerationSession StartZero(ForgeModel model, IReadOnlyList<int> prompt, int maxNewTokens,
        ulong seed = 1, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.Network.CreateTokenExecutor(cancellationToken).CreateGenerationSession(prompt, maxNewTokens,
            new SamplingOptions(DoSample: false), seed, cancellationToken: cancellationToken);
    }

    /// <summary>F2 只接受档案完全相同的模型；共享层随后再次验证会话中的模型指纹和时钟。</summary>
    public static GenerationSession RestoreEquivalent(ForgeModel source, ForgeModel destination, byte[] checkpoint)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (ForgeArchive.Fingerprint(ForgeArchive.Export(source)) != ForgeArchive.Fingerprint(ForgeArchive.Export(destination)))
            throw new NotSupportedException(Message("MigrationUnavailable"));
        return destination.Network.CreateTokenExecutor().RestoreGenerationSession(checkpoint);
    }

    /// <summary>F3 从同一已见序列的零到预测位置之前重放；预测位置及之后的 token 不传给执行器。</summary>
    public static FeedbackWarmup WarmSeenPrefix(ForgeModel keep, ForgeModel candidate, IReadOnlyList<int> seenTokens,
        int predictionPosition, int maxNewTokens, ulong seed = 1, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keep);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(seenTokens);
        if (predictionPosition <= 0 || predictionPosition > seenTokens.Count)
            throw new ArgumentOutOfRangeException(nameof(predictionPosition), Message("InvalidPrefix"));
        var a = keep.Network.TextBindings;
        var b = candidate.Network.TextBindings;
        if (a is null || b is null || a.TokenizerIdentity != b.TokenizerIdentity || a.VocabularySize != b.VocabularySize || a.InternalSteps != b.InternalSteps || InputDefinition(keep) != InputDefinition(candidate))
            throw new InvalidOperationException(Message("BindingsMismatch"));
        var prefix = seenTokens.Take(predictionPosition).ToArray();
        var first = StartZero(keep, prefix, maxNewTokens, seed, cancellationToken);
        var second = StartZero(candidate, prefix, maxNewTokens, seed, cancellationToken);
        return new(first, second, checked(2L * prefix.Length * a.InternalSteps));
    }

    private static string InputDefinition(ForgeModel model)
    {
        using var document = JsonDocument.Parse(model.Network.ExportCheckpoint());
        var text = document.RootElement.GetProperty("Text");
        return string.Join("|", new[] { "Identity", "Inputs", "Readouts", "Encoding", "Steps" }
            .Select(name => text.GetProperty(name).GetRawText()));
    }

    /// <summary>验证公共 runner 的样本重置路径；F2/F3 不得冒充已集成的训练状态迁移。</summary>
    public static void ValidateConfiguration(ForgeRunConfiguration configuration, FeedbackStateRule rule = FeedbackStateRule.F1ZeroReset)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (rule != FeedbackStateRule.F1ZeroReset) throw new NotSupportedException(Message("MigrationUnavailable"));
        var action = configuration.Action;
        if (configuration.Direction != "Feedback" || action is null || action.Kind != "add-feedback" ||
            action.Delay <= 0 || !double.IsFinite(action.Weight) || string.IsNullOrWhiteSpace(action.Target) || string.IsNullOrWhiteSpace(action.Destination))
            throw new ArgumentException(Message("InvalidAction"));
    }
}
