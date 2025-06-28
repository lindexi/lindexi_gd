using MathGraphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphsTests;

[TestClass()]
public class MathGraphTests
{
    [TestMethod()]
    public void CreateAndAddElementTest()
    {
        var graph = new MathGraph<string, int>();
        MathGraphElement<string, int> element = graph.CreateAndAddElement("A");
        Assert.IsNotNull(element);
        Assert.AreEqual("A", element.Value);
        Assert.AreEqual(1, graph.ElementList.Count);
        Assert.AreSame(element, graph.ElementList[0]);
    }

    [TestMethod()]
    public void AddEdgeTest()
    {
        var graph = new MathGraph<string, int>();
        MathGraphElement<string, int> a = graph.CreateAndAddElement("A");
        MathGraphElement<string, int> b = graph.CreateAndAddElement("B");
        graph.AddEdge(a, b, 42);

        // 检查A的出度包含B
        Assert.IsTrue(a.OutElementList.Contains(b));
        // 检查B的入度包含A
        Assert.IsTrue(b.InElementList.Contains(a));
        // 检查A的边信息
        Assert.IsTrue(a.EdgeList.Any(e => e.EdgeInfo == 42 && e.GetOtherElement(a) == b));
    }

    [TestMethod()]
    public void AddBidirectionalEdgeTest()
    {
        var graph = new MathGraph<string, int>();
        var a = graph.CreateAndAddElement("A");
        var b = graph.CreateAndAddElement("B");
        graph.AddBidirectionalEdge(a, b, 99);

        // 检查A和B互为出入度
        Assert.IsTrue(a.OutElementList.Contains(b));
        Assert.IsTrue(b.OutElementList.Contains(a));
        Assert.IsTrue(a.InElementList.Contains(b));
        Assert.IsTrue(b.InElementList.Contains(a));
        // 检查A的边信息
        Assert.IsTrue(a.EdgeList.Any(e => e.EdgeInfo == 99));
    }

    [TestMethod()]
    public void GetSerializerTest()
    {
        var graph = new MathGraph<string, int>();
        var serializer = graph.GetSerializer();
        Assert.IsNotNull(serializer);
        // 可进一步测试序列化结果
        var serialized = serializer.Serialize();
        Assert.IsInstanceOfType(serialized, typeof(string));
    }
}