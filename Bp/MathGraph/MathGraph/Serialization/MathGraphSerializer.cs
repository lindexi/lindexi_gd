using System.Diagnostics;
using System.Text.Json;

namespace MathGraph;

public class MathGraphSerializer<TElementInfo, TEdgeInfo>
{
    public MathGraphSerializer(MathGraph<TElementInfo, TEdgeInfo> mathGraph,
        IDeserializationContext? deserializationContext = null)
    {
        _mathGraph = mathGraph;
        _deserializationContext = deserializationContext ?? new DefaultDeserializationContext();
    }

    private readonly MathGraph<TElementInfo, TEdgeInfo> _mathGraph;
    private readonly IDeserializationContext _deserializationContext;

    public readonly record struct ElementSerializationContext
    (
        string Value,
        string? ElementType,
        string Id,
        int Index,
        List<int> InList,
        List<int> OutList,
        List<EdgeSerializationContext> EdgeList);

    public readonly record struct EdgeSerializationContext
    (
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

                if (!ReferenceEquals(a, element))
                {
                    // 边只序列化一次，所以只序列化入边，无论是否双向边
                    continue;
                }

                var aElementIndex = dictionary[a];
                var bElementIndex = dictionary[b];

                string edgeInfoText;
                var edgeInfo = mathGraphEdge.EdgeInfo;
                if (edgeInfo is ISerializableEdge serializableEdge)
                {
                    edgeInfoText = serializableEdge.Serialize();
                }
                else
                {
                    edgeInfoText = JsonSerializer.Serialize(edgeInfo);
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

                var edgeInfo = Deserialize<TEdgeInfo?>(edgeSerializationContext.EdgeInfo,
                    edgeSerializationContext.EdgeInfoType);

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