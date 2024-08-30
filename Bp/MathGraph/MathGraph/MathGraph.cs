using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MathGraph;

public class MathGraph<T>
{
    public MathGraph()
    {
        _elementList = [];
    }

    private readonly List<MathGraphElement<T>> _elementList;

    public IReadOnlyList<MathGraphElement<T>> ElementList => _elementList;

    public MathGraphElement<T> CreateAndAddElement(T value, string? id = null)
    {
        var element = new MathGraphElement<T>(this, value, id);
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

    public MathGraphSerializer<T> GetSerializer() => new MathGraphSerializer<T>(this);

    /// <summary>
    /// 添加单向边
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    public void AddEdge(MathGraphElement<T> from, MathGraphElement<T> to)
    {
        from.AddOutElement(to);
        Debug.Assert(to.InElementList.Contains(from));
    }

    /// <summary>
    /// 添加双向边
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public void AddBidirectionalEdge(MathGraphElement<T> a, MathGraphElement<T> b)
    {
        AddEdge(a, b);
        AddEdge(b, a);
    }
}

public class MathGraphSerializer<T>
{
    public MathGraphSerializer(MathGraph<T> mathGraph)
    {
        _mathGraph = mathGraph;
    }

    private readonly MathGraph<T> _mathGraph;

    public readonly record struct SerializationContext(string Value, string? ElementType, string Id, int Index, List<int> InList, List<int> OutList);

    public string Serialize()
    {
        var elementList = _mathGraph.ElementList;

        var dictionary = new Dictionary<MathGraphElement<T>, int>();
        for (var i = 0; i < elementList.Count; i++)
        {
            dictionary[elementList[i]] = i;
        }

        var contextList = new List<SerializationContext>(elementList.Count);

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

            string value;
            if (element.Value is ISerializableElement serializableElement)
            {
                value = serializableElement.Serialize();
            }
            else
            {
                value = JsonSerializer.Serialize(element.Value);
            }

            contextList.Add(new SerializationContext(value, element.GetType().FullName, element.Id, dictionary[element], inList, outList));
        }

        return JsonSerializer.Serialize(contextList);
    }

    public void Deserialize(string json)
    {
        var list = JsonSerializer.Deserialize<List<SerializationContext>>(json);

        if (list is null)
        {
            return;
        }

        var dictionary = new Dictionary<int, MathGraphElement<T>>();
        foreach (var serializationContext in list)
        {
            var elementType = serializationContext.ElementType;
            var value = JsonSerializer.Deserialize<T>(serializationContext.Value);

            Debug.Assert(value is not null);

            MathGraphElement<T> mathGraphElement = _mathGraph.CreateAndAddElement(value, serializationContext.Id);
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
        }
    }
}

public class MathGraphElement<T>
{
    public MathGraphElement(MathGraph<T> mathGraph, T value, string? id = null)
    {
        Value = value;
        MathGraph = mathGraph;

        if (id is null)
        {
            var idCounter = Interlocked.Increment(ref _idCounter);
            id = idCounter.ToString();
        }

        Id = id;

        if (Value is IMathGraphElementSensitive<T> sensitive)
        {
            sensitive.MathGraphElement = this;
        }
    }

    public MathGraph<T> MathGraph { get; }

    public string Id { get; }

    public T Value { get; }

    public IReadOnlyList<MathGraphElement<T>> OutElementList => _outElementList;

    public IReadOnlyList<MathGraphElement<T>> InElementList => _inElementList;

    private static ulong _idCounter = 0;

    private readonly List<MathGraphElement<T>> _outElementList = [];
    private readonly List<MathGraphElement<T>> _inElementList = [];

    public void AddOutElement(MathGraphElement<T> element)
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

    public void AddInElement(MathGraphElement<T> element)
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

    public override string ToString()
    {
        return $"Value={Value} ; Id={Id};\r\nOut={string.Join(',', OutElementList.Select(t => $"(Value={t.Value};Id={t.Id})"))};\r\nIn={string.Join(',', InElementList.Select(t => $"(Value={t.Value};Id={t.Id})"))}";
    }

    private void EnsureSameMathGraph(MathGraphElement<T> element)
    {
        if (!ReferenceEquals(MathGraph, element.MathGraph))
        {
            throw new InvalidOperationException();
        }
    }
}

public class MathGraphEdge<T>
{
    public MathGraphEdge(MathGraphElement<T> from, MathGraphElement<T> to)
    {
        From = from;
        To = to;

        //from.AddOutElement(to);
        //to.AddInElement(from);
    }

    public MathGraphElement<T> From { get; }

    public MathGraphElement<T> To { get; }
}