using System.Diagnostics;
using MathGraphs.Serialization;

namespace MathGraphs;

public class MathGraph<TElementInfo, TEdgeInfo> : ISerializableElement
{
    public MathGraph()
    {
        _elementList = [];
    }

    private readonly List<MathGraphElement<TElementInfo, TEdgeInfo>> _elementList;

    public IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> ElementList => _elementList;

    public MathGraphElement<TElementInfo, TEdgeInfo> CreateAndAddElement(TElementInfo value, string? id = null)
    {
        var element = new MathGraphElement<TElementInfo, TEdgeInfo>(this, value, id);
        _elementList.Add(element);
        return element;
    }

    public MathGraphSerializer<TElementInfo, TEdgeInfo> GetSerializer(IDeserializationContext? deserializationContext = null) =>
        new MathGraphSerializer<TElementInfo, TEdgeInfo>(this, deserializationContext);

    /// <summary>
    /// 添加单向边，添加边的同时，会添加元素关系
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="edgeInfo"></param>
    public void AddEdge(MathGraphElement<TElementInfo, TEdgeInfo> from, MathGraphElement<TElementInfo, TEdgeInfo> to,
        TEdgeInfo? edgeInfo = default)
    {
        from.AddOutElement(to);
        Debug.Assert(to.InElementList.Contains(from));

        if (edgeInfo != null)
        {
            var edge = new MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo>(from, to)
            {
                EdgeInfo = edgeInfo
            };
            from.AddEdge(edge);
        }
    }

    /// <summary>
    /// 添加双向边
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="edgeInfo"></param>
    public void AddBidirectionalEdge(MathGraphElement<TElementInfo, TEdgeInfo> a,
        MathGraphElement<TElementInfo, TEdgeInfo> b, TEdgeInfo? edgeInfo = default)
    {
        AddEdge(a, b);
        AddEdge(b, a);

        if (edgeInfo != null)
        {
            var edge = new MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo>(a, b)
            {
                EdgeInfo = edgeInfo
            };
            a.AddEdge(edge);
            //b.AddEdge(edge);
        }
    }

    string ISerializableElement.Serialize()
    {
        var mathGraphSerializer = GetSerializer();
        return mathGraphSerializer.Serialize();
    }

    internal void StartDeserialize(int elementCount)
    {
        _elementList.Clear();
        EnsureElementCapacity(elementCount);
    }

    /// <summary>
    /// 序列化使用设置元素的大小
    /// </summary>
    /// <param name="capacity"></param>
    private void EnsureElementCapacity(int capacity)
    {
        _elementList.EnsureCapacity(capacity);
    }
}