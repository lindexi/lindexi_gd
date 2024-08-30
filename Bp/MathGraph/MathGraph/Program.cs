using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathGraph;

internal class Program
{
    private static void Main(string[] args)
    {
        AddBidirectionalEdge();

        AddEdge();

        Serialize();
    }

    private static void AddBidirectionalEdge()
    {
        var mathGraph = new MathGraph<string>();
        var a = mathGraph.CreateAndAddElement("a");
        var b = mathGraph.CreateAndAddElement("b");

        mathGraph.AddBidirectionalEdge(a, b);

        Debug.Assert(a.OutElementList.Contains(b));
        Debug.Assert(b.InElementList.Contains(a));
        Debug.Assert(a.InElementList.Contains(b));
        Debug.Assert(b.OutElementList.Contains(a));
        SerializeDeserialize(mathGraph);
    }

    private static void AddEdge()
    {
        var mathGraph = new MathGraph<string>();
        var a = mathGraph.CreateAndAddElement("a");
        var b = mathGraph.CreateAndAddElement("b");

        mathGraph.AddEdge(a, b);

        Debug.Assert(a.OutElementList.Contains(b));
        Debug.Assert(b.InElementList.Contains(a));

        SerializeDeserialize(mathGraph);
    }

    private static void Serialize()
    {
        var mathGraph = new MathGraph<int>();

        List<MathGraphElement<int>> elementList = [];

        for (int i = 0; i < 10; i++)
        {
            var mathGraphElement = mathGraph.CreateAndAddElement(i);
            elementList.Add(mathGraphElement);
        }

        for (int i = 0; i < 10; i++)
        {
            var mathGraphElement = elementList[i];
            for (int j = 0; j < 10; j++)
            {
                if (i != j)
                {
                    mathGraphElement.AddInElement(elementList[j]);
                }
            }
        }

        SerializeDeserialize(mathGraph);
    }

    private static void SerializeDeserialize<T>(MathGraph<T> mathGraph)
    {
        var mathGraphSerializer = mathGraph.GetSerializer();
        var json = mathGraphSerializer.Serialize();
        Equals(mathGraph, Deserialize<T>(json));
    }

    private static MathGraph<T> Deserialize<T>(string json)
    {
        var mathGraph = new MathGraph<T>();
        var mathGraphSerializer = mathGraph.GetSerializer();
        mathGraphSerializer.Deserialize(json);
        return mathGraph;
    }

    private static void Equals<T>(MathGraph<T> a, MathGraph<T> b)
    {
        Debug.Assert(a.ElementList.Count == b.ElementList.Count);
        for (var i = 0; i < a.ElementList.Count; i++)
        {
            var elementA = a.ElementList[i];
            var elementB = b.ElementList[i];

            ElementEquals(elementA, elementB);

            ElementListEquals(elementA.InElementList, elementB.InElementList);
            ElementListEquals(elementA.OutElementList, elementB.OutElementList);
        }

        static void ElementListEquals(IReadOnlyList<MathGraphElement<T>> a, IReadOnlyList<MathGraphElement<T>> b)
        {
            Debug.Assert(a.Count == b.Count);
            for (var i = 0; i < a.Count; i++)
            {
                ElementEquals(a[i], b[i]);
            }
        }

        static void ElementEquals(MathGraphElement<T> a, MathGraphElement<T> b)
        {
            Debug.Assert(EqualityComparer<T>.Default.Equals(a.Value, b.Value));
            Debug.Assert(a.Id == b.Id);
        }
    }
}