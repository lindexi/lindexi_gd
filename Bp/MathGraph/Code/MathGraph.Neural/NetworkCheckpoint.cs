using System.Text.Json;

namespace MathGraphs.Neural;

public sealed partial class RecursiveNetwork
{
    private sealed record ParameterData(Guid Id, double Value, bool Decay, double First, double Second, long Updates);
    private sealed record EdgeData(Guid Id, string From, string To, Guid Weight, int Delay);
    private sealed record NodeData(string Id, Guid Bias, ScalarActivation Activation, Guid? Gate, GraphData? Child, NodeStatistics? Statistics = null);
    private sealed record GraphData(NodeData[] Nodes, EdgeData[] Edges);
    private sealed record TextData(string Identity, string[] Inputs, string[] Readouts, double[][] Encoding,
        int Steps, Guid[][] Weights, Guid[] Biases);
    private sealed record CheckpointData(int Format, long Version, NetworkBudget Budget,
        GraphData Graph, ParameterData[] Parameters, TextData? Text, ControllerData? Controller = null, long TrainingUpdates = 0, TrainingJobState? TrainingJob = null, PipelineTrainingState? Pipeline = null);

    /// <summary>在一致更新边界序列化模型、文本绑定和优化器状态；包含已配置训练作业的样本游标，不包含外部数据本体或推理会话。</summary>
    public byte[] ExportCheckpoint()
    {
        lock (_sync)
        {
            var layout = ExecutionLayout.Create(Graph, Budget);
            var parameters = new Dictionary<Guid, ScalarParameter>(layout.Parameters);
            TextData? text = null;
            if (TextBindings is { } bindings)
            {
                foreach (var parameter in bindings.Parameters) parameters.TryAdd(parameter.Id, parameter);
                text = new TextData(bindings.TokenizerIdentity, bindings.InputNodes.ToArray(), bindings.ReadoutNodes.ToArray(),
                    bindings.CopyEncoding(), bindings.InternalSteps, bindings.Weights.Select(row => row.Select(p => p.Id).ToArray()).ToArray(),
                    bindings.Biases.Select(p => p.Id).ToArray());
            }
            return JsonSerializer.SerializeToUtf8Bytes(new CheckpointData(4, Version, Budget, Capture(Graph),
                parameters.Values.Select(p => new ParameterData(p.Id, p.Value, p.Decay, p.FirstMoment, p.SecondMoment, p.Updates)).ToArray(), text, _controller, _trainingUpdates, _trainingJob, _pipeline),
                new JsonSerializerOptions { MaxDepth = 256 });
        }

        static GraphData Capture(MathGraph<Neuron, NeuralConnection> graph) => new(
            graph.ElementList.Select(n => new NodeData(n.Id, n.Value.Bias.Id, n.Value.Activation,
                n.Value.Gate?.Id, n.Value.Child is { } child ? Capture(child.Graph) : null, n.Value.Statistics)).ToArray(),
            graph.ElementList.SelectMany(n => n.OutEdgeList).Select(e => new EdgeData(e.EdgeInfo!.Id,
                e.From.Id, e.To.Id, e.EdgeInfo.Weight.Id, e.EdgeInfo.Delay)).ToArray());
    }

    /// <summary>从受大小及资源限制的检查点恢复，验证格式、身份、边界和数值后才返回网络。</summary>
    public static RecursiveNetwork ImportCheckpoint(byte[] bytes, NetworkBudget? limits = null, int maxBytes = 67108864)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        if (bytes.Length > maxBytes) throw new InvalidDataException();
        var cap = limits ?? new NetworkBudget();
        if (cap.MaxNodes <= 0 || cap.MaxDepth < 0 || cap.MaxDelay < 0 || cap.MaxSteps <= 0 || cap.MaxTapeCells <= 0 || cap.MaxConnections < 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
        try
        {
            var data = JsonSerializer.Deserialize<CheckpointData>(bytes, new JsonSerializerOptions
            {
                MaxDepth = 256,
                UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
            }) ?? throw new InvalidDataException();
            if ((data.Format < 1 || data.Format > 4) || (data.Format < 4 && data.Pipeline is not null) || (data.Format < 3 && data.TrainingJob is not null) || (data.Format == 1 && (data.Controller is not null || data.TrainingUpdates != 0)) || data.Version < 0 || data.Budget is null || data.Graph is null || data.Parameters is null ||
                data.Parameters.LongLength > cap.MaxTapeCells) throw new InvalidDataException();
            var b = data.Budget;
            if (b.MaxNodes > cap.MaxNodes || b.MaxDepth > cap.MaxDepth || b.MaxDelay > cap.MaxDelay ||
                b.MaxSteps > cap.MaxSteps || b.MaxTapeCells > cap.MaxTapeCells || b.MaxConnections > cap.MaxConnections || b.MaxConnections < 0) throw new InvalidDataException();
            var parameters = new Dictionary<Guid, ScalarParameter>();
            foreach (var p in data.Parameters)
            {
                if (p is null || p.Id == Guid.Empty || !double.IsFinite(p.Value) || !double.IsFinite(p.First) ||
                    !double.IsFinite(p.Second) || p.Second < 0 || p.Updates < 0 ||
                    (p.Updates == 0 && (p.First != 0 || p.Second != 0))) throw new InvalidDataException();
                parameters.Add(p.Id, new ScalarParameter(p.Value, p.Decay)
                { Id = p.Id, FirstMoment = p.First, SecondMoment = p.Second, Updates = p.Updates });
            }
            var used = new HashSet<Guid>();
            var nodeCount = 0;
            long edgeCount = 0;
            var network = new RecursiveNetwork(Restore(data.Graph, 0), b);
            if (data.Text is { } text)
            {
                if (text.Encoding is null || text.Inputs is null || text.Readouts is null || text.Weights is null || text.Biases is null ||
                    (long)text.Encoding.Length * (text.Inputs.Length + text.Readouts.Length + 1) > cap.MaxTapeCells)
                    throw new InvalidDataException();
                var bindings = new TokenBindings(text.Identity, text.Inputs, text.Readouts, text.Encoding, text.Steps);
                if (text.Weights.Length != bindings.VocabularySize || text.Biases.Length != bindings.VocabularySize)
                    throw new InvalidDataException();
                for (var c = 0; c < bindings.VocabularySize; c++)
                {
                    if (text.Weights[c] is null || text.Weights[c].Length != bindings.ReadoutNodes.Count)
                        throw new InvalidDataException();
                    bindings.Biases[c] = Parameter(text.Biases[c]);
                    for (var r = 0; r < bindings.ReadoutNodes.Count; r++) bindings.Weights[c][r] = Parameter(text.Weights[c][r]);
                }
                // 当前文本 API 创建独立读出参数，不接受检查点暗中引入额外共享约定。
                var graphParameters = ExecutionLayout.Create(network.Graph, b).Parameters;
                if (bindings.Parameters.Select(p => p.Id).Distinct().Count() != bindings.Parameters.Count ||
                    bindings.Parameters.Any(p => graphParameters.ContainsKey(p.Id)))
                    throw new InvalidDataException();
                network.BindText(bindings);
            }
            if (used.Count != parameters.Count) throw new InvalidDataException();
            network.Version = data.Version;
            network.RestoreController(data.Controller, data.TrainingUpdates);
            network.RestoreTrainingJob(data.TrainingJob);
            if (data.Pipeline is { } pipeline)
            {
                network.ValidatePipeline(pipeline);
                network._pipeline = pipeline;
            }
            return network;

            ScalarParameter Parameter(Guid id)
            {
                used.Add(id);
                return parameters.TryGetValue(id, out var parameter) ? parameter : throw new InvalidDataException();
            }

            MathGraph<Neuron, NeuralConnection> Restore(GraphData graph, int depth)
            {
                if (graph.Nodes is null || graph.Edges is null || depth > cap.MaxDepth ||
                    (nodeCount += graph.Nodes.Length) > Math.Min(cap.MaxNodes, b.MaxNodes) ||
                    (edgeCount += graph.Edges.Length) > Math.Min(cap.MaxConnections, b.MaxConnections) || edgeCount > cap.MaxTapeCells)
                    throw new InvalidDataException();
                if (depth > 0 && graph.Nodes.Length != 5) throw new InvalidDataException();
                var result = new MathGraph<Neuron, NeuralConnection>();
                var nodes = new Dictionary<string, MathGraphElement<Neuron, NeuralConnection>>();
                foreach (var node in graph.Nodes)
                {
                    if (node is null || string.IsNullOrWhiteSpace(node.Id) || (node.Gate is null) != (node.Child is null))
                        throw new InvalidDataException();
                    var statistics = node.Statistics ?? new NodeStatistics();
                    statistics.Validate();
                    var neuron = new Neuron(Parameter(node.Bias), node.Activation) { Statistics = statistics };
                    if (node.Child is { } child && node.Gate is { } gate)
                    {
                        neuron.Gate = Parameter(gate);
                        if (neuron.Gate.Decay) throw new InvalidDataException();
                        neuron.Child = new FiveNodeGraph(Restore(child, depth + 1));
                    }
                    nodes.Add(node.Id, result.CreateAndAddElement(neuron, node.Id));
                }
                foreach (var edge in graph.Edges)
                {
                    if (edge is null || edge.Id == Guid.Empty || edge.From is null || edge.To is null ||
                        !nodes.TryGetValue(edge.From, out var from) || !nodes.TryGetValue(edge.To, out var to))
                        throw new InvalidDataException();
                    result.AddEdge(from, to, new NeuralConnection(Parameter(edge.Weight), edge.Delay) { Id = edge.Id });
                }
                return result;
            }
        }
        catch (JsonException error) { throw new InvalidDataException(null, error); }
        catch (ArgumentException error) { throw new InvalidDataException(null, error); }
        catch (InvalidOperationException error) { throw new InvalidDataException(null, error); }
    }

    /// <summary>从文件异步读取检查点，在分配载荷前检查长度，并拒绝截断或并发变化的文件。</summary>
    public static async Task<RecursiveNetwork> LoadCheckpointAsync(string path, NetworkBudget? limits = null,
        int maxBytes = 67108864, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length <= 0 || stream.Length > maxBytes) throw new InvalidDataException();
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
        if (await stream.ReadAsync(new byte[1], cancellationToken).ConfigureAwait(false) != 0)
            throw new InvalidDataException();
        cancellationToken.ThrowIfCancellationRequested();
        return ImportCheckpoint(bytes, limits, maxBytes);
    }

    /// <summary>使用同目录临时文件写入并发布检查点；失败时不覆盖原检查点。</summary>
    public async Task SaveCheckpointAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        await WriteCheckpointFileAsync(path, ExportCheckpoint(), true, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteCheckpointFileAsync(string path, byte[] bytes, bool overwrite, CancellationToken cancellationToken)
    {
        var destination = Path.GetFullPath(path);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                65536, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, destination, overwrite);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
