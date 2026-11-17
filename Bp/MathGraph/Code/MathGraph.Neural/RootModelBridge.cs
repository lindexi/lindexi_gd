namespace MathGraphs.Neural;

public sealed partial class RecursiveNetwork
{
    /// <summary>为显式根图创建新身份参数，优化器矩和更新数均从零开始。</summary>
    public static ScalarParameter CreateRootParameter(Guid id, double value, bool decay = true)
    {
        if (id == Guid.Empty) throw new ArgumentException(nameof(id));
        return new ScalarParameter(value, decay) { Id = id };
    }

    /// <summary>复制一个参数的稳定身份、值和 AdamW 历史，不开放参数 setter。</summary>
    public static ScalarParameter CopyParameterState(ScalarParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return new ScalarParameter(parameter.Value, parameter.Decay)
        {
            Id = parameter.Id, FirstMoment = parameter.FirstMoment,
            SecondMoment = parameter.SecondMoment, Updates = parameter.Updates
        };
    }

    /// <summary>为外部显式根图编译器建立稳定连接身份；构造后仍由正常布局完整验证。</summary>
    public static NeuralConnection CreateRootConnection(Guid id, ScalarParameter weight, int delay = 0)
    {
        if (id == Guid.Empty) throw new ArgumentException(nameof(id));
        return new NeuralConnection(weight, delay) { Id = id };
    }

    /// <summary>在完整更新边界复制文本读出和更新计数到独立根图；不迁移旧布局缓存或旧控制事务。</summary>
    public RecursiveNetwork RebuildRoot(MathGraph<Neuron, NeuralConnection> graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        lock (_sync)
        {
            if (_controller is not null || _trainingJob is not null || _pipeline is not null ||
                Graph.ElementList.Any(n => n.Value.Child is not null)) throw new InvalidOperationException();
            var result = new RecursiveNetwork(graph, Budget);
            if (TextBindings is { } text)
            {
                var bindings = new TokenBindings(text.TokenizerIdentity, text.InputNodes, text.ReadoutNodes, text.CopyEncoding(), text.InternalSteps);
                for (var c = 0; c < text.VocabularySize; c++)
                {
                    bindings.Biases[c] = CopyParameterState(text.Biases[c]);
                    for (var r = 0; r < text.ReadoutNodes.Count; r++) bindings.Weights[c][r] = CopyParameterState(text.Weights[c][r]);
                }
                result.BindText(bindings);
            }
            result._trainingUpdates = _trainingUpdates;
            result.Version = checked(Version + 1);
            return result;
        }
    }
}
