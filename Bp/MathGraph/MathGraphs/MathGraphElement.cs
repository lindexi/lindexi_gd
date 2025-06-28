using System.Diagnostics;

namespace MathGraph;

/// <summary>
/// 表示图里的一个元素
/// </summary>
/// <typeparam name="TElementInfo">元素本身的类型</typeparam>
/// <typeparam name="TEdgeInfo">边的类型</typeparam>
public class MathGraphElement<TElementInfo, TEdgeInfo>
{
    /// <summary>
    /// 创建图里的一个元素
    /// </summary>
    /// <param name="mathGraph"></param>
    /// <param name="value"></param>
    /// <param name="id"></param>
    public MathGraphElement(MathGraph<TElementInfo, TEdgeInfo> mathGraph, TElementInfo value, string? id = null)
    {
        Value = value;
        MathGraph = mathGraph;

        if (id is null)
        {
            id = MathGraphElementIdGenerator.GenerateId();
        }

        Id = id;

        if (Value is IMathGraphElementSensitive<TElementInfo, TEdgeInfo> sensitive)
        {
            Debug.Assert(sensitive.MathGraphElement is null);
            sensitive.MathGraphElement = this;
        }
    }

    /// <summary>
    /// 元素所在的图
    /// </summary>
    public MathGraph<TElementInfo, TEdgeInfo> MathGraph { get; }

    public string Id { get; }

    /// <summary>
    /// 元素自己的数据
    /// </summary>
    public TElementInfo Value { get; }

    /// <summary>
    /// 出度的元素列表，包含所有从当前元素出发的边指向的元素。对于带双向边的元素来说，出度与入度是相同的
    /// </summary>
    public IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> OutElementList => _outElementList;

    /// <summary>
    /// 入度的元素列表，包含所有指向当前元素的边的起点元素。对于带双向边的元素来说，出度与入度是相同的
    /// </summary>
    public IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> InElementList => _inElementList;

    public IReadOnlyList<MathGraphEdge<TElementInfo, TEdgeInfo>> EdgeList => _edgeList;

    private readonly List<MathGraphElement<TElementInfo, TEdgeInfo>> _outElementList = [];
    private readonly List<MathGraphElement<TElementInfo, TEdgeInfo>> _inElementList = [];

    private readonly List<MathGraphEdge<TElementInfo, TEdgeInfo>> _edgeList = [];

    /// <summary>
    /// 添加边的关系，只加边关系，不加元素关系。如需加元素关系，调用 <see cref="MathGraph{TElementInfo, TEdgeInfo}.AddEdge"/> 方法，或调用 <see cref="AddInElement"/> 或 <see cref="AddOutElement"/> 方法
    /// </summary>
    /// <param name="edge"></param>
    public void AddEdge(MathGraphEdge<TElementInfo, TEdgeInfo> edge)
    {
        foreach (var mathGraphEdge in _edgeList)
        {
            if (ReferenceEquals(mathGraphEdge, edge))
            {
                return;
            }
        }

        edge.EnsureContain(this);

        _edgeList.Add(edge);

        var otherElement = edge.GetOtherElement(this);
#if DEBUG
        foreach (var mathGraphEdge in otherElement._edgeList)
        {
            if (ReferenceEquals(mathGraphEdge, edge))
            {
                Debug.Fail("边都是成对出现，在当前元素不存在的边，在边的对应元素必定也不存在");
            }
        }
#endif
        otherElement._edgeList.Add(edge);
    }

    public void AddOutElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        if (_outElementList.Contains(element))
        {
            return;
        }

        _outElementList.Add(element);
        Debug.Assert(!element._inElementList.Contains(this));
        element._inElementList.Add(this);
    }

    public void RemoveOutElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        if (!_outElementList.Contains(element))
        {
            return;
        }

        _outElementList.Remove(element);
        Debug.Assert(element._inElementList.Contains(this));
        element._inElementList.Remove(this);
    }

    public void AddInElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        if (_inElementList.Contains(element))
        {
            return;
        }

        _inElementList.Add(element);
        Debug.Assert(!element._outElementList.Contains(this));
        element._outElementList.Add(this);
    }

    public void RemoveInElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        if (!_inElementList.Contains(element))
        {
            return;
        }

        _inElementList.Remove(element);
        Debug.Assert(element._outElementList.Contains(this));
        element._outElementList.Remove(this);
    }

    public override string ToString()
    {
        // Out={string.Join(',', OutElementList.Select(t => $"(Value={t.Value};Id={t.Id})"))};
        // In={string.Join(',', InElementList.Select(t => $"(Value={t.Value};Id={t.Id})"))}
        return $"Value={Value} ; Id={Id};";
    }

    /// <summary>
    /// 确保两个元素在相同的一个图里面
    /// </summary>
    /// <param name="element"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private void EnsureSameMathGraph(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        if (!ReferenceEquals(MathGraph, element.MathGraph))
        {
            throw new InvalidOperationException();
        }
    }
}