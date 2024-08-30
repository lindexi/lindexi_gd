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

        var mathGraphSerializer = mathGraph.GetSerializer();
        var json = mathGraphSerializer.Serialize();
        Equals(mathGraph, Deserialize(json));
    }

    private static MathGraph<int> Deserialize(string json)
    {
        var mathGraph = new MathGraph<int>();
        var mathGraphSerializer = mathGraph.GetSerializer();
        mathGraphSerializer.Deserialize(json);
        return mathGraph;
    }

    private static void Equals<T>(MathGraph<T> a, MathGraph<T> b)
    {
        Debug.Assert(a.ElementList == b.ElementList);
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
