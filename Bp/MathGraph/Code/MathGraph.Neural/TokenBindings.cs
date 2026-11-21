namespace MathGraphs.Neural;

public sealed class TokenBindings
{
    private readonly double[][] _encoding;
    internal double[][] CopyEncoding() => _encoding.Select(row => row.ToArray()).ToArray();
    internal ScalarParameter[][] Weights { get; }
    internal ScalarParameter[] Biases { get; }

    /// <summary>登记 tokenizer 身份、固定 token 编码、标量输入和多节点词表读出。</summary>
    public TokenBindings(string tokenizerIdentity, IReadOnlyList<string> inputNodes,
        IReadOnlyList<string> readoutNodes, IReadOnlyList<IReadOnlyList<double>> encoding,
        int internalSteps = 1, int seed = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenizerIdentity);
        ArgumentNullException.ThrowIfNull(inputNodes);
        ArgumentNullException.ThrowIfNull(readoutNodes);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(internalSteps);
        if (inputNodes.Count == 0 || readoutNodes.Count == 0 || encoding.Count < 2 ||
            inputNodes.Any(string.IsNullOrWhiteSpace) || readoutNodes.Any(string.IsNullOrWhiteSpace) ||
            inputNodes.Distinct().Count() != inputNodes.Count || readoutNodes.Distinct().Count() != readoutNodes.Count)
            throw new ArgumentException(nameof(inputNodes));
        if (encoding.Any(row => row is null || row.Count != inputNodes.Count || row.Any(v => !double.IsFinite(v))))
            throw new ArgumentException(nameof(encoding));
        TokenizerIdentity = tokenizerIdentity;
        InputNodes = Array.AsReadOnly(inputNodes.ToArray());
        ReadoutNodes = Array.AsReadOnly(readoutNodes.ToArray());
        _encoding = encoding.Select(row => row.ToArray()).ToArray();
        InternalSteps = internalSteps;
        var random = new Random(seed);
        Weights = encoding.Select(_ => readoutNodes.Select(_ =>
            new ScalarParameter((random.NextDouble() - 0.5) / Math.Sqrt(readoutNodes.Count))).ToArray()).ToArray();
        Biases = encoding.Select(_ => new ScalarParameter()).ToArray();
    }

    public string TokenizerIdentity { get; }
    public IReadOnlyList<string> InputNodes { get; }
    public IReadOnlyList<string> ReadoutNodes { get; }
    public int InternalSteps { get; }
    public int VocabularySize => _encoding.Length;
    public IReadOnlyList<ScalarParameter> Parameters => Array.AsReadOnly(Weights.SelectMany(r => r).Concat(Biases).ToArray());

    internal IReadOnlyDictionary<string, double> Encode(int token)
    {
        ValidateToken(token);
        return InputNodes.Select((id, i) => (id, value: _encoding[token][i])).ToDictionary(p => p.id, p => p.value);
    }

    internal void ValidateToken(int token)
    {
        if ((uint)token >= (uint)VocabularySize) throw new ArgumentOutOfRangeException(nameof(token));
    }
}

public sealed record TokenPrediction(int Position, IReadOnlyList<double> Logits);

public sealed class TokenExecutionResult
{
    internal TokenExecutionResult(TokenPrediction[] predictions, ExecutionResult execution)
    {
        Predictions = Array.AsReadOnly(predictions);
        Execution = execution;
    }

    public IReadOnlyList<TokenPrediction> Predictions { get; }
    public RecurrentState State => Execution.State;
    internal ExecutionResult Execution { get; }
}

public sealed partial class TokenExecutor
{
    private readonly NetworkExecutor _executor;
    private readonly TokenBindings _bindings;
    private readonly double[][] _weights;
    private readonly double[] _biases;
    private readonly NetworkBudget _budget;

    internal TokenExecutor(NetworkExecutor executor, TokenBindings bindings, NetworkBudget budget)
    {
        _executor = executor;
        _bindings = bindings;
        _budget = budget;
        _weights = bindings.Weights.Select(row => row.Select(p => p.Value).ToArray()).ToArray();
        _biases = bindings.Biases.Select(p => p.Value).ToArray();
    }

    /// <summary>按 token 执行 K 个共享内部步；掩码为 false 的位置不推进循环状态。</summary>
    public TokenExecutionResult Run(IReadOnlyList<int> tokens, IReadOnlyList<bool>? mask = null,
        RecurrentState? state = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (mask is not null && mask.Count != tokens.Count) throw new ArgumentException(nameof(mask));
        if (tokens.Count > _budget.MaxSteps) throw new ArgumentOutOfRangeException(nameof(tokens));
        var positions = Enumerable.Range(0, tokens.Count).Where(i => mask is null || mask[i]).ToArray();
        if ((long)positions.Length * _bindings.InternalSteps > _budget.MaxSteps ||
            (long)positions.Length * _bindings.VocabularySize > _budget.MaxTapeCells)
            throw new ArgumentOutOfRangeException(nameof(tokens));
        var inputs = new List<IReadOnlyDictionary<string, double>>();
        foreach (var position in positions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var input = _bindings.Encode(tokens[position]);
            for (var k = 0; k < _bindings.InternalSteps; k++) inputs.Add(input);
        }
        var result = _executor.Run(inputs, state, cancellationToken);
        var predictions = new TokenPrediction[positions.Length];
        var readoutValues = new double[_bindings.ReadoutNodes.Count];
        for (var t = 0; t < positions.Length; t++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var output = result.Outputs[(t + 1) * _bindings.InternalSteps - 1];
            var logits = new double[_bindings.VocabularySize];
            for (var r = 0; r < readoutValues.Length; r++)
                readoutValues[r] = output[_bindings.ReadoutNodes[r]];
            for (var c = 0; c < logits.Length; c++)
            {
                var value = _biases[c];
                for (var r = 0; r < readoutValues.Length; r++)
                    value += _weights[c][r] * readoutValues[r];
                if (!double.IsFinite(value)) Numeric.RequireFinite(value, $"token={positions[t]}, class={c}");
                logits[c] = value;
            }
            predictions[t] = new TokenPrediction(positions[t], Array.AsReadOnly(logits));
        }
        return new TokenExecutionResult(predictions, result);
    }

    /// <summary>使用同一状态转移进行贪心生成；返回续写 token，不包含提示词。</summary>
    public IReadOnlyList<int> Generate(IReadOnlyList<int> prompt, int maxNewTokens, int? eosToken = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentOutOfRangeException.ThrowIfNegative(maxNewTokens);
        if (prompt.Count == 0 || maxNewTokens > _budget.MaxSteps) throw new ArgumentOutOfRangeException(nameof(prompt));
        if (eosToken.HasValue) _bindings.ValidateToken(eosToken.Value);
        if (maxNewTokens == 0) return Array.Empty<int>();
        var result = Run(prompt, cancellationToken: cancellationToken);
        var generated = new List<int>();
        for (var i = 0; i < maxNewTokens; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var logits = result.Predictions[^1].Logits;
            var next = 0;
            for (var c = 1; c < logits.Count; c++) if (logits[c] > logits[next]) next = c;
            generated.Add(next);
            if (next == eosToken || i + 1 == maxNewTokens) break;
            result = Run(new[] { next }, state: result.State, cancellationToken: cancellationToken);
        }
        return generated.AsReadOnly();
    }
}
