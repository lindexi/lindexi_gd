using System.Text.Json;

namespace MathGraphs.Neural;

/// <summary>绑定模型快照的有界生成会话，每一步以事务方式提交循环值、上下文和采样随机状态。</summary>
public sealed class GenerationSession
{
    private sealed record SessionData(int Format, SamplingOptions Options, ulong RandomState, int[] Context,
        double[] Logits, byte[] State, int Generated, int MaxNewTokens, int? EosToken);

    private readonly object _sync = new();
    private readonly TokenExecutor _executor;
    private readonly SamplingOptions _options;
    private readonly int _maxNewTokens;
    private readonly int? _eosToken;
    private RecurrentState _state;
    private double[] _logits;
    private int[] _context;
    private ulong _randomState;
    private int _generated;

    internal GenerationSession(TokenExecutor executor, SamplingOptions options, ulong randomState, int[] context,
        double[] logits, RecurrentState state, int generated, int maxNewTokens, int? eosToken)
    {
        _executor = executor;
        _options = options;
        _randomState = randomState;
        _context = context;
        _logits = logits;
        _state = state;
        _generated = generated;
        _maxNewTokens = maxNewTokens;
        _eosToken = eosToken;
    }

    public bool IsCompleted
    {
        get { lock (_sync) return _generated >= _maxNewTokens || (_generated > 0 && _context[^1] == _eosToken); }
    }

    public IReadOnlyList<int> Context
    {
        get { lock (_sync) return Array.AsReadOnly((int[])_context.Clone()); }
    }

    /// <summary>返回不包含提示词的生成 token 快照，适合整体解码 UTF-8 字节级分词结果。</summary>
    public IReadOnlyList<int> GetGeneratedTokens()
    {
        lock (_sync) return Array.AsReadOnly(_context[(_context.Length - _generated)..]);
    }

    /// <summary>生成一个 token；发生取消或计算错误时保留整个旧会话。</summary>
    public int NextToken(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsCompleted) throw new InvalidOperationException();
            var random = _randomState;
            var probabilities = TokenSampling.Probabilities(_logits, _context, _options);
            var token = TokenSampling.Select(probabilities, _options.DoSample, ref random);
            var output = _executor.Run(new[] { token }, state: _state, cancellationToken: cancellationToken);
            var context = new int[_context.Length + 1];
            _context.CopyTo(context, 0);
            context[^1] = token;
            var logits = output.Predictions[0].Logits.ToArray();
            cancellationToken.ThrowIfCancellationRequested();
            _context = context;
            _logits = logits;
            _state = output.State;
            _randomState = random;
            _generated++;
            return token;
        }
    }

    /// <summary>保存模型指纹校验的循环状态、上下文、待采样 logits、采样配置和随机状态。</summary>
    public byte[] ExportCheckpoint()
    {
        lock (_sync)
            return JsonSerializer.SerializeToUtf8Bytes(new SessionData(1, _options, _randomState, _context, _logits,
                _executor.ExportState(_state), _generated, _maxNewTokens, _eosToken));
    }

    internal static GenerationSession Restore(TokenExecutor executor, byte[] bytes, int vocabulary,
        int contextLimit, int steps, int maxBytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        if (bytes.Length > maxBytes) throw new InvalidDataException();
        try
        {
            var data = JsonSerializer.Deserialize<SessionData>(bytes, new JsonSerializerOptions
            {
                UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
            }) ?? throw new InvalidDataException();
            if (data.Format != 1 || data.Options is null || data.Context is null || data.Logits is null || data.State is null ||
                data.Generated < 0 || data.MaxNewTokens < 0 || data.Generated > data.MaxNewTokens ||
                data.Context.Length <= data.Generated || data.Context.Length > contextLimit ||
                (long)data.Context.Length - data.Generated + data.MaxNewTokens > contextLimit ||
                data.Logits.Length != vocabulary || data.Logits.Any(v => !double.IsFinite(v)) ||
                data.Context.Any(token => (uint)token >= (uint)vocabulary) ||
                (data.EosToken.HasValue && (uint)data.EosToken.Value >= (uint)vocabulary))
                throw new InvalidDataException();
            TokenSampling.Validate(data.Options);
            var state = executor.ImportState(data.State, maxBytes);
            if (state.Step != (long)data.Context.Length * steps) throw new InvalidDataException();
            var firstGenerated = data.Context.Length - data.Generated;
            for (var i = firstGenerated; i < data.Context.Length - 1; i++)
                if (data.Context[i] == data.EosToken) throw new InvalidDataException();
            return new GenerationSession(executor, data.Options, data.RandomState, data.Context, data.Logits,
                state, data.Generated, data.MaxNewTokens, data.EosToken);
        }
        catch (JsonException error) { throw new InvalidDataException(null, error); }
        catch (ArgumentException error) { throw new InvalidDataException(null, error); }
    }
}

public sealed partial class TokenExecutor
{
    /// <summary>创建可恢复生成会话；提示词和最大续写总长度受 MaxSteps 限制。</summary>
    public GenerationSession CreateGenerationSession(IReadOnlyList<int> prompt, int maxNewTokens,
        SamplingOptions? options = null, ulong seed = 1, int? eosToken = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentOutOfRangeException.ThrowIfNegative(maxNewTokens);
        if (prompt.Count == 0 || (long)prompt.Count + maxNewTokens > _budget.MaxSteps)
            throw new ArgumentOutOfRangeException(nameof(prompt));
        var configuration = options ?? new SamplingOptions();
        TokenSampling.Validate(configuration);
        if (eosToken.HasValue) _bindings.ValidateToken(eosToken.Value);
        var context = prompt.ToArray();
        var result = Run(context, cancellationToken: cancellationToken);
        return new GenerationSession(this, configuration, seed, context, result.Predictions[^1].Logits.ToArray(),
            result.State, 0, maxNewTokens, eosToken);
    }

    /// <summary>将生成检查点恢复到兼容模型快照；验证采样配置、上下文预算及内部时钟。</summary>
    public GenerationSession RestoreGenerationSession(byte[] bytes, int maxBytes = 67108864) =>
        GenerationSession.Restore(this, bytes, _bindings.VocabularySize, _budget.MaxSteps, _bindings.InternalSteps, maxBytes);
}
