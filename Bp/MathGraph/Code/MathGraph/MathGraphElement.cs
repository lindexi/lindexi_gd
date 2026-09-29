using System.Diagnostics;

namespace MathGraphs;

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
    /// 从当前元素出发的边列表
    /// </summary>
    public IReadOnlyList<MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo>> OutEdgeList => _outEdgeList;

    /// <summary>
    /// 指向当前元素的边列表
    /// </summary>
    public IReadOnlyList<MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo>> InEdgeList => _inEdgeList;

    /// <summary>
    /// 出度的元素列表，包含所有从当前元素出发的边指向的元素
    /// </summary>
    public IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> OutElementList => GetOutElementList();

    /// <summary>
    /// 入度的元素列表，包含所有指向当前元素的边的起点元素
    /// </summary>
    public IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> InElementList => GetInElementList();

    public IReadOnlyList<MathGraphEdge<TElementInfo, TEdgeInfo>> EdgeList => _edgeList;

    private readonly List<MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo>> _outEdgeList = [];
    private readonly List<MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo>> _inEdgeList = [];
    private readonly List<MathGraphEdge<TElementInfo, TEdgeInfo>> _edgeList = [];

    /// <summary>
    /// 添加边关系
    /// </summary>
    /// <param name="edge"></param>
    public void AddEdge(MathGraphEdge<TElementInfo, TEdgeInfo> edge)
    {
        if (edge is MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> unidirectionalEdge)
        {
            if (ReferenceEquals(unidirectionalEdge.From, this))
            {
                AddOutEdge(unidirectionalEdge);
                return;
            }

            if (ReferenceEquals(unidirectionalEdge.To, this))
            {
                AddInEdge(unidirectionalEdge);
                return;
            }

            throw new InvalidOperationException();
        }

        edge.EnsureContain(this);
        var otherElement = edge.GetOtherElement(this);
        if (ContainsReference(_edgeList, edge))
        {
            return;
        }

        _edgeList.Add(edge);
        if (!ContainsReference(otherElement._edgeList, edge))
        {
            otherElement._edgeList.Add(edge);
        }
    }

    public void AddOutEdge(MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> edge)
    {
        if (!ReferenceEquals(edge.From, this))
        {
            throw new InvalidOperationException();
        }

        EnsureSameMathGraph(edge.To);
        if (ContainsReference(_outEdgeList, edge))
        {
            return;
        }

        _outEdgeList.Add(edge);
        if (!ContainsReference(_edgeList, edge))
        {
            _edgeList.Add(edge);
        }

        if (!ContainsReference(edge.To._inEdgeList, edge))
        {
            edge.To._inEdgeList.Add(edge);
        }

        if (!ContainsReference(edge.To._edgeList, edge))
        {
            edge.To._edgeList.Add(edge);
        }
    }

    public void AddInEdge(MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> edge)
    {
        if (!ReferenceEquals(edge.To, this))
        {
            throw new InvalidOperationException();
        }

        EnsureSameMathGraph(edge.From);
        if (ContainsReference(_inEdgeList, edge))
        {
            return;
        }

        _inEdgeList.Add(edge);
        if (!ContainsReference(_edgeList, edge))
        {
            _edgeList.Add(edge);
        }

        if (!ContainsReference(edge.From._outEdgeList, edge))
        {
            edge.From._outEdgeList.Add(edge);
        }

        if (!ContainsReference(edge.From._edgeList, edge))
        {
            edge.From._edgeList.Add(edge);
        }
    }

    /// <summary>
    /// 添加出度的元素关系，即从当前元素出发创建一条指向目标元素的边
    /// </summary>
    /// <param name="element"></param>
    public void AddOutElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        if (OutElementList.Contains(element))
        {
            return;
        }

        AddOutEdge(new MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo>(this, element));
    }

    /// <summary>
    /// 删除出度的元素关系，即从当前元素出发的边指向的元素
    /// </summary>
    /// <param name="element"></param>
    public void RemoveOutElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        var removed = false;
        for (var i = _outEdgeList.Count - 1; i >= 0; i--)
        {
            var edge = _outEdgeList[i];
            if (!ReferenceEquals(edge.To, element))
            {
                continue;
            }

            RemoveOutEdge(edge);
            removed = true;
        }

        if (!removed)
        {
            return;
        }
    }

    /// <summary>
    /// 添加入度的元素关系，即从指定元素创建一条指向当前元素的边
    /// </summary>
    /// <param name="element"></param>
    public void AddInElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        if (InElementList.Contains(element))
        {
            return;
        }

        AddInEdge(new MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo>(element, this));
    }

    /// <summary>
    /// 删除入度的元素关系，即指向当前元素的边的起点元素
    /// </summary>
    /// <param name="element"></param>
    public void RemoveInElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        var removed = false;
        for (var i = _inEdgeList.Count - 1; i >= 0; i--)
        {
            var edge = _inEdgeList[i];
            if (!ReferenceEquals(edge.From, element))
            {
                continue;
            }

            RemoveInEdge(edge);
            removed = true;
        }

        if (!removed)
        {
            return;
        }
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

    private void RemoveOutEdge(MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> edge)
    {
        _outEdgeList.Remove(edge);
        _edgeList.Remove(edge);
        edge.To._inEdgeList.Remove(edge);
        edge.To._edgeList.Remove(edge);
    }

    private void RemoveInEdge(MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> edge)
    {
        _inEdgeList.Remove(edge);
        _edgeList.Remove(edge);
        edge.From._outEdgeList.Remove(edge);
        edge.From._edgeList.Remove(edge);
    }

    private List<MathGraphElement<TElementInfo, TEdgeInfo>> GetOutElementList()
    {
        var list = new List<MathGraphElement<TElementInfo, TEdgeInfo>>(_outEdgeList.Count);
        foreach (var edge in _outEdgeList)
        {
            if (!list.Contains(edge.To))
            {
                list.Add(edge.To);
            }
        }

        return list;
    }

    private List<MathGraphElement<TElementInfo, TEdgeInfo>> GetInElementList()
    {
        var list = new List<MathGraphElement<TElementInfo, TEdgeInfo>>(_inEdgeList.Count);
        foreach (var edge in _inEdgeList)
        {
            if (!list.Contains(edge.From))
            {
                list.Add(edge.From);
            }
        }

        return list;
    }

    private static bool ContainsReference<T>(List<T> list, T item)
        where T : class
    {
        foreach (var current in list)
        {
            if (ReferenceEquals(current, item))
            {
                return true;
            }
        }

        return false;
    }
}