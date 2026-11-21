using System.Diagnostics;
using System.Threading.Channels;

namespace MathGraphs.Neural;

public sealed partial class RecursiveNetwork
{
    private PipelineTrainingState? _pipeline;
    public PipelineTrainingState? PipelineTraining { get { lock (_sync) return _pipeline; } }

    /// <summary>登记流水线文件和训练语义，不读取整份数据；在线哈希分集，消费进度进入模型检查点。</summary>
    public void ConfigurePipeline(string path, string format, MiniMindTextTokenizer tokenizer, PipelineOptions options,
        JsonlDatasetOptions limits, DatasetPartitionOptions partition, AdamWOptions optimizer,
        StructureOptions? controller = null, ulong seed = 1)
    {
        ArgumentNullException.ThrowIfNull(tokenizer);
        var file = new FileInfo(Path.GetFullPath(path));
        if (!file.Exists) throw new FileNotFoundException(null, path);
        lock (_sync)
        {
            if (_pipeline is not null || _trainingJob is not null || _controller is not null) throw new InvalidOperationException();
            var state = new PipelineTrainingState(file.FullName, file.Length, file.LastWriteTimeUtc.Ticks, format,
                tokenizer.Identity, options, limits, partition, optimizer, controller, seed, Selection: []);
            ValidatePipeline(state);
            _pipeline = state;
        }
    }

    private void ValidatePipeline(PipelineTrainingState state)
    {
        if (TextBindings is null || TextBindings.TokenizerIdentity != state.TokenizerIdentity ||
            state.Format is not ("pretrain" or "chat") || state.Options is null || state.Limits is null || state.Partition is null ||
            state.Optimizer is null || state.Options.Epochs <= 0 || state.Options.WarmupUpdates < 0 || state.Options.PrefetchCapacity <= 0 ||
            state.Options.SelectionSamples <= 0 || state.Options.PrefixLines < 0 || state.Limits.MaxLineBytes <= 0 ||
            state.Limits.MaxFileBytes <= 0 || state.Limits.MaxSamples <= 0 || state.Limits.MaxTotalTokens <= 0 ||
            state.Limits.MaxSampleTokens < 2 || state.Limits.MaxSampleTokens > Budget.MaxSteps ||
            (long)(state.Limits.MaxSampleTokens - 1) * TextBindings.InternalSteps > Budget.MaxSteps ||
            state.Epoch < 0 || state.Epoch > state.Options.Epochs || state.ByteOffset < 0 || state.ByteOffset > state.SourceLength ||
            state.Line < 0 || state.TrainedSamples < 0 || state.WarmupCompleted < 0 || state.WarmupCompleted > state.Options.WarmupUpdates ||
            !double.IsFinite(state.Partition.StructureSelectionFraction) || state.Partition.StructureSelectionFraction <= 0 ||
            !double.IsFinite(state.Partition.TestFraction) || state.Partition.TestFraction <= 0 ||
            state.Partition.StructureSelectionFraction + state.Partition.TestFraction >= 1 ||
            state.Selection is null || state.Selection.Length > state.Options.SelectionSamples)
            throw new ArgumentException(nameof(state));
        ValidateAdamW(state.Optimizer);
        if (state.ControllerOptions is { } controller) ValidateStructureOptions(controller);
    }

    /// <summary>后台分词预取与顺序训练并行；每个检查点记录消费位置，恢复后重新生产未消费样本。</summary>
    public async Task<TrainingRunResult> RunPipelineAsync(MiniMindTextTokenizer tokenizer, string archiveRoot,
        TrainingArchiveOptions? archive = null, IProgress<PipelineProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokenizer);
        var state = _pipeline ?? throw new InvalidOperationException();
        ValidatePipeline(state);
        var file = new FileInfo(state.SourcePath);
        if (!file.Exists || file.Length != state.SourceLength || file.LastWriteTimeUtc.Ticks != state.SourceWriteTicks ||
            tokenizer.Identity != state.TokenizerIdentity) throw new InvalidDataException();
        var settings = archive ?? new TrainingArchiveOptions();
        if (settings.SaveEveryUpdates <= 0 || settings.SaveEverySeconds <= 0 || settings.SaveTimeoutSeconds <= 0 || settings.MaxCheckpoints <= 0)
            throw new ArgumentOutOfRangeException(nameof(archive));
        var directory = Path.Combine(Path.GetFullPath(archiveRoot), "pipeline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var runStart = Stopwatch.GetTimestamp();
        double trainingSeconds = 0, evaluationSeconds = 0, serializationSeconds = 0, writeSeconds = 0, retentionSeconds = 0;
        long saves = 0, lastCheckpointBytes = 0;
        double? lastTrainingLoss = null, selectionLoss = null;
        double weightedLossSum = 0;
        long supervisedTokens = 0, measuredSamples = 0;
        StructureDecision? lastDecision = null;
        StructureFeedback? lastFeedback = null;
        long sequence = 0;
        var savedUpdates = _trainingUpdates;
        var savedTime = Stopwatch.GetTimestamp();
        var lastCheckpoint = string.Empty;
        TokenDataset? selection = null;
        await SaveAsync("initial").ConfigureAwait(false);
        try
        {
            while ((_pipeline ?? throw new InvalidOperationException()).Epoch < state.Options.Epochs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                state = _pipeline!;
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var channel = Channel.CreateBounded<PipelineItem>(new BoundedChannelOptions(state.Options.PrefetchCapacity)
                    { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
                var producer = Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var item in PipelineSource.ReadAsync(state, tokenizer, stop.Token).ConfigureAwait(false))
                            await channel.Writer.WriteAsync(item, stop.Token).ConfigureAwait(false);
                    }
                    finally { channel.Writer.TryComplete(); }
                }, CancellationToken.None);
                try
                {
                    await ObserveAsync().ConfigureAwait(false);
                    await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (item.End)
                        {
                            await producer.ConfigureAwait(false);
                            lock (_sync)
                            {
                                var current = _pipeline!;
                                _pipeline = current with { Epoch = current.Epoch + 1, ByteOffset = 0, Line = 0,
                                    EpochSamples = 0, EpochTokens = 0, SelectionFrozen = true };
                            }
                            await ObserveAsync().ConfigureAwait(false);
                            await SaveAsync("epoch").ConfigureAwait(false);
                            continue;
                        }
                        var warmupFinished = false;
                        lock (_sync)
                        {
                            var current = _pipeline!;
                            var selectionSamples = current.Selection ?? throw new InvalidOperationException();
                            var next = current with { ByteOffset = item.Offset, Line = item.Line };
                            if (item.Tokens is { } tokens)
                            {
                                if (current.EpochSamples >= current.Limits.MaxSamples || tokens.Length > current.Limits.MaxTotalTokens - current.EpochTokens)
                                    throw new InvalidDataException();
                                next = next with { EpochSamples = current.EpochSamples + 1, EpochTokens = current.EpochTokens + tokens.Length };
                                if (item.Split == 0)
                                {
                                    var trainingStart = Stopwatch.GetTimestamp();
                                    lastTrainingLoss = TrainTokensCore(tokens, options: current.Optimizer, cancellationToken: cancellationToken, labelMask: item.Labels);
                                    var supervised = _lastTrainingMeasurement!.SupervisedTokens;
                                    weightedLossSum += lastTrainingLoss.Value * supervised;
                                    supervisedTokens += supervised;
                                    measuredSamples++;
                                    trainingSeconds += Stopwatch.GetElapsedTime(trainingStart).TotalSeconds;
                                    var warmup = Math.Min(current.Options.WarmupUpdates, current.WarmupCompleted + 1);
                                    warmupFinished = warmup > current.WarmupCompleted && warmup == current.Options.WarmupUpdates;
                                    next = next with { TrainedSamples = current.TrainedSamples + 1, WarmupCompleted = warmup };
                                }
                                else if (item.Split == 1 && !current.SelectionFrozen && !selectionSamples.Any(s => s.ContentHash == item.Hash))
                                {
                                    var samples = selectionSamples.Append(new PipelineSelectionSample(item.Hash, tokens, item.Labels)).ToArray();
                                    next = next with { Selection = samples, SelectionFrozen = samples.Length == current.Options.SelectionSamples };
                                }
                            }
                            _pipeline = next;
                        }
                        if (warmupFinished) await SaveAsync("warmup").ConfigureAwait(false);
                        if (_trainingUpdates - savedUpdates >= settings.SaveEveryUpdates || Stopwatch.GetElapsedTime(savedTime).TotalSeconds >= settings.SaveEverySeconds)
                            await SaveAsync("periodic").ConfigureAwait(false);
                        await ObserveAsync().ConfigureAwait(false);
                        Report("consumed");
                    }
                    await producer.ConfigureAwait(false);
                }
                finally
                {
                    stop.Cancel();
                    try { await producer.ConfigureAwait(false); }
                    catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
                }
            }
            await SaveAsync("completed").ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await SaveAsync("cancelled").ConfigureAwait(false);
            throw;
        }
        catch (InvalidDataException)
        {
            await SaveAsync("data-error").ConfigureAwait(false);
            throw;
        }
        catch (ArithmeticException)
        {
            await SaveAsync("numerical-error").ConfigureAwait(false);
            throw;
        }
        return new TrainingRunResult(directory, lastCheckpoint, _pipeline!.TrainedSamples);

        async Task ObserveAsync()
        {
            var current = _pipeline!;
            if (current.ControllerOptions is null || !current.SelectionFrozen || current.Selection!.Length == 0 ||
                current.WarmupCompleted < current.Options.WarmupUpdates) return;
            selection ??= new TokenDataset(current.TokenizerIdentity, current.Selection.Select(s => (IReadOnlyList<int>)s.Tokens),
                int.MaxValue, current.Format == "chat" ? current.Selection.Select(s => (IReadOnlyList<bool>)(s.Labels ?? throw new InvalidDataException())).ToArray() : null);
            if (_controller is null) ConfigureStructureController(current.ControllerOptions, current.ControllerSeed);
            if (_controller!.Pending is { } pending && _trainingUpdates - pending.TrainingUpdates >= _controller.Options.ObservationUpdates)
            {
                lastFeedback = CompleteStructureObservation(EvaluateSelection());
                Report("feedback");
                await SaveAsync("feedback").ConfigureAwait(false);
            }
            if (_controller.Pending is null && current.Epoch < current.Options.Epochs)
            {
                lastDecision = BeginStructureObservation(EvaluateSelection(), cancellationToken);
                lastFeedback = null;
                Report("structure");
                await SaveAsync("structure").ConfigureAwait(false);
            }
        }

        double EvaluateSelection()
        {
            Report("evaluating");
            var started = Stopwatch.GetTimestamp();
            var result = EvaluateDataset(selection ?? throw new InvalidOperationException(), cancellationToken);
            evaluationSeconds += Stopwatch.GetElapsedTime(started).TotalSeconds;
            selectionLoss = result.MeanCrossEntropy;
            Report("evaluated");
            return result.MeanCrossEntropy;
        }

        void Report(string stage) => progress?.Report(new PipelineProgress(stage, _pipeline!.Epoch,
            _pipeline.ByteOffset, _pipeline.SourceLength, _pipeline.TrainedSamples, _pipeline.WarmupCompleted,
            _pipeline.Selection!.Length, lastCheckpoint, new PipelineTimings(Stopwatch.GetElapsedTime(runStart).TotalSeconds,
                trainingSeconds, evaluationSeconds, serializationSeconds, writeSeconds, retentionSeconds, saves, lastCheckpointBytes),
            new PipelineLearning(lastTrainingLoss, weightedLossSum, supervisedTokens, measuredSamples,
                selectionLoss, lastDecision, lastFeedback)));

        async Task SaveAsync(string reason)
        {
            Report("saving");
            var serializationStart = Stopwatch.GetTimestamp();
            byte[] bytes;
            lock (_sync) bytes = ExportCheckpoint();
            serializationSeconds += Stopwatch.GetElapsedTime(serializationStart).TotalSeconds;
            lastCheckpointBytes = bytes.LongLength;
            var path = Path.Combine(directory, FormattableString.Invariant($"checkpoint-{++sequence:D8}-update-{_trainingUpdates:D12}-{reason}.json"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(settings.SaveTimeoutSeconds));
            var writeStart = Stopwatch.GetTimestamp();
            try { await WriteCheckpointFileAsync(path, bytes, false, timeout.Token).ConfigureAwait(false); }
            catch (OperationCanceledException error) when (timeout.IsCancellationRequested) { throw new IOException(Numeric.Message("ArchiveTimeout"), error); }
            writeSeconds += Stopwatch.GetElapsedTime(writeStart).TotalSeconds;
            var retentionStart = Stopwatch.GetTimestamp();
            CheckpointArchive.RetainLatest(archiveRoot, settings.MaxCheckpoints, path);
            retentionSeconds += Stopwatch.GetElapsedTime(retentionStart).TotalSeconds;
            saves++;
            savedUpdates = _trainingUpdates;
            savedTime = Stopwatch.GetTimestamp();
            lastCheckpoint = path;
            Report(reason);
        }
    }
}
