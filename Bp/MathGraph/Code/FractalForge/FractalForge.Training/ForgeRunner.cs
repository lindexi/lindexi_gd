using System.Text.Json;
using MathGraphs.Neural;

namespace FractalForge.Training;

/// <summary>公共单动作运行器；方向传入固定名称，入口负责将 Ctrl+C 转为 CancellationToken。</summary>
public static class ForgeRunner
{
    public const string Semantics = "fractalforge.console-run.v1";
    private const string CursorSemantics = "sequential-complete-samples.zero-history.random-unchanged.v1";
    private sealed record Manifest(string Semantics, string ConfigurationHash, string Direction, string RunId,
        string SourceHash, string CursorSemantics, string TrialFile, string TrialHash, TrialProgress Progress);

    /// <summary>执行 train/resume/inspect/evaluate/generate；参数为命令、配置路径，其后 resume/inspect/evaluate/generate 必须显式指定清单。
    /// train/resume 可追加本次更新上限；generate 再追加提示词、新 token 上限和种子。返回 0 成功、1 运行错误、2 参数错误、130 取消。</summary>
    public static async Task<int> RunAsync(string expectedDirection, IReadOnlyList<string> arguments,
        TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (arguments.Count < 2 || arguments[0] is not ("train" or "resume" or "inspect" or "evaluate" or "generate"))
                throw new ArgumentException(RunErrors.Message("Usage"));
            var command = arguments[0];
            var expectedCount = command switch { "train" => 2, "generate" => 6, _ => 3 };
            if (arguments.Count != expectedCount && !(command is "train" or "resume" && arguments.Count == expectedCount + 1))
                throw new ArgumentException(RunErrors.Message("Usage"));
            var configuration = await ForgeRunConfiguration.LoadAsync(arguments[1], cancellationToken).ConfigureAwait(false);
            configuration.Validate(expectedDirection);
            ForgeCorpusPreparation.ValidateUsage(configuration);
            var maxUpdates = int.MaxValue;
            if (command is "train" or "resume" && arguments.Count == expectedCount + 1 &&
                (!int.TryParse(arguments[^1], out maxUpdates) || maxUpdates <= 0)) throw new ArgumentException(RunErrors.Message("Usage"));
            Directory.CreateDirectory(configuration.DirectoryPath);
            using var lease = new FileStream(Path.Combine(configuration.DirectoryPath, "writer.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var tokenizer = await MiniMindTextTokenizer.LoadAsync(configuration.TokenizerPath, configuration.TokenizerConfigPath,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (tokenizer.Identity != configuration.TokenizerIdentity) throw RunErrors.Invalid();
            using var trainingLease = new FileStream(configuration.Training.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var acceptanceLease = new FileStream(configuration.Acceptance.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            await ForgeJsonlStream.VerifyAsync(configuration.Training, configuration.DataLimits.MaxFileBytes, cancellationToken).ConfigureAwait(false);
            await ForgeJsonlStream.VerifyAsync(configuration.Acceptance, configuration.DataLimits.MaxFileBytes, cancellationToken).ConfigureAwait(false);
            var source = await ReadBoundedAsync(configuration.SourceArchive, 67108864, cancellationToken).ConfigureAwait(false);
            if (ForgeArchive.Fingerprint(source) != configuration.SourceSha256) throw RunErrors.Invalid();
            var manifestPath = Path.Combine(configuration.DirectoryPath, "run.json");
            PairedTrial trial;
            if (command == "train")
            {
                if (File.Exists(manifestPath)) throw RunErrors.Invalid();
                var model = ForgeArchive.Import(source);
                ValidateBindings(model, tokenizer);
                if (configuration.DataLimits.MaxSampleTokens > model.Network.Budget.MaxSteps) throw RunErrors.Invalid();
                trial = PairedTrial.Begin(model, configuration.Action, configuration.Protocol, randomState: configuration.RandomState);
                Save(configuration, trial, manifestPath);
            }
            else
            {
                manifestPath = Path.GetFullPath(arguments[2]);
                if (!string.Equals(Path.GetDirectoryName(manifestPath), configuration.DirectoryPath, StringComparison.OrdinalIgnoreCase))
                    throw RunErrors.Invalid();
                var bytes = await ReadBoundedAsync(manifestPath, 1048576, cancellationToken).ConfigureAwait(false);
                var manifest = JsonSerializer.Deserialize<Manifest>(bytes, ForgeRunConfiguration.Json) ?? throw RunErrors.Invalid();
                if (manifest.Semantics != Semantics || manifest.ConfigurationHash != configuration.Fingerprint ||
                    manifest.Direction != expectedDirection || manifest.RunId != configuration.RunId || manifest.SourceHash != configuration.SourceSha256 ||
                    manifest.CursorSemantics != CursorSemantics || !ForgeRunConfiguration.Hash(manifest.TrialHash) ||
                    manifest.TrialFile != manifest.TrialHash + ".trial.json") throw RunErrors.Invalid();
                var trialPath = Path.Combine(configuration.DirectoryPath, manifest.TrialFile);
                var trialBytes = await ReadBoundedAsync(trialPath, 1048576, cancellationToken).ConfigureAwait(false);
                if (ForgeArchive.Fingerprint(trialBytes) != manifest.TrialHash) throw RunErrors.Invalid();
                trial = PairedTrial.Load(trialPath, configuration.Protocol);
                if (trial.SourceFingerprint != manifest.SourceHash || trial.Progress != manifest.Progress || trial.Action != configuration.Action ||
                    trial.Progress.RandomState != configuration.RandomState || trial.Progress.Position != trial.Progress.Updates) throw RunErrors.Invalid();
                ValidateBindings(trial.Keep, tokenizer);
                ValidateBindings(trial.Candidate, tokenizer);
            }
            if (command is "train" or "resume")
            {
                if (trial.Decision != "pending" || trial.Evaluation is not null) throw RunErrors.Invalid();
                var performed = 0;
                if (trial.Progress.Updates < configuration.MaxUpdates)
                {
                    await foreach (var sample in ForgeJsonlStream.ReadAsync(configuration.Training, tokenizer, configuration.DataLimits,
                        trial.Progress.Position, cancellationToken).ConfigureAwait(false))
                    {
                        trial.Advance(sample, trial.Progress.Position, trial.Progress.RandomState, cancellationToken);
                        Save(configuration, trial, manifestPath);
                        performed++;
                        if (trial.Progress.Updates == configuration.MaxUpdates || performed == maxUpdates) break;
                    }
                    if (performed < maxUpdates && trial.Progress.Updates < configuration.MaxUpdates) throw RunErrors.Invalid();
                }
            }
            else if (command == "evaluate")
            {
                await trial.EvaluateAsync(configuration.Acceptance.Identity,
                    ForgeJsonlStream.ReadAsync(configuration.Acceptance, tokenizer, configuration.DataLimits, 0, cancellationToken), cancellationToken).ConfigureAwait(false);
                trial.Complete();
                Save(configuration, trial, manifestPath);
            }
            else if (command == "generate")
            {
                if (trial.Decision == "pending" || !int.TryParse(arguments[4], out var count) || count < 0 ||
                    !ulong.TryParse(arguments[5], out var seed)) throw new ArgumentException(RunErrors.Message("Usage"));
                var model = trial.Decision == "candidate" ? trial.Candidate : trial.Keep;
                var session = model.Network.CreateTokenExecutor(cancellationToken).CreateTextSession(tokenizer, arguments[3], count,
                    new SamplingOptions(DoSample: false), seed, cancellationToken);
                while (!session.IsCompleted) session.NextToken(cancellationToken);
                await output.WriteLineAsync(JsonSerializer.Serialize(new { Text = session.GetGeneratedText() })).ConfigureAwait(false);
                return 0;
            }
            cancellationToken.ThrowIfCancellationRequested();
            await output.WriteLineAsync(JsonSerializer.Serialize(new { Manifest = manifestPath, trial.SourceFingerprint,
                trial.Progress, trial.Decision, trial.Evaluation, KeepCost = trial.Keep.MeasureCost(), CandidateCost = trial.Candidate.MeasureCost() })).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException)
        {
            await error.WriteLineAsync(RunErrors.Message("Cancelled")).ConfigureAwait(false);
            return 130;
        }
        catch (ArgumentException exception)
        {
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            return 2;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException or ArithmeticException)
        {
            await error.WriteLineAsync(RunErrors.Message("Failed") + " " + exception.Message).ConfigureAwait(false);
            return 1;
        }
    }

    private static void ValidateBindings(ForgeModel model, MiniMindTextTokenizer tokenizer)
    {
        var bindings = model.Network.TextBindings;
        if (bindings is null || bindings.TokenizerIdentity != tokenizer.Identity || bindings.VocabularySize != tokenizer.VocabularySize)
            throw RunErrors.Invalid();
    }

    private static void Save(ForgeRunConfiguration configuration, PairedTrial trial, string path)
    {
        var temporaryTrial = Path.Combine(configuration.DirectoryPath, Guid.NewGuid().ToString("N") + ".trial.tmp");
        trial.Save(temporaryTrial);
        var trialHash = ForgeArchive.Fingerprint(File.ReadAllBytes(temporaryTrial));
        var file = trialHash + ".trial.json";
        var trialPath = Path.Combine(configuration.DirectoryPath, file);
        if (!File.Exists(trialPath)) File.Move(temporaryTrial, trialPath);
        else
        {
            if (ForgeArchive.Fingerprint(File.ReadAllBytes(trialPath)) != trialHash) throw RunErrors.Invalid();
            File.Delete(temporaryTrial);
        }
        var manifest = new Manifest(Semantics, configuration.Fingerprint, configuration.Direction, configuration.RunId,
            configuration.SourceSha256, CursorSemantics, file, trialHash, trial.Progress);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.Write(JsonSerializer.SerializeToUtf8Bytes(manifest, ForgeRunConfiguration.Json));
            stream.Flush(true);
        }
        File.Move(temporary, path, true);
    }

    internal static async Task<byte[]> ReadBoundedAsync(string path, int maxBytes, CancellationToken ct)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous);
        if (stream.Length <= 0 || stream.Length > maxBytes) throw RunErrors.Invalid();
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, ct).ConfigureAwait(false);
        if (await stream.ReadAsync(new byte[1], ct).ConfigureAwait(false) != 0) throw RunErrors.Invalid();
        return bytes;
    }
}
