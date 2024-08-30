using System;
using System.Collections.Generic;
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



    public string Serialize()
    {
       

        return JsonSerializer.Serialize(ElementList);
    }

    public void Deserialize(string json)
    {
        ElementList.Clear();
        var list = JsonSerializer.Deserialize<List<MathGraphElement<T>>>(json);
        if (list != null)
        {
            ElementList.AddRange(list);
        }
    }


}

public class MathGraphSerializer<T>
{
    public MathGraphSerializer(MathGraph<T> mathGraph)
    {
        _mathGraph = mathGraph;
    }

    private readonly MathGraph<T> _mathGraph;

    public readonly record struct SerializationContext(T Value, string Id, int Index, List<int> InList, List<int> OutList);

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
            List<int> inList = new List<int>(element.InElementList.Count);
            List<int> outList = new List<int>(element.OutElementList.Count);

            foreach (var inElement in element.InElementList)
            {
                inList.Add(dictionary[inElement]);
            }

            foreach (var outElement in element.OutElementList)
            {
                outList.Add(dictionary[outElement]);
            }

            contextList.Add(new SerializationContext(element.Value, element.Id, dictionary[element], inList, outList));
        }

        return JsonSerializer.Serialize(contextList);
    }

    public void Deserialize(string json)
    {

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

    public List<MathGraphElement<T>> OutElementList { get; } = [];

    public List<MathGraphElement<T>> InElementList { get; } = [];

    private static ulong _idCounter = 0;
}

public class MathGraphEdge<T>
{
    public MathGraphEdge(MathGraphElement<T> from, MathGraphElement<T> to)
    {
        From = from;
        To = to;

        from.OutElementList.Add(to);
        to.InElementList.Add(from);
    }

    public MathGraphElement<T> From { get; }

    public MathGraphElement<T> To { get; }
}