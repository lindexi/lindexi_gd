using System.Security.Cryptography;
using System.Text.Json;
using FractalForge.Training;
using MathGraphs.Neural;

namespace FractalForge.Research.Widening;

/// <summary>固定 Widening 方向的准备、保持训练和公共克隆配对入口。</summary>
public static class WideningRunner
{
    private sealed record KeepManifest(string Semantics, string ConfigurationHash, string ModelHash, int Updates, long Tokens, double? Loss);

    /// <summary>prepare request.json directory；其他命令使用版本化实验配置，参数顺序与 ForgeRunner 一致。</summary>
    public static async Task<int> RunAsync(IReadOnlyList<string> arguments, TextWriter output, TextWriter error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (arguments.Count < 2) throw new ArgumentException(WideningJson.Message("Usage"));
            if (arguments[0] == "prepare")
            {
                if (arguments.Count != 3) throw new ArgumentException(WideningJson.Message("Usage"));
                var request = WideningJson.Parse<WideningPreparation>(await ReadAsync(arguments[1], 1048576, cancellationToken).ConfigureAwait(false));
                await WideningStrategy.PrepareAsync(request, arguments[2], cancellationToken).ConfigureAwait(false);
                return 0;
            }
            var experiment = WideningJson.Parse<WideningExperiment>(await ReadAsync(arguments[1], 1048576, cancellationToken).ConfigureAwait(false));
            Validate(experiment);
            if (arguments[0] == "diagnose")
            {
                if (arguments.Count != 4) throw WideningJson.Invalid();
                var tokens = WideningJson.Parse<int[]>(await ReadAsync(arguments[2], 1048576, cancellationToken).ConfigureAwait(false));
                var probes = WideningJson.Parse<Dictionary<string, double>[]>(await ReadAsync(arguments[3], 1048576, cancellationToken).ConfigureAwait(false));
                var sourceBytes = await ReadAsync(experiment.Run.SourceArchive, 67108864, cancellationToken).ConfigureAwait(false);
                if (ForgeArchive.Fingerprint(sourceBytes) != experiment.Run.SourceSha256) throw WideningJson.Invalid();
                var sourceModel = ForgeArchive.Import(sourceBytes);
                var candidate = WideningStrategy.CreateCandidate(sourceModel, experiment.Branch, experiment.Run.Action.Target, experiment.Run.Action.Alpha, cancellationToken);
                var loss = WideningMetrics.MeasureLoss(sourceModel, candidate, tokens, cancellationToken);
                var signals = experiment.Branch == "W0" ? Array.Empty<BranchSignal>() :
                    new[] { WideningMetrics.MeasureBranches(candidate, candidate.Topology.Units.Last(), probes, cancellationToken) };
                await output.WriteLineAsync(JsonSerializer.Serialize(new { experiment.Branch, TokenProbeSha256 = ForgeArchive.Fingerprint(JsonSerializer.SerializeToUtf8Bytes(tokens)),
                    InputProbeSha256 = ForgeArchive.Fingerprint(JsonSerializer.SerializeToUtf8Bytes(probes)), Loss = loss, Signals = signals })).ConfigureAwait(false);
                return 0;
            }
            if (experiment.Branch == "W0") return await RunKeepAsync(experiment, arguments, output, cancellationToken).ConfigureAwait(false);
            // The immutable derived configuration lets the shared runner enforce its complete contract on every invocation.
            var bytes = JsonSerializer.SerializeToUtf8Bytes(experiment.Run);
            var directory = Path.GetDirectoryName(Path.GetFullPath(arguments[1]))!;
            var path = Path.Combine(directory, ForgeArchive.Fingerprint(bytes) + ".run.json");
            if (!File.Exists(path)) await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            else if (!(await ReadAsync(path, 1048576, cancellationToken).ConfigureAwait(false)).SequenceEqual(bytes)) throw WideningJson.Invalid();
            var forwarded = arguments.ToArray(); forwarded[1] = path;
            return await ForgeRunner.RunAsync("Widening", forwarded, output, error, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { await error.WriteLineAsync(WideningJson.Message("Cancelled")).ConfigureAwait(false); return 130; }
        catch (ArgumentException ex) { await error.WriteLineAsync(ex.Message).ConfigureAwait(false); return 2; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException or ArithmeticException)
        { await error.WriteLineAsync(WideningJson.Message("Failed") + " " + ex.Message).ConfigureAwait(false); return 1; }
    }

    private static void Validate(WideningExperiment e)
    {
        if (e.Semantics != WideningExperiment.Version || e.Run is null || e.Run.Action is null || e.Run.Direction != "Widening" || e.Run.Semantics != ForgeRunner.Semantics ||
            e.Branch is not ("W0" or "W1" or "W2") || e.Run.Action.Kind != "clone-widen" ||
            e.Branch == "W1" && e.Run.Action.Alpha != .5 || e.Branch == "W2" &&
            (!double.IsFinite(e.Run.Action.Alpha) || e.Run.Action.Alpha <= 0 || e.Run.Action.Alpha >= 1 || e.Run.Action.Alpha == .5))
            throw WideningJson.Invalid();
    }

    private static async Task<int> RunKeepAsync(WideningExperiment e, IReadOnlyList<string> args, TextWriter output, CancellationToken ct)
    {
        var command = args[0];
        var count = command switch { "train" => 2, "resume" or "inspect" or "evaluate" => 3, "generate" => 6, _ => throw WideningJson.Invalid() };
        if (args.Count != count && !(command is "train" or "resume" && args.Count == count + 1)) throw WideningJson.Invalid();
        var invocation = int.MaxValue;
        if (args.Count == count + 1 && (!int.TryParse(args[^1], out invocation) || invocation <= 0)) throw WideningJson.Invalid();
        var c = e.Run;
        ForgeCorpusPreparation.ValidateUsage(c);
        static bool Segment(string s) => !string.IsNullOrWhiteSpace(s) && s.Length <= 80 && s.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_');
        if (!Segment(c.RunId) || c.MaxUpdates <= 0 || c.MaxTokens <= 0 || c.DataLimits is null || c.Optimizer is null ||
            c.Training.Identity == c.Acceptance.Identity || c.Training.Identity == c.FinalTest.Identity || c.Acceptance.Identity == c.FinalTest.Identity ||
            new[] { c.Training, c.Acceptance, c.FinalTest }.Select(x => x.Sha256).Distinct().Count() != 3 ||
            new[] { c.Training, c.Acceptance, c.FinalTest }.Select(x => Path.GetFullPath(x.Path)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
            throw WideningJson.Invalid();
        if (string.IsNullOrWhiteSpace(c.RuleVersion) || !double.IsFinite(c.MinimumGain) || c.MaxPairTapeCells <= 0 ||
            c.DataLimits.MaxLineBytes <= 0 || c.DataLimits.MaxFileBytes <= 0 || c.DataLimits.MaxSamples <= 0 || c.DataLimits.MaxSampleTokens < 2 || c.DataLimits.MaxTotalTokens < 2 ||
            !double.IsFinite(c.Optimizer.LearningRate) || c.Optimizer.LearningRate <= 0 ||
            !double.IsFinite(c.Optimizer.Beta1) || c.Optimizer.Beta1 < 0 || c.Optimizer.Beta1 >= 1 ||
            !double.IsFinite(c.Optimizer.Beta2) || c.Optimizer.Beta2 < 0 || c.Optimizer.Beta2 >= 1 ||
            !double.IsFinite(c.Optimizer.Epsilon) || c.Optimizer.Epsilon <= 0 || !double.IsFinite(c.Optimizer.WeightDecay) || c.Optimizer.WeightDecay < 0 ||
            new[] { c.Training, c.Acceptance, c.FinalTest }.Any(x => string.IsNullOrWhiteSpace(x.Identity) || x.Sha256.Length != 64 || x.Sha256.Any(ch => !(ch is >= '0' and <= '9' or >= 'A' and <= 'F'))))
            throw WideningJson.Invalid();
        var directory = Path.GetFullPath(Path.Combine(c.OutputRoot, "Widening", c.RunId));
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(directory, "writer.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifestPath = Path.Combine(directory, "keep.json");
        if (command != "train" && Path.GetFullPath(args[2]) != manifestPath) throw WideningJson.Invalid();
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(c.TokenizerPath, c.TokenizerConfigPath, cancellationToken: ct).ConfigureAwait(false);
        var source = await ReadAsync(c.SourceArchive, 67108864, ct).ConfigureAwait(false);
        if (ForgeArchive.Fingerprint(source) != c.SourceSha256 || tokenizer.Identity != c.TokenizerIdentity) throw WideningJson.Invalid();
        var hash = ForgeArchive.Fingerprint(JsonSerializer.SerializeToUtf8Bytes(e, WideningJson.Options));
        KeepManifest state;
        ForgeModel model;
        if (command == "train")
        {
            if (File.Exists(manifestPath)) throw WideningJson.Invalid();
            state = new("fractalforge.widening.keep.v1", hash, c.SourceSha256, 0, 0, null);
            model = ForgeArchive.Import(source);
        }
        else
        {
            state = WideningJson.Parse<KeepManifest>(await ReadAsync(manifestPath, 1048576, ct).ConfigureAwait(false));
            if (state.Semantics != "fractalforge.widening.keep.v1" || state.ConfigurationHash != hash ||
                state.Updates < 0 || state.Updates > c.MaxUpdates || state.Tokens < 0 || state.Tokens > c.MaxTokens ||
                state.ModelHash.Length != 64 || state.ModelHash.Any(ch => !char.IsAsciiHexDigit(ch))) throw WideningJson.Invalid();
            var bytes = await ReadAsync(Path.Combine(directory, state.ModelHash + ".forge"), 67108864, ct).ConfigureAwait(false);
            if (ForgeArchive.Fingerprint(bytes) != state.ModelHash) throw WideningJson.Invalid();
            model = ForgeArchive.Import(bytes);
        }
        if (model.Network.TextBindings is not { } binding || binding.TokenizerIdentity != tokenizer.Identity || binding.VocabularySize != tokenizer.VocabularySize ||
            model.MeasureCost().WindowTapeCells > c.MaxPairTapeCells || c.DataLimits.MaxSampleTokens > model.Network.Budget.MaxSteps) throw WideningJson.Invalid();
        foreach (var corpus in new[] { c.Training, c.Acceptance })
        {
            await using var stream = new FileStream(corpus.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length <= 0 || stream.Length > c.DataLimits.MaxFileBytes || Convert.ToHexString(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false)) != corpus.Sha256)
                throw WideningJson.Invalid();
        }
        async Task SaveAsync()
        {
            var bytes = ForgeArchive.Export(model);
            state = state with { ModelHash = ForgeArchive.Fingerprint(bytes) };
            var path = Path.Combine(directory, state.ModelHash + ".forge");
            if (!File.Exists(path)) await WriteDurableAsync(path, bytes, ct).ConfigureAwait(false);
            else if (!(await ReadAsync(path, 67108864, ct).ConfigureAwait(false)).SequenceEqual(bytes)) throw WideningJson.Invalid();
            var temporary = manifestPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await WriteDurableAsync(temporary, JsonSerializer.SerializeToUtf8Bytes(state, WideningJson.Options), ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            File.Move(temporary, manifestPath, true);
        }
        if (command is "train" or "resume")
        {
            if (state.Loss is not null) throw WideningJson.Invalid();
            await using var data = ForgeJsonlStream.ReadAsync(c.Training, tokenizer, c.DataLimits, state.Updates, ct).GetAsyncEnumerator(ct);
            if (command == "train") await SaveAsync().ConfigureAwait(false);
            var performed = 0;
            while (state.Updates < c.MaxUpdates && performed < invocation)
            {
                ct.ThrowIfCancellationRequested();
                if (!await data.MoveNextAsync().ConfigureAwait(false)) throw WideningJson.Invalid();
                var sample = data.Current;
                if (sample.Count - 1 > c.MaxTokens - state.Tokens) throw WideningJson.Invalid();
                model.Network.TrainTokens(sample, options: c.Optimizer, cancellationToken: ct);
                state = state with { Updates = state.Updates + 1, Tokens = checked(state.Tokens + sample.Count - 1) };
                await SaveAsync().ConfigureAwait(false);
                performed++;
            }
        }
        else if (command == "evaluate")
        {
            if (state.Updates != c.MaxUpdates || state.Loss is not null) throw WideningJson.Invalid();
            double sum = 0; long tokens = 0;
            await foreach (var sample in ForgeJsonlStream.ReadAsync(c.Acceptance, tokenizer, c.DataLimits, 0, ct).ConfigureAwait(false))
            {
                var n = sample.Count - 1;
                sum += n * model.Network.EvaluateTokens(sample, cancellationToken: ct); tokens = checked(tokens + n);
            }
            if (tokens == 0) throw WideningJson.Invalid();
            state = state with { Loss = sum / tokens };
            await SaveAsync().ConfigureAwait(false);
        }
        else if (command == "generate")
        {
            if (state.Loss is null || !int.TryParse(args[4], out var maximum) || maximum < 0 || !ulong.TryParse(args[5], out var seed)) throw WideningJson.Invalid();
            var session = model.Network.CreateTokenExecutor(ct).CreateTextSession(tokenizer, args[3], maximum, new SamplingOptions(DoSample: false), seed, ct);
            while (!session.IsCompleted) session.NextToken(ct);
            await output.WriteLineAsync(JsonSerializer.Serialize(new { Text = session.GetGeneratedText() })).ConfigureAwait(false);
            return 0;
        }
        await output.WriteLineAsync(JsonSerializer.Serialize(new { Manifest = manifestPath, State = state, Cost = model.MeasureCost() })).ConfigureAwait(false);
        return 0;
    }

    private static async Task WriteDurableAsync(string path, byte[] bytes, CancellationToken ct)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous);
        await stream.WriteAsync(bytes, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
    }

    private static async Task<byte[]> ReadAsync(string path, long maximum, CancellationToken ct)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > maximum) throw WideningJson.Invalid();
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, ct).ConfigureAwait(false);
        return bytes;
    }
}
