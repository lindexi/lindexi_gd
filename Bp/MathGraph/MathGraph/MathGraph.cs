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

    public List<MathGraphElement<T>> ElementList { get; private set; }

    public void AddElement(MathGraphElement<T> element)
    {
        ElementList.Add(element);
    }

    public void RemoveElement(MathGraphElement<T> element)
    {
        ElementList.Remove(element);
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(ElementList);
    }

    public void Deserialize(string json)
    {
        ElementList = JsonSerializer.Deserialize<List<MathGraphElement<T>>>(json);
    }
}

public class MathGraphElement<T>
{
    public MathGraphElement(T value)
    {
        Value = value;

        if (Value is IMathGraphElementSensitive<T> sensitive)
        {
            sensitive.MathGraphElement = this;
        }
    }

    public T Value { get; }

    public List<MathGraphElement<T>> OutElementList { get; } = [];

    public List<MathGraphElement<T>> InElementList { get; } = [];
}