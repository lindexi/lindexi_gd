using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using FractalForge.Core;
using MathGraphs.Neural;

namespace FractalForge.Training;

/// <summary>版本化四点模型封装；Neural 内层格式保持原语义。</summary>
public static class ForgeArchive
{
    private sealed record NodeEntry(string Id, ForgeNode Node);
    private sealed record BiasEntry(string Id, ForgeBias Bias);
    private sealed record ModelData(string Semantics, NodeEntry[] Nodes, BiasEntry[] Biases, ForgeRoute[] Routes, FourPointUnit[] Units, byte[] Network);
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 128
    };

    /// <summary>导出完整逻辑路径、参数身份及现有 Neural 优化器和读出状态。</summary>
    public static byte[] Export(ForgeModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return JsonSerializer.SerializeToUtf8Bytes(new ModelData(ForgeTopology.Semantics,
            model.Topology.Graph.ElementList.Select(n => new NodeEntry(n.Id, n.Value)).ToArray(),
            model.Topology.Biases.Select(p => new BiasEntry(p.Key, p.Value)).ToArray(),
            model.Topology.Routes.ToArray(), model.Topology.Units.ToArray(), model.Network.ExportCheckpoint()), JsonOptions);
    }

    /// <summary>拒绝未知语义、额外字段和不匹配逻辑/数值拓扑，不按节点数量猜格式。</summary>
    public static ForgeModel Import(byte[] bytes, NetworkBudget? limits = null, int maxBytes = 67108864)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length > maxBytes || maxBytes <= 0) throw new InvalidDataException();
        var data = JsonSerializer.Deserialize<ModelData>(bytes, JsonOptions) ?? throw new InvalidDataException();
        if (data.Semantics != ForgeTopology.Semantics || data.Nodes is null || data.Biases is null || data.Routes is null || data.Units is null || data.Network is null) throw new InvalidDataException();
        var topology = new ForgeTopology(data.Nodes.ToDictionary(n => n.Id, n => n.Node), data.Biases.ToDictionary(b => b.Id, b => b.Bias), data.Routes, data.Units);
        return ForgeModel.Restore(topology, RecursiveNetwork.ImportCheckpoint(data.Network, limits));
    }

    /// <summary>计算内容身份，用于显式清单引用与来源校验。</summary>
    public static string Fingerprint(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
