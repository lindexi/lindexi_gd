using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
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

    //public void AddElement(MathGraphElement<T> element)
    //{
    //    ElementList.Add(element);
    //}

    //public void RemoveElement(MathGraphElement<T> element)
    //{
    //    ElementList.Remove(element);
    //}

    public MathGraphSerializer<TElementInfo, TEdgeInfo> GetSerializer() =>
        new MathGraphSerializer<TElementInfo, TEdgeInfo>(this);

    /// <summary>
    /// 添加单向边
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
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

public interface IDeserializationContext
{
    bool TryDeserialize(string value, string? type, [NotNullWhen(true)] out object? result);
}

class DefaultDeserializationContext : IDeserializationContext
{
    public bool TryDeserialize(string value, string? type, out object? result)
    {
        result = null;
        return false;
    }
}

public class MathGraphSerializer<TElementInfo, TEdgeInfo>
{
    public MathGraphSerializer(MathGraph<TElementInfo, TEdgeInfo> mathGraph, IDeserializationContext? deserializationContext = null)
    {
        _mathGraph = mathGraph;
        _deserializationContext = deserializationContext ?? new DefaultDeserializationContext();
    }

    private readonly MathGraph<TElementInfo, TEdgeInfo> _mathGraph;
    private IDeserializationContext _deserializationContext;

    public readonly record struct ElementSerializationContext(
        string Value,
        string? ElementType,
        string Id,
        int Index,
        List<int> InList,
        List<int> OutList,
        List<EdgeSerializationContext> EdgeList);

    public readonly record struct EdgeSerializationContext(
        EdgeType EdgeType,
        int AElementIndex,
        int BElementIndex,
        string EdgeInfo,
        string? EdgeInfoType);

    public enum EdgeType
    {
        Unidirectional,
        Bidirectional
    }

    public string Serialize()
    {
        var elementList = _mathGraph.ElementList;

        var dictionary = new Dictionary<MathGraphElement<TElementInfo, TEdgeInfo>, int>();
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
                MathGraphElement<TElementInfo, TEdgeInfo> a;
                MathGraphElement<TElementInfo, TEdgeInfo> b;

                if (mathGraphEdge is MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> unidirectionalEdge)
                {
                    type = EdgeType.Unidirectional;
                    a = unidirectionalEdge.From;
                    b = unidirectionalEdge.To;
                }
                else if (mathGraphEdge is MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo> bidirectionalEdge)
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

                var edgeSerializationContext = new EdgeSerializationContext(type, aElementIndex, bElementIndex,
                    edgeInfoText, edgeInfo?.GetType().FullName);
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

            contextList.Add(new ElementSerializationContext(value, element.Value?.GetType().FullName, element.Id,
                dictionary[element], inList, outList, edgeList));
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

        var dictionary = new Dictionary<int, MathGraphElement<TElementInfo, TEdgeInfo>>();
        foreach (var serializationContext in list)
        {
            var elementType = serializationContext.ElementType;
            var value = Deserialize<TElementInfo>(serializationContext.Value, elementType);

            Debug.Assert(value is not null);

            MathGraphElement<TElementInfo, TEdgeInfo> mathGraphElement =
                _mathGraph.CreateAndAddElement(value, serializationContext.Id);
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
                MathGraphElement<TElementInfo, TEdgeInfo> a = dictionary[edgeSerializationContext.AElementIndex];
                MathGraphElement<TElementInfo, TEdgeInfo> b = dictionary[edgeSerializationContext.BElementIndex];
                MathGraphEdge<TElementInfo, TEdgeInfo> edge;

                var edgeInfo = Deserialize<TEdgeInfo?>(edgeSerializationContext.EdgeInfo, edgeSerializationContext.EdgeInfoType);

                if (edgeSerializationContext.EdgeType == EdgeType.Unidirectional)
                {
                    edge = new MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo>(a, b)
                    {
                        EdgeInfo = edgeInfo,
                    };
                    a.AddEdge(edge);
                }
                else if (edgeSerializationContext.EdgeType == EdgeType.Bidirectional)
                {
                    edge = new MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo>(a, b)
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

    private T Deserialize<T>(string value, string? type)
    {
        if (_deserializationContext.TryDeserialize(value, type, out var result))
        {
            return (T) result;
        }

        Type? returnType = null;
        if (type is not null)
        {
            returnType = Type.GetType(type);
        }

        if (returnType is null)
        {
            returnType = typeof(T);
        }

        return (T) JsonSerializer.Deserialize(value, returnType)!;
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