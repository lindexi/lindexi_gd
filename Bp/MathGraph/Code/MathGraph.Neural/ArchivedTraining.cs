using System.Diagnostics;
using System.Text.Json;

namespace MathGraphs.Neural;

/// <summary>更新次数与时间任一达到即保存；时间在一致更新边界检查，发布成功后按根目录保留最近检查点。</summary>
public sealed record TrainingArchiveOptions(int SaveEveryUpdates = 10, int SaveEverySeconds = 600,
    int SaveTimeoutSeconds = 120, int MaxCheckpoints = 100);

public sealed record TrainingRunResult(string Directory, string LastCheckpoint, long CompletedSamples);

public sealed partial class RecursiveNetwork
{
    /// <summary>执行已配置训练作业并自动保留历史检查点；可用独立结构选择集驱动已配置控制器。不得与其他模型写操作并行运行。</summary>
    public async Task<TrainingRunResult> RunTrainingAsync(TokenDataset dataset, string archiveRoot,
        TrainingArchiveOptions? options = null, TokenDataset? structureSelection = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentException.ThrowIfNullOrWhiteSpace(archiveRoot);
        var configuration = options ?? new TrainingArchiveOptions();
        if (configuration.SaveEveryUpdates <= 0 || configuration.SaveEverySeconds <= 0 ||
            configuration.SaveTimeoutSeconds <= 0 || configuration.SaveTimeoutSeconds > 86400 || configuration.MaxCheckpoints <= 0)
            throw new ArgumentOutOfRangeException(nameof(options));
        lock (_sync)
        {
            var job = _trainingJob ?? throw new InvalidOperationException();
            if (dataset.Identity != job.DatasetIdentity) throw new ArgumentException(nameof(dataset));
            if ((_controller is null) != (structureSelection is null)) throw new ArgumentException(nameof(structureSelection));
            if (structureSelection is not null && (structureSelection.Identity == dataset.Identity ||
                structureSelection.TokenizerIdentity != dataset.TokenizerIdentity)) throw new ArgumentException(nameof(structureSelection));
        }
        var directory = Path.Combine(Path.GetFullPath(archiveRoot),
            DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", System.Globalization.CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(configuration.SaveTimeoutSeconds)))
        {
            await File.WriteAllBytesAsync(Path.Combine(directory, "run.json"), JsonSerializer.SerializeToUtf8Bytes(new
            {
                Format = 1, DatasetIdentity = dataset.Identity, SelectionIdentity = structureSelection?.Identity,
                Archive = configuration, Job = TrainingJob
            }), timeout.Token).ConfigureAwait(false);
        }
        long sequence = 0;
        long lastSavedUpdates = 0;
        var lastSaveTime = Stopwatch.GetTimestamp();
        var lastCheckpoint = string.Empty;
        await SaveAsync("initial").ConfigureAwait(false);
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool completed;
                bool feedbackReady;
                bool startObservation;
                lock (_sync)
                {
                    var job = _trainingJob ?? throw new InvalidOperationException();
                    completed = job.IsCompleted;
                    feedbackReady = _controller?.Pending is { } pending &&
                        _trainingUpdates - pending.TrainingUpdates >= _controller.Options.ObservationUpdates;
                    startObservation = _controller is { Pending: null } &&
                        job.TotalSamples - job.CompletedSamples >= _controller.Options.ObservationUpdates;
                }
                if (feedbackReady)
                {
                    var loss = EvaluateDataset(structureSelection ?? throw new InvalidOperationException(), cancellationToken);
                    CompleteStructureObservation(loss.MeanCrossEntropy);
                    await SaveAsync("feedback").ConfigureAwait(false);
                    continue;
                }
                if (completed) break;
                if (startObservation)
                {
                    var loss = EvaluateDataset(structureSelection ?? throw new InvalidOperationException(), cancellationToken);
                    BeginStructureObservation(loss.MeanCrossEntropy, cancellationToken);
                    await SaveAsync("structure").ConfigureAwait(false);
                }
                TrainNextSample(dataset, cancellationToken);
                if (_trainingUpdates - lastSavedUpdates >= configuration.SaveEveryUpdates ||
                    Stopwatch.GetElapsedTime(lastSaveTime).TotalSeconds >= configuration.SaveEverySeconds)
                    await SaveAsync("periodic").ConfigureAwait(false);
            }
            await SaveAsync("completed").ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await SaveAsync("cancelled").ConfigureAwait(false);
            throw;
        }
        catch (ArithmeticException)
        {
            await SaveAsync("numerical-error").ConfigureAwait(false);
            throw;
        }
        return new TrainingRunResult(directory, lastCheckpoint, TrainingJob?.CompletedSamples ?? 0);

        async Task SaveAsync(string reason)
        {
            byte[] bytes;
            long updates;
            long version;
            lock (_sync)
            {
                bytes = ExportCheckpoint();
                updates = _trainingUpdates;
                version = Version;
            }
            var name = FormattableString.Invariant($"checkpoint-{++sequence:D8}-update-{updates:D12}-version-{version:D12}-{reason}.json");
            var path = Path.Combine(directory, name);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(configuration.SaveTimeoutSeconds));
            try
            {
                await WriteCheckpointFileAsync(path, bytes, false, timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException error) when (timeout.IsCancellationRequested)
            {
                throw new IOException(Numeric.Message("ArchiveTimeout"), error);
            }
            CheckpointArchive.RetainLatest(archiveRoot, configuration.MaxCheckpoints, path);
            lastCheckpoint = path;
            lastSavedUpdates = updates;
            lastSaveTime = Stopwatch.GetTimestamp();
        }
    }
}
