using System.Text;
using System.Text.Json;
using System.Resources;
using MathGraphs.Neural;

namespace MathGraphs.Neural.Cli;

public static class CpuApplication
{
    private static readonly ResourceManager Resources = new("MathGraph.Neural.Cli.Messages", typeof(CpuApplication).Assembly);
    private static string Message(string key) => Resources.GetString(key) ?? key;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    /// <summary>运行 CPU 训练、续训、原始文本/聊天生成和检查点查看；命令路径相对配置文件解析。</summary>
    public static async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        if (args.Length < 2 || args[0] is not ("train" or "resume" or "generate" or "chat" or "inspect"))
        {
            await output.WriteLineAsync(Message("Usage"));
            return 2;
        }
        try
        {
            var configPath = Path.GetFullPath(args[1]);
            var config = JsonSerializer.Deserialize<ApplicationConfig>(await File.ReadAllTextAsync(configPath, cancellationToken), JsonOptions)
                ?? throw new InvalidDataException();
            var baseDirectory = Path.GetDirectoryName(configPath) ?? throw new InvalidOperationException();
            string Resolve(string path) => Path.GetFullPath(path, baseDirectory);
            if (args[0] != "train" && args.Length < 3) throw new ArgumentException(Message("Usage"));
            RecursiveNetwork? model = args[0] == "train" ? null :
                await RecursiveNetwork.LoadCheckpointAsync(Resolve(args[2]), config.Budget, cancellationToken: cancellationToken);
            if (args[0] == "train")
            {
                var latest = CheckpointArchive.FindLatest(Resolve(config.ArchiveRoot));
                if (latest is not null)
                {
                    await output.WriteLineAsync(string.Format(System.Globalization.CultureInfo.CurrentCulture, Message("AutoResume"), latest));
                    model = await RecursiveNetwork.LoadCheckpointAsync(latest, config.Budget, cancellationToken: cancellationToken);
                }
            }
            if (args[0] == "inspect")
            {
                await output.WriteLineAsync(JsonSerializer.Serialize(new { model!.Version, model.TrainingJob, model.PipelineTraining,
                    Nodes = model.GetNodeObservations().Count, model.Budget, Tokenizer = model.TextBindings?.TokenizerIdentity }, JsonOptions));
                return 0;
            }
            var tokenizer = await MiniMindTextTokenizer.LoadAsync(Resolve(config.TokenizerPath), Resolve(config.TokenizerConfigPath), cancellationToken: cancellationToken);
            if (model is not null && (model.TextBindings?.TokenizerIdentity != tokenizer.Identity || model.TextBindings.VocabularySize != tokenizer.VocabularySize))
                throw new ArgumentException(Message("TokenizerMismatch"));
            if (args[0] is "generate" or "chat")
            {
                if (args.Length != 4) throw new ArgumentException(Message("Usage"));
                var executor = model!.CreateTokenExecutor(cancellationToken);
                var session = args[0] == "chat"
                    ? executor.CreateChatSession(tokenizer, new[] { new SupervisedChatMessage("user", args[3]) }, config.MaxNewTokens,
                        options: config.Sampling, cancellationToken: cancellationToken)
                    : executor.CreateTextSession(tokenizer, args[3], config.MaxNewTokens, config.Sampling, cancellationToken: cancellationToken);
                while (!session.IsCompleted) session.NextToken(cancellationToken);
                await output.WriteLineAsync(session.GetGeneratedText());
                return 0;
            }
            await output.WriteLineAsync(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                Message(config.PrefixLines == 0 ? "FullDataset" : "PrefixDataset"), Resolve(config.DatasetPath), config.PrefixLines,
                config.DataLimits.MaxSampleTokens));
            if (model?.PipelineTraining is not null || (model is null && config.Pipeline))
            {
                if (model is null)
                {
                    model = CreateModel(tokenizer, config);
                    model.ConfigurePipeline(Resolve(config.DatasetPath), config.DatasetFormat, tokenizer,
                        new PipelineOptions(config.Epochs, config.WarmupUpdates, config.PrefetchCapacity, config.SelectionSamples, config.PrefixLines),
                        config.DataLimits, config.Partition, config.Optimizer, config.Controller, (ulong)(uint)config.Seed);
                }
                var pipelineResult = await model.RunPipelineAsync(tokenizer, Resolve(config.ArchiveRoot), config.Archive,
                    new PipelineOutput(output), cancellationToken);
                await output.WriteLineAsync(JsonSerializer.Serialize(pipelineResult, JsonOptions));
                return 0;
            }
            var progress = new PreparationProgress(output);
            var data = await LoadDatasetAsync(Resolve(config.DatasetPath), tokenizer, config, cancellationToken, progress);
            var partition = DatasetPartition.Create(data, config.Partition, config.DataLimits.MaxTotalTokens, cancellationToken, progress);
            await output.WriteLineAsync(string.Format(System.Globalization.CultureInfo.CurrentCulture, Message("DatasetReady"),
                data.Count, partition.Training.Count, partition.StructureSelection.Count, partition.Test.Count, config.Epochs));
            var archiveRoot = Resolve(config.ArchiveRoot);
            if (model is null)
            {
                model = CreateModel(tokenizer, config);
                var setup = Path.Combine(archiveRoot, "setup-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(setup);
                await output.WriteLineAsync(setup);
                await File.WriteAllTextAsync(Path.Combine(setup, "config.json"), JsonSerializer.Serialize(config, JsonOptions), cancellationToken);
                await model.SaveCheckpointAsync(Path.Combine(setup, "initial.json"), cancellationToken);
                if (config.WarmupUpdates < 0) throw new ArgumentOutOfRangeException(nameof(config.WarmupUpdates));
                for (var i = 0; i < config.WarmupUpdates; i++)
                {
                    var index = i % partition.Training.Count;
                    model.TrainTokens(partition.Training[index], options: config.Optimizer,
                        cancellationToken: cancellationToken, labelMask: partition.Training.GetLabelMask(index));
                    using var saveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(config.Archive.SaveTimeoutSeconds));
                    await model.SaveCheckpointAsync(Path.Combine(setup, $"warmup-{i + 1:D6}.json"), saveTimeout.Token);
                }
                model.ConfigureTrainingJob(partition.Training, config.Epochs, config.Optimizer);
                if (config.Controller is not null) model.ConfigureStructureController(config.Controller, (ulong)(uint)config.Seed);
                await model.SaveCheckpointAsync(Path.Combine(setup, "ready.json"), cancellationToken);
            }
            await output.WriteLineAsync(Message("Training"));
            var result = await model.RunTrainingAsync(partition.Training, archiveRoot, config.Archive,
                config.Controller is null ? null : partition.StructureSelection, cancellationToken);
            await output.WriteLineAsync(JsonSerializer.Serialize(result, JsonOptions));
            return 0;
        }
        catch (OperationCanceledException) { await output.WriteLineAsync(Message("Cancelled")); return 130; }
        catch (IOException e) { await output.WriteLineAsync(e.Message); return 1; }
        catch (ArgumentException e) { await output.WriteLineAsync(e.Message); return 2; }
        catch (InvalidOperationException e) { await output.WriteLineAsync(e.Message); return 1; }
        catch (JsonException e) { await output.WriteLineAsync(e.Message); return 1; }
        catch (ArithmeticException e) { await output.WriteLineAsync(e.Message); return 1; }
    }

    private sealed class PipelineOutput(TextWriter output) : IProgress<PipelineProgress>
    {
        private long _last = System.Diagnostics.Stopwatch.GetTimestamp();
        private double _weightedLoss, _elapsed;
        private long _tokens, _samples;
        public void Report(PipelineProgress value)
        {
            if (value.Stage is "consumed" or "periodic" && System.Diagnostics.Stopwatch.GetElapsedTime(_last).TotalSeconds < 10) return;
            _last = System.Diagnostics.Stopwatch.GetTimestamp();
            output.WriteLine();
            output.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Message("PipelineProgress"),
                value.Stage, value.Epoch, value.TrainedSamples, value.WarmupCompleted, value.ByteOffset / 1048576.0,
                value.SourceLength / 1048576.0, value.SelectionSamples, value.Checkpoint));
            if (value.Timings is { } timing)
                output.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Message("PipelineTimings"),
                    timing.ElapsedSeconds, timing.TrainingSeconds, timing.EvaluationSeconds, timing.SerializationSeconds,
                    timing.WriteSeconds, timing.RetentionSeconds, timing.Saves, timing.LastCheckpointBytes / 1048576.0));
            if (value.Learning is { } learning)
            {
                var tokens = learning.SupervisedTokens - _tokens;
                var elapsed = (value.Timings?.ElapsedSeconds ?? _elapsed) - _elapsed;
                if (tokens > 0)
                {
                    output.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Message("PipelineLoss"),
                        learning.LastTrainingLoss, (learning.WeightedLossSum - _weightedLoss) / tokens,
                        learning.TrainingSamples - _samples, tokens, elapsed > 0 ? tokens / elapsed : 0));
                    _weightedLoss = learning.WeightedLossSum;
                    _tokens = learning.SupervisedTokens;
                    _samples = learning.TrainingSamples;
                    _elapsed = value.Timings?.ElapsedSeconds ?? _elapsed;
                }
                if (value.Stage == "evaluated")
                    output.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Message("PipelineSelectionLoss"), learning.SelectionLoss));
                if (value.Stage == "structure" && learning.Decision is { } decision)
                    output.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Message("PipelineDecision"),
                        decision.NodeId ?? Message("KeepStructure"), decision.Probability, decision.AddedNodes));
                if (value.Stage == "feedback" && learning.Feedback is { } feedback)
                    output.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Message("PipelineFeedback"),
                        feedback.Reward, feedback.Advantage, feedback.Baseline));
            }
            output.Flush();
        }
    }

    private sealed class PreparationProgress(TextWriter output) : IProgress<DatasetProgress>
    {
        private readonly System.Diagnostics.Stopwatch _timer = System.Diagnostics.Stopwatch.StartNew();
        public void Report(DatasetProgress value)
        {
            var percent = value.TotalBytes is > 0 ? (100.0 * value.Bytes / value.TotalBytes.Value).ToString("F2") + "%" : "-";
            output.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Message("PreparationProgress"),
                Message("Stage" + value.Stage), value.Bytes / 1048576.0, percent, value.Samples, value.Tokens,
                _timer.Elapsed.TotalSeconds, value.Samples / Math.Max(0.001, _timer.Elapsed.TotalSeconds)));
            output.Flush();
        }
    }

    private static RecursiveNetwork CreateModel(MiniMindTextTokenizer tokenizer, ApplicationConfig config)
    {
        var bits = 1;
        while ((1L << bits) < tokenizer.VocabularySize) bits++;
        var graph = new MathGraphs.MathGraph<Neuron, NeuralConnection>();
        var nodes = Enumerable.Range(0, bits).Select(i => graph.CreateAndAddElement(new Neuron(), $"input-{i}")).ToArray();
        var random = new Random(config.Seed);
        for (var i = 0; i < nodes.Length; i++)
            graph.AddEdge(nodes[i], nodes[(i + 1) % nodes.Length], new NeuralConnection(new ScalarParameter(random.NextDouble() - 0.5), 1));
        var model = new RecursiveNetwork(graph, config.Budget);
        var ids = nodes.Select(n => n.Id).ToArray();
        var encoding = Enumerable.Range(0, tokenizer.VocabularySize).Select(id => (IReadOnlyList<double>)
            Enumerable.Range(0, bits).Select(bit => ((id >> bit) & 1) == 0 ? -1d : 1d).ToArray()).ToArray();
        model.BindText(new TokenBindings(tokenizer.Identity, ids, ids, encoding, config.InternalSteps, config.Seed));
        return model;
    }

    private static async Task<TokenDataset> LoadDatasetAsync(string path, MiniMindTextTokenizer tokenizer,
        ApplicationConfig config, CancellationToken cancellationToken, IProgress<DatasetProgress> progress)
    {
        if (config.DatasetFormat is not ("pretrain" or "chat") || config.PrefixLines < 0) throw new ArgumentException(nameof(config));
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous);
        if (config.PrefixLines == 0)
            return config.DatasetFormat == "chat" ? await ChatJsonl.LoadAsync(file, tokenizer, config.DataLimits, cancellationToken, progress)
                : await PretrainingJsonl.LoadAsync(file, tokenizer, config.DataLimits, cancellationToken, progress);
        using var prefix = new MemoryStream();
        var buffer = new byte[8192];
        var lines = 0;
        while (lines < config.PrefixLines)
        {
            var read = await file.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            for (var i = 0; i < read && lines < config.PrefixLines; i++)
            {
                if (prefix.Length >= config.DataLimits.MaxFileBytes) throw new InvalidDataException();
                prefix.WriteByte(buffer[i]);
                if (buffer[i] == '\n') lines++;
            }
        }
        prefix.Position = 0;
        return config.DatasetFormat == "chat" ? await ChatJsonl.LoadAsync(prefix, tokenizer, config.DataLimits, cancellationToken, progress)
            : await PretrainingJsonl.LoadAsync(prefix, tokenizer, config.DataLimits, cancellationToken, progress);
    }
}
