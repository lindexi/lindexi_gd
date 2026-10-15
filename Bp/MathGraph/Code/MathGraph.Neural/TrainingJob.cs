namespace MathGraphs.Neural;

public sealed partial class RecursiveNetwork
{
    private TrainingJobState? _trainingJob;

    public TrainingJobState? TrainingJob
    {
        get { lock (_sync) return _trainingJob; }
    }

    /// <summary>登记固定顺序数据集、AdamW 配置和轮数；每个样本是零状态完整训练窗口。</summary>
    public void ConfigureTrainingJob(TokenDataset dataset, int epochs = 1, AdamWOptions? optimizer = null)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(epochs);
        var options = optimizer ?? new AdamWOptions();
        ValidateAdamW(options);
        lock (_sync)
        {
            if (_trainingJob is not null) throw new InvalidOperationException();
            var bindings = TextBindings ?? throw new InvalidOperationException();
            if (dataset.TokenizerIdentity != bindings.TokenizerIdentity) throw new ArgumentException(nameof(dataset));
            var maxLength = 0;
            for (var i = 0; i < dataset.Count; i++)
            {
                var sample = dataset[i];
                if (sample.Count > Budget.MaxSteps || (long)(sample.Count - 1) * bindings.InternalSteps > Budget.MaxSteps)
                    throw new ArgumentException(nameof(dataset));
                foreach (var token in sample) bindings.ValidateToken(token);
                maxLength = Math.Max(maxLength, sample.Count);
            }
            _trainingJob = new TrainingJobState(dataset.Identity, dataset.TokenizerIdentity, dataset.Count, options, epochs, maxLength);
        }
    }

    /// <summary>从持久游标训练下一样本；数据必须与配置时内容完全相同，失败不推进游标。</summary>
    public double TrainNextSample(TokenDataset dataset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var job = _trainingJob ?? throw new InvalidOperationException();
            if (dataset.Identity != job.DatasetIdentity || dataset.TokenizerIdentity != job.TokenizerIdentity || dataset.Count != job.SampleCount)
                throw new ArgumentException(nameof(dataset));
            if (job.IsCompleted) throw new InvalidOperationException();
            var next = job with { CompletedSamples = checked(job.CompletedSamples + 1) };
            var loss = TrainTokensCore(dataset[job.NextSampleIndex], options: job.Optimizer, cancellationToken: cancellationToken,
                labelMask: dataset.GetLabelMask(job.NextSampleIndex));
            _trainingJob = next;
            return loss;
        }
    }

    private void RestoreTrainingJob(TrainingJobState? job)
    {
        if (job is null) return;
        if (string.IsNullOrWhiteSpace(job.DatasetIdentity) || job.DatasetIdentity.Length != 64 ||
            job.DatasetIdentity.Any(c => !Uri.IsHexDigit(c)) || TextBindings is null ||
            job.TokenizerIdentity != TextBindings.TokenizerIdentity || job.Optimizer is null ||
            job.SampleCount <= 0 || job.Epochs <= 0 || job.MaxSampleTokens < 2 || job.MaxSampleTokens > Budget.MaxSteps ||
            (long)(job.MaxSampleTokens - 1) * TextBindings.InternalSteps > Budget.MaxSteps ||
            job.CompletedSamples < 0 || job.CompletedSamples > job.TotalSamples || job.CompletedSamples > _trainingUpdates)
            throw new InvalidDataException();
        ValidateAdamW(job.Optimizer);
        _trainingJob = job;
    }
}
