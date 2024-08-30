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
        ElementList = [];
    }

    public List<MathGraphElement<T>> ElementList { get; }

    public MathGraphElement<T> CreateAndAddElement(T value, string? id = null)
    {
        var element = new MathGraphElement<T>(value, id);
        ElementList.Add(element);
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
    public MathGraphElement(T value, string? id = null)
    {
        Value = value;

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

    public string Id { get; }

    public T Value { get; }

    public IReadOnlyList<MathGraphElement<T>> OutElementList => _outElementList;

    public IReadOnlyList<MathGraphElement<T>> InElementList => _inElementList;

    private static ulong _idCounter = 0;

    private readonly List<MathGraphElement<T>> _outElementList = [];
    private readonly List<MathGraphElement<T>> _inElementList = [];

    public void AddOutElement(MathGraphElement<T> element)
    {
        if (_outElementList.Contains(element))
        {
            return;
        }

        _outElementList.Add(element);
        element._inElementList.Add(this);
    }

    public void AddInElement(MathGraphElement<T> element)
    {
        if (_inElementList.Contains(element))
        {
            return;
        }

        _inElementList.Add(element);
        element._outElementList.Add(this);
    }

    public override string ToString()
    {
        return $"Value={Value} ; Id={Id};\r\nOut={string.Join(',', OutElementList.Select(t=>$"(Value={t.Value};Id={Id})"))};\r\nIn={string.Join(',', InElementList.Select(t => $"(Value={t.Value};Id={Id})"))}";
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