namespace MathGraphs.Neural;

public sealed partial class RecursiveNetwork
{
    public TokenBindings? TextBindings { get; private set; }

    private static (int[] Tokens, bool[] Labels) SelectTokenLabels(IReadOnlyList<int> tokens,
        IReadOnlyList<bool>? mask, IReadOnlyList<bool>? labelMask)
    {
        if (labelMask is not null && labelMask.Count != tokens.Count) throw new ArgumentException(nameof(labelMask));
        var positions = Enumerable.Range(0, tokens.Count).Where(i => mask is null || mask[i]).ToArray();
        var labels = positions.Select(i => labelMask is null || labelMask[i]).ToArray();
        if (!labels.Skip(1).Any(value => value)) throw new ArgumentException(nameof(labelMask));
        return (positions.Select(i => tokens[i]).ToArray(), labels);
    }

    /// <summary>在根网络上登记一次文本绑定，拒绝不存在的节点和超预算配置。</summary>
    public void BindText(TokenBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        lock (_sync)
        {
            if (TextBindings is not null) throw new InvalidOperationException();
            var layout = ExecutionLayout.Create(Graph, Budget);
            if (bindings.InputNodes.Concat(bindings.ReadoutNodes).Any(id => !layout.Outputs.ContainsKey(id)) ||
                bindings.InternalSteps > Budget.MaxSteps ||
                (long)bindings.VocabularySize * (bindings.InputNodes.Count + bindings.ReadoutNodes.Count + 1) > Budget.MaxTapeCells)
                throw new ArgumentException(nameof(bindings));
            TextBindings = bindings;
            Version++;
        }
    }

    /// <summary>捕获递归网络和词表读出参数的一致快照。</summary>
    public TokenExecutor CreateTokenExecutor(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var bindings = TextBindings ?? throw new InvalidOperationException();
            var executor = new TokenExecutor(CreateExecutor(cancellationToken), bindings, Budget);
            cancellationToken.ThrowIfCancellationRequested();
            return executor;
        }
    }

    /// <summary>只读评估下一 token 交叉熵；labelMask 指定原序列中参与损失的目标位置，不移除上下文。</summary>
    public double EvaluateTokens(IReadOnlyList<int> tokens, IReadOnlyList<bool>? mask = null,
        CancellationToken cancellationToken = default, IReadOnlyList<bool>? labelMask = null)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (mask is not null && mask.Count != tokens.Count) throw new ArgumentException(nameof(mask));
        if (tokens.Count > Budget.MaxSteps) throw new ArgumentOutOfRangeException(nameof(tokens));
        TokenExecutor executor;
        int[] active;
        bool[] labels;
        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bindings = TextBindings ?? throw new InvalidOperationException();
            (active, labels) = SelectTokenLabels(tokens, mask, labelMask);
            if (active.Length < 2) throw new ArgumentException(nameof(tokens));
            foreach (var token in active) bindings.ValidateToken(token);
            executor = CreateTokenExecutor(cancellationToken);
        }
        var result = executor.Run(active[..^1], cancellationToken: cancellationToken);
        var labelCount = labels.Skip(1).Count(value => value);
        double loss = 0;
        for (var t = 0; t < result.Predictions.Count; t++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!labels[t + 1]) continue;
            var logits = result.Predictions[t].Logits;
            var maximum = logits.Max();
            loss += (Math.Log(logits.Sum(value => Math.Exp(value - maximum))) +
                (maximum - logits[active[t + 1]])) / labelCount;
        }
        return Numeric.RequireFinite(loss);
    }

    /// <summary>训练独立样本；mask 移除 padding，labelMask 仅屏蔽目标损失，按有效标签数平均并完整反向。</summary>
    public double TrainTokens(IReadOnlyList<int> tokens, IReadOnlyList<bool>? mask = null,
        AdamWOptions? options = null, CancellationToken cancellationToken = default, IReadOnlyList<bool>? labelMask = null)
    {
        lock (_sync)
        {
            if (_trainingJob is not null) throw new InvalidOperationException();
            return TrainTokensCore(tokens, mask, options, cancellationToken, labelMask);
        }
    }

    private double TrainTokensCore(IReadOnlyList<int> tokens, IReadOnlyList<bool>? mask = null,
        AdamWOptions? options = null, CancellationToken cancellationToken = default, IReadOnlyList<bool>? labelMask = null)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (mask is not null && mask.Count != tokens.Count) throw new ArgumentException(nameof(mask));
        if (tokens.Count > Budget.MaxSteps) throw new ArgumentOutOfRangeException(nameof(tokens));
        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bindings = TextBindings ?? throw new InvalidOperationException();
            var measurementStart = System.Diagnostics.Stopwatch.GetTimestamp();
            var allocatedStart = GC.GetAllocatedBytesForCurrentThread();
            var (active, labels) = SelectTokenLabels(tokens, mask, labelMask);
            var labelCount = labels.Skip(1).Count(value => value);
            if (active.Length < 2) throw new ArgumentException(nameof(tokens));
            foreach (var token in active) bindings.ValidateToken(token);
            var layoutAllocation = GC.GetAllocatedBytesForCurrentThread();
            var layoutStart = System.Diagnostics.Stopwatch.GetTimestamp();
            var layout = ExecutionLayout.Create(Graph, Budget, cancellationToken: cancellationToken);
            var executor = new NetworkExecutor(layout, Budget, Version);
            var readout = new TrainingReadout(bindings, cancellationToken);
            var count = active.Length - 1;
            if ((long)count * bindings.InternalSteps > Budget.MaxSteps ||
                (long)count * bindings.VocabularySize > Budget.MaxTapeCells)
                throw new ArgumentOutOfRangeException(nameof(tokens));
            var forwardAllocation = GC.GetAllocatedBytesForCurrentThread();
            var forwardStart = System.Diagnostics.Stopwatch.GetTimestamp();
            var inputs = new List<IReadOnlyDictionary<string, double>>(count * bindings.InternalSteps);
            for (var t = 0; t < count; t++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = bindings.Encode(active[t]);
                for (var k = 0; k < bindings.InternalSteps; k++) inputs.Add(input);
            }
            var result = executor.Run(inputs, cancellationToken: cancellationToken);
            var lossAllocation = GC.GetAllocatedBytesForCurrentThread();
            var lossStart = System.Diagnostics.Stopwatch.GetTimestamp();
            var seeds = Enumerable.Range(0, count * bindings.InternalSteps)
                .Select(_ => new Dictionary<string, double>()).ToArray();
            var parameters = new Dictionary<Guid, ScalarParameter>(layout.Parameters);
            foreach (var parameter in bindings.Parameters) parameters.Add(parameter.Id, parameter);
            var gradients = parameters.Keys.ToDictionary(id => id, _ => 0d);
            double loss = 0;
            var exponentials = new double[bindings.VocabularySize];
            var biasGradients = new double[bindings.VocabularySize];
            var weightGradients = Enumerable.Range(0, bindings.ReadoutNodes.Count)
                .Select(_ => new double[bindings.VocabularySize]).ToArray();
            var logits = new double[bindings.VocabularySize];
            var readoutValues = new double[bindings.ReadoutNodes.Count];
            var readoutGradients = new double[bindings.ReadoutNodes.Count];
            for (var t = 0; t < count; t++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!labels[t + 1]) continue;
                var q = (t + 1) * bindings.InternalSteps - 1;
                for (var r = 0; r < readoutValues.Length; r++)
                    readoutValues[r] = result.Outputs[q][bindings.ReadoutNodes[r]];
                readout.Forward(readoutValues, logits, t, cancellationToken);
                var maximum = logits.Max();
                for (var c = 0; c < exponentials.Length; c++) exponentials[c] = Math.Exp(logits[c] - maximum);
                var total = exponentials.Sum();
                loss += (Math.Log(total) + (maximum - logits[active[t + 1]])) / labelCount;
                TrainingReadout.NormalizeGradients(exponentials, total, active[t + 1], labelCount, biasGradients);
                readout.Backward(exponentials, readoutValues, weightGradients, readoutGradients, cancellationToken);
                for (var r = 0; r < readoutGradients.Length; r++)
                    seeds[q][bindings.ReadoutNodes[r]] = readoutGradients[r];
            }
            for (var c = 0; c < bindings.VocabularySize; c++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Add(bindings.Biases[c].Id, biasGradients[c]);
                for (var r = 0; r < readoutValues.Length; r++)
                    Add(bindings.Weights[c][r].Id, weightGradients[r][c]);
            }
            Numeric.RequireFinite(loss);
            var backwardAllocation = GC.GetAllocatedBytesForCurrentThread();
            var backwardStart = System.Diagnostics.Stopwatch.GetTimestamp();
            var statistics = new Dictionary<Neuron, NodeStatistics>();
            foreach (var (id, value) in result.BackwardCore(seeds, statistics, cancellationToken)) Add(id, value);
            var nextTrainingUpdates = checked(_trainingUpdates + 1);
            var optimizerAllocation = GC.GetAllocatedBytesForCurrentThread();
            var optimizerStart = System.Diagnostics.Stopwatch.GetTimestamp();
            ApplyAdamW(parameters, gradients, options ?? new AdamWOptions(), cancellationToken);
            foreach (var (neuron, updated) in statistics) neuron.Statistics = updated;
            _trainingUpdates = nextTrainingUpdates;
            Version++;
            var end = System.Diagnostics.Stopwatch.GetTimestamp();
            _lastTrainingMeasurement = new TrainingMeasurement(Version, labelCount,
                System.Diagnostics.Stopwatch.GetElapsedTime(measurementStart, end),
                System.Diagnostics.Stopwatch.GetElapsedTime(layoutStart, forwardStart),
                System.Diagnostics.Stopwatch.GetElapsedTime(forwardStart, lossStart),
                System.Diagnostics.Stopwatch.GetElapsedTime(lossStart, backwardStart),
                System.Diagnostics.Stopwatch.GetElapsedTime(backwardStart, optimizerStart),
                System.Diagnostics.Stopwatch.GetElapsedTime(optimizerStart, end),
                GC.GetAllocatedBytesForCurrentThread() - allocatedStart,
                forwardAllocation - layoutAllocation, lossAllocation - forwardAllocation,
                backwardAllocation - lossAllocation, optimizerAllocation - backwardAllocation,
                GC.GetAllocatedBytesForCurrentThread() - optimizerAllocation);
            return loss;

            void Add(Guid id, double value)
            {
                var sum = gradients[id] + value;
                if (!double.IsFinite(sum)) Numeric.RequireFinite(sum, $"parameter={id}");
                gradients[id] = sum;
            }
        }
    }
}
