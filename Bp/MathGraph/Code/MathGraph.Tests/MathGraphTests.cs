using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraph.Tests;

[TestClass]
public class MathGraphTests
{
    /// <summary>
    /// 验证嵌套图通过自定义上下文完成序列化往返。
    /// </summary>
    [TestMethod]
    public void AddSuperPoint()
    {
        var graph = new MathGraph<MathGraph<string, string>, int>();
        var a = new MathGraph<string, string>();
        a.CreateAndAddElement("aaa");
        var b = new MathGraph<string, string>();
        b.CreateAndAddElement("bbb");
        graph.CreateAndAddElement(a);
        graph.CreateAndAddElement(b);

        AssertRoundTrip(graph, new SuperPointDeserializationContext());
    }

    /// <summary>
    /// 验证带信息的边在序列化往返后保持不变。
    /// </summary>
    [TestMethod]
    public void SerializeEdge()
    {
        var graph = CreateChain("ab", "bc");

        AssertRoundTrip(graph);
    }

    /// <summary>
    /// 验证不带边信息的关系在序列化往返后保持不变。
    /// </summary>
    [TestMethod]
    public void SerializeLink()
    {
        var graph = CreateChain(null, null);

        AssertRoundTrip(graph);
    }

    /// <summary>
    /// 验证双向关系在序列化往返后保持不变。
    /// </summary>
    [TestMethod]
    public void AddBidirectionalEdge()
    {
        var graph = new MathGraph<string, string>();
        var a = graph.CreateAndAddElement("a");
        var b = graph.CreateAndAddElement("b");
        graph.AddBidirectionalEdge(a, b);

        AssertRoundTrip(graph);
    }

    /// <summary>
    /// 验证单向关系在序列化往返后保持不变。
    /// </summary>
    [TestMethod]
    public void AddEdge()
    {
        var graph = new MathGraph<string, string>();
        var a = graph.CreateAndAddElement("a");
        var b = graph.CreateAndAddElement("b");
        graph.AddEdge(a, b);

        AssertRoundTrip(graph);
    }

    /// <summary>
    /// 验证十个节点的完全图在序列化往返后保持不变。
    /// </summary>
    [TestMethod]
    public void Serialize()
    {
        var graph = CreateCompleteGraph();

        AssertRoundTrip(graph);
    }

    /// <summary>
    /// 验证链式图的各个入出关系。
    /// </summary>
    [DataTestMethod]
    [DataRow("ab", "bc", 0)]
    [DataRow("ab", "bc", 1)]
    [DataRow("ab", "bc", 2)]
    [DataRow("ab", "bc", 3)]
    [DataRow(null, null, 0)]
    [DataRow(null, null, 1)]
    [DataRow(null, null, 2)]
    [DataRow(null, null, 3)]
    public void ChainContainsExpectedRelationship(string? firstEdge, string? secondEdge, int relationship)
    {
        var graph = CreateChain(firstEdge, secondEdge);
        var a = graph.ElementList[0];
        var b = graph.ElementList[1];
        var c = graph.ElementList[2];
        var relationships = new[]
        {
            (Actual: a.OutElementList[0], Expected: b),
            (Actual: b.OutElementList[0], Expected: c),
            (Actual: c.InElementList[0], Expected: b),
            (Actual: b.InElementList[0], Expected: a)
        };

        Assert.AreSame(relationships[relationship].Expected, relationships[relationship].Actual);
    }

    /// <summary>
    /// 验证单向边仅建立指定方向的关系。
    /// </summary>
    [DataTestMethod]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(3, false)]
    public void UnidirectionalEdgeContainsExpectedRelationship(int relationship, bool expected)
    {
        var graph = new MathGraph<string, string>();
        var a = graph.CreateAndAddElement("a");
        var b = graph.CreateAndAddElement("b");

        graph.AddEdge(a, b);
        var relationships = new[]
        {
            a.OutElementList.Contains(b), b.InElementList.Contains(a),
            a.InElementList.Contains(b), b.OutElementList.Contains(a)
        };

        Assert.AreEqual(expected, relationships[relationship]);
    }

    /// <summary>
    /// 验证双向边建立两个方向的关系。
    /// </summary>
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void BidirectionalEdgeContainsExpectedRelationship(int relationship)
    {
        var graph = new MathGraph<string, string>();
        var a = graph.CreateAndAddElement("a");
        var b = graph.CreateAndAddElement("b");

        graph.AddBidirectionalEdge(a, b);
        var relationships = new[]
        {
            a.OutElementList.Contains(b), b.InElementList.Contains(a),
            a.InElementList.Contains(b), b.OutElementList.Contains(a)
        };

        Assert.IsTrue(relationships[relationship]);
    }

    private static MathGraph<string, string> CreateChain(string? firstEdge, string? secondEdge)
    {
        var graph = new MathGraph<string, string>();
        var a = graph.CreateAndAddElement("a");
        var b = graph.CreateAndAddElement("b");
        var c = graph.CreateAndAddElement("c");
        graph.AddEdge(a, b, firstEdge);
        graph.AddEdge(b, c, secondEdge);
        return graph;
    }

    private static MathGraph<int, string> CreateCompleteGraph()
    {
        var graph = new MathGraph<int, string>();
        var elements = Enumerable.Range(0, 10).Select(value => graph.CreateAndAddElement(value)).ToArray();
        foreach (var element in elements)
        {
            foreach (var other in elements.Where(other => other != element))
            {
                element.AddInElement(other);
            }
        }
        return graph;
    }

    private static void AssertRoundTrip<TElementInfo, TEdgeInfo>(MathGraph<TElementInfo, TEdgeInfo> expected,
        IDeserializationContext? context = null)
    {
        var json = expected.GetSerializer().Serialize();
        var actual = new MathGraph<TElementInfo, TEdgeInfo>();
        actual.GetSerializer(context).Deserialize(json);

        AssertGraphEqual(expected, actual);
    }

    private static void AssertGraphEqual<TElementInfo, TEdgeInfo>(MathGraph<TElementInfo, TEdgeInfo> expected,
        MathGraph<TElementInfo, TEdgeInfo> actual)
    {
        Assert.AreEqual(expected.ElementList.Count, actual.ElementList.Count);
        for (var i = 0; i < expected.ElementList.Count; i++)
        {
            var a = expected.ElementList[i];
            var b = actual.ElementList[i];
            AssertElementEqual(a, b);
            AssertElementListEqual(a.InElementList, b.InElementList);
            AssertElementListEqual(a.OutElementList, b.OutElementList);
            Assert.AreEqual(a.EdgeList.Count, b.EdgeList.Count);
            for (var j = 0; j < a.EdgeList.Count; j++)
            {
                AssertEdgeEqual(a.EdgeList[j], b.EdgeList[j]);
            }
        }
    }

    private static void AssertElementListEqual<TElementInfo, TEdgeInfo>(
        IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> expected,
        IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> actual)
    {
        Assert.AreEqual(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            AssertElementEqual(expected[i], actual[i]);
        }
    }

    private static void AssertElementEqual<TElementInfo, TEdgeInfo>(MathGraphElement<TElementInfo, TEdgeInfo> expected,
        MathGraphElement<TElementInfo, TEdgeInfo> actual)
    {
        Assert.AreEqual(expected.Id, actual.Id);
        if (expected.Value is MathGraph<string, string> expectedGraph &&
            actual.Value is MathGraph<string, string> actualGraph)
        {
            AssertGraphEqual(expectedGraph, actualGraph);
            return;
        }
        Assert.AreEqual(expected.Value, actual.Value);
    }

    private static void AssertEdgeEqual<TElementInfo, TEdgeInfo>(MathGraphEdge<TElementInfo, TEdgeInfo> expected,
        MathGraphEdge<TElementInfo, TEdgeInfo> actual)
    {
        Assert.AreEqual(expected.EdgeInfo, actual.EdgeInfo);
        Assert.AreEqual(expected.GetType(), actual.GetType());
        if (expected is MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo> expectedBidirectional &&
            actual is MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo> actualBidirectional)
        {
            AssertElementEqual(expectedBidirectional.AElement, actualBidirectional.AElement);
            AssertElementEqual(expectedBidirectional.BElement, actualBidirectional.BElement);
        }
        else if (expected is MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> expectedUnidirectional &&
                 actual is MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> actualUnidirectional)
        {
            AssertElementEqual(expectedUnidirectional.From, actualUnidirectional.From);
            AssertElementEqual(expectedUnidirectional.To, actualUnidirectional.To);
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    private sealed class SuperPointDeserializationContext : IDeserializationContext
    {
        public bool TryDeserialize(string value, string? type, [NotNullWhen(true)] out object? result)
        {
            result = null;
            if (type == typeof(MathGraph<string, string>).FullName)
            {
                var graph = new MathGraph<string, string>();
                graph.GetSerializer(this).Deserialize(value);
                result = graph;
                return true;
            }
            return false;
        }
    }
}
