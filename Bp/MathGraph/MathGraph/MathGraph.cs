using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathGraph;

public class MathGraph<TElementInfo, TEdgeInfo>
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

    public MathGraphSerializer<TElementInfo, TEdgeInfo> GetSerializer() =>
        new MathGraphSerializer<TElementInfo, TEdgeInfo>(this);

    /// <summary>
    /// 添加单向边
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
}

static class MathGraphElementIdGenerator
{
    private static ulong _idCounter = 0;

    public static string GenerateId()
    {
        return Interlocked.Increment(ref _idCounter).ToString();
    }
}

public class MathGraphElement<TElementInfo, TEdgeInfo>
{
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

    public MathGraph<TElementInfo, TEdgeInfo> MathGraph { get; }

    public string Id { get; }

    public TElementInfo Value { get; }

    public IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> OutElementList => _outElementList;

    public IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> InElementList => _inElementList;

    public IReadOnlyList<MathGraphEdge<TElementInfo, TEdgeInfo>> EdgeList => _edgeList;

    private readonly List<MathGraphElement<TElementInfo, TEdgeInfo>> _outElementList = [];
    private readonly List<MathGraphElement<TElementInfo, TEdgeInfo>> _inElementList = [];

    private readonly List<MathGraphEdge<TElementInfo, TEdgeInfo>> _edgeList = [];

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
        return
            $"Value={Value} ; Id={Id};\r\nOut={string.Join(',', OutElementList.Select(t => $"(Value={t.Value};Id={t.Id})"))};\r\nIn={string.Join(',', InElementList.Select(t => $"(Value={t.Value};Id={t.Id})"))}";
    }

    private void EnsureSameMathGraph(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        if (!ReferenceEquals(MathGraph, element.MathGraph))
        {
            throw new InvalidOperationException();
        }
    }
}

public class MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> : MathGraphEdge<TElementInfo, TEdgeInfo>
{
    public MathGraphUnidirectionalEdge(MathGraphElement<TElementInfo, TEdgeInfo> from,
        MathGraphElement<TElementInfo, TEdgeInfo> to) : base(from, to)
    {
    }

    public MathGraphElement<TElementInfo, TEdgeInfo> From => base.A;

    public MathGraphElement<TElementInfo, TEdgeInfo> To => base.B;
}

public class MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo> : MathGraphEdge<TElementInfo, TEdgeInfo>
{
    public MathGraphBidirectionalEdge(MathGraphElement<TElementInfo, TEdgeInfo> a,
        MathGraphElement<TElementInfo, TEdgeInfo> b) : base(a, b)
    {
    }

    public MathGraphElement<TElementInfo, TEdgeInfo> AElement => base.A;

    public MathGraphElement<TElementInfo, TEdgeInfo> BElement => base.B;
}

public abstract class MathGraphEdge<TElementInfo, TEdgeInfo>
{
    protected MathGraphEdge(MathGraphElement<TElementInfo, TEdgeInfo> a, MathGraphElement<TElementInfo, TEdgeInfo> b)
    {
        A = a;
        B = b;
    }

    public TEdgeInfo? EdgeInfo { get; set; }

    protected MathGraphElement<TElementInfo, TEdgeInfo> A { get; }
    protected MathGraphElement<TElementInfo, TEdgeInfo> B { get; }

    public void EnsureContain(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        if (!ReferenceEquals(element, A) && !ReferenceEquals(element, B))
        {
            throw new InvalidOperationException();
        }
    }

    public MathGraphElement<TElementInfo, TEdgeInfo> GetOtherElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        if (ReferenceEquals(element, A))
        {
            return B;
        }

        if (ReferenceEquals(element, B))
        {
            return A;
        }

        throw new InvalidOperationException();
    }
}