using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MathGraph;

public class MathGraph<TElementInfo>
{
    public MathGraph()
    {
        _elementList = [];
    }

    private readonly List<MathGraphElement<TElementInfo>> _elementList;

    public IReadOnlyList<MathGraphElement<TElementInfo>> ElementList => _elementList;

    public MathGraphElement<TElementInfo> CreateAndAddElement(TElementInfo value, string? id = null)
    {
        var element = new MathGraphElement<TElementInfo>(this, value, id);
        _elementList.Add(element);
        return element;
    }

    //public void AddElement(MathGraphElement<T> element)
    //{
    //    ElementList.Add(element);
    //}

    //public void RemoveElement(MathGraphElement<T> element)
    //{
    //    ElementList.Remove(element);
    //}

    public MathGraphSerializer<TElementInfo> GetSerializer() => new MathGraphSerializer<TElementInfo>(this);

    /// <summary>
    /// 添加单向边
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    public void AddEdge(MathGraphElement<TElementInfo> from, MathGraphElement<TElementInfo> to, IEdgeInfo? edgeInfo = null)
    {
        from.AddOutElement(to);
        Debug.Assert(to.InElementList.Contains(from));

        if (edgeInfo != null)
        {
            var edge = new MathGraphUnidirectionalEdge<TElementInfo>(from, to)
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
    public void AddBidirectionalEdge(MathGraphElement<TElementInfo> a, MathGraphElement<TElementInfo> b, IEdgeInfo? edgeInfo = null)
    {
        AddEdge(a, b);
        AddEdge(b, a);

        if (edgeInfo != null)
        {
            var edge = new MathGraphBidirectionalEdge<TElementInfo>(a, b)
            {
                EdgeInfo = edgeInfo
            };
            a.AddEdge(edge);
            //b.AddEdge(edge);
        }
    }
}

public class MathGraphSerializer<TElementInfo>
{
    public MathGraphSerializer(MathGraph<TElementInfo> mathGraph)
    {
        _mathGraph = mathGraph;
    }

    private readonly MathGraph<TElementInfo> _mathGraph;

    public readonly record struct ElementSerializationContext(string Value, string? ElementType, string Id, int Index, List<int> InList, List<int> OutList, List<EdgeSerializationContext> EdgeList);

    public readonly record struct EdgeSerializationContext(EdgeType EdgeType, int AElementIndex, int BElementIndex, string EdgeInfo, string? EdgeInfoType);

    public enum EdgeType
    {
        Unidirectional,
        Bidirectional
    }

    public string Serialize()
    {
        var elementList = _mathGraph.ElementList;

        var dictionary = new Dictionary<MathGraphElement<TElementInfo>, int>();
        for (var i = 0; i < elementList.Count; i++)
        {
            dictionary[elementList[i]] = i;
        }

        var contextList = new List<ElementSerializationContext>(elementList.Count);

        foreach (var element in elementList)
        {
            var inList = new List<int>(element.InElementList.Count);
            var outList = new List<int>(element.OutElementList.Count);

            foreach (var inElement in element.InElementList)
            {
                inList.Add(dictionary[inElement]);
            }

            foreach (var outElement in element.OutElementList)
            {
                outList.Add(dictionary[outElement]);
            }

            var edgeList = new List<EdgeSerializationContext>(element.EdgeList.Count);
            foreach (var mathGraphEdge in element.EdgeList)
            {
                EdgeType type;
                MathGraphElement<TElementInfo> a;
                MathGraphElement<TElementInfo> b;

                if (mathGraphEdge is MathGraphUnidirectionalEdge<TElementInfo> unidirectionalEdge)
                {
                    type = EdgeType.Unidirectional;
                    a = unidirectionalEdge.From;
                    b = unidirectionalEdge.To;
                }
                else if (mathGraphEdge is MathGraphBidirectionalEdge<TElementInfo> bidirectionalEdge)
                {
                    type = EdgeType.Bidirectional;
                    a = bidirectionalEdge.AElement;
                    b = bidirectionalEdge.BElement;
                }
                else
                {
                    throw new InvalidOperationException();
                }

                var aElementIndex = dictionary[a];
                var bElementIndex = dictionary[b];

                var edgeInfoText = string.Empty;
                var edgeInfo = mathGraphEdge.EdgeInfo;
                if (edgeInfo is ISerializableEdge serializableEdge)
                {
                    edgeInfoText = serializableEdge.Serialize();
                }

                var edgeSerializationContext = new EdgeSerializationContext(type, aElementIndex, bElementIndex, edgeInfoText, edgeInfo?.GetType().FullName);
                edgeList.Add(edgeSerializationContext);
            }

            string value;
            if (element.Value is ISerializableElement serializableElement)
            {
                value = serializableElement.Serialize();
            }
            else
            {
                value = JsonSerializer.Serialize(element.Value);
            }

            contextList.Add(new ElementSerializationContext(value, element.GetType().FullName, element.Id, dictionary[element], inList, outList, edgeList));
        }

        return JsonSerializer.Serialize(contextList);
    }

    public void Deserialize(string json)
    {
        var list = JsonSerializer.Deserialize<List<ElementSerializationContext>>(json);

        if (list is null)
        {
            return;
        }

        var dictionary = new Dictionary<int, MathGraphElement<TElementInfo>>();
        foreach (var serializationContext in list)
        {
            var elementType = serializationContext.ElementType;
            var value = JsonSerializer.Deserialize<TElementInfo>(serializationContext.Value);

            Debug.Assert(value is not null);

            MathGraphElement<TElementInfo> mathGraphElement = _mathGraph.CreateAndAddElement(value, serializationContext.Id);
            dictionary[serializationContext.Index] = mathGraphElement;
        }

        foreach (var serializationContext in list)
        {
            var mathGraphElement = dictionary[serializationContext.Index];
            foreach (var inIndex in serializationContext.InList)
            {
                mathGraphElement.AddInElement(dictionary[inIndex]);
            }

            foreach (var outIndex in serializationContext.OutList)
            {
                mathGraphElement.AddOutElement(dictionary[outIndex]);
            }

            foreach (var edgeSerializationContext in serializationContext.EdgeList)
            {
                MathGraphElement<TElementInfo> a = dictionary[edgeSerializationContext.AElementIndex];
                MathGraphElement<TElementInfo> b = dictionary[edgeSerializationContext.BElementIndex];
                MathGraphEdge<TElementInfo> edge;
                IEdgeInfo? edgeInfo = null;

                if (edgeSerializationContext.EdgeInfoType is not null)
                {
                    var edgeInfoType = Type.GetType(edgeSerializationContext.EdgeInfoType);
                    if (edgeInfoType is null)
                    {
                        throw new InvalidOperationException();
                    }

                    edgeInfo = (IEdgeInfo?) JsonSerializer.Deserialize(edgeSerializationContext.EdgeInfo, edgeInfoType);
                }

                if (edgeSerializationContext.EdgeType == EdgeType.Unidirectional)
                {
                    edge = new MathGraphUnidirectionalEdge<TElementInfo>(a, b)
                    {
                        EdgeInfo = edgeInfo,
                    };
                    a.AddEdge(edge);
                }
                else if (edgeSerializationContext.EdgeType == EdgeType.Bidirectional)
                {
                    edge = new MathGraphBidirectionalEdge<TElementInfo>(a, b)
                    {
                        EdgeInfo = edgeInfo,
                    };
                    a.AddEdge(edge);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
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

public class MathGraphElement<TElementInfo>
{
    public MathGraphElement(MathGraph<TElementInfo> mathGraph, TElementInfo value, string? id = null)
    {
        Value = value;
        MathGraph = mathGraph;

        if (id is null)
        {
            id = MathGraphElementIdGenerator.GenerateId();
        }

        Id = id;

        if (Value is IMathGraphElementSensitive<TElementInfo> sensitive)
        {
            Debug.Assert(sensitive.MathGraphElement is null);
            sensitive.MathGraphElement = this;
        }
    }

    public MathGraph<TElementInfo> MathGraph { get; }

    public string Id { get; }

    public TElementInfo Value { get; }

    public IReadOnlyList<MathGraphElement<TElementInfo>> OutElementList => _outElementList;

    public IReadOnlyList<MathGraphElement<TElementInfo>> InElementList => _inElementList;

    public IReadOnlyList<MathGraphEdge<TElementInfo>> EdgeList => _edgeList;

    private readonly List<MathGraphElement<TElementInfo>> _outElementList = [];
    private readonly List<MathGraphElement<TElementInfo>> _inElementList = [];

    private readonly List<MathGraphEdge<TElementInfo>> _edgeList = [];

    public void AddEdge(MathGraphEdge<TElementInfo> edge)
    {
        edge.EnsureContain(this);

        _edgeList.Add(edge);

        var otherElement = edge.GetOtherElement(this);
        otherElement._edgeList.Add(edge);
    }

    public void AddOutElement(MathGraphElement<TElementInfo> element)
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

    public void RemoveOutElement(MathGraphElement<TElementInfo> element)
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

    public void AddInElement(MathGraphElement<TElementInfo> element)
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

    public void RemoveInElement(MathGraphElement<TElementInfo> element)
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
        return $"Value={Value} ; Id={Id};\r\nOut={string.Join(',', OutElementList.Select(t => $"(Value={t.Value};Id={t.Id})"))};\r\nIn={string.Join(',', InElementList.Select(t => $"(Value={t.Value};Id={t.Id})"))}";
    }

    private void EnsureSameMathGraph(MathGraphElement<TElementInfo> element)
    {
        if (!ReferenceEquals(MathGraph, element.MathGraph))
        {
            throw new InvalidOperationException();
        }
    }
}

public interface IEdgeInfo
{
}

public class MathGraphUnidirectionalEdge<TElementInfo> : MathGraphEdge<TElementInfo>
{
    public MathGraphUnidirectionalEdge(MathGraphElement<TElementInfo> from, MathGraphElement<TElementInfo> to) : base(from, to)
    {
    }

    public MathGraphElement<TElementInfo> From => base.A;

    public MathGraphElement<TElementInfo> To => base.B;
}

public class MathGraphBidirectionalEdge<TElementInfo> : MathGraphEdge<TElementInfo>
{
    public MathGraphBidirectionalEdge(MathGraphElement<TElementInfo> a, MathGraphElement<TElementInfo> b) : base(a, b)
    {
    }

    public MathGraphElement<TElementInfo> AElement => base.A;

    public MathGraphElement<TElementInfo> BElement => base.B;
}

public abstract class MathGraphEdge<TElementInfo>
{
    protected MathGraphEdge(MathGraphElement<TElementInfo> a, MathGraphElement<TElementInfo> b)
    {
        A = a;
        B = b;
    }

    public IEdgeInfo? EdgeInfo { get; set; }

    protected MathGraphElement<TElementInfo> A { get; }
    protected MathGraphElement<TElementInfo> B { get; }

    public void EnsureContain(MathGraphElement<TElementInfo> element)
    {
        if (!ReferenceEquals(element, A) && !ReferenceEquals(element, B))
        {
            throw new InvalidOperationException();
        }
    }

    public MathGraphElement<TElementInfo> GetOtherElement(MathGraphElement<TElementInfo> element)
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