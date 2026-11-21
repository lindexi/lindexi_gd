using FractalForge.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Core.Tests;

[TestClass]
public class ForgeTopologyTests
{
    [TestMethod]
    public void DiamondHasFourActualNodes() => Assert.AreEqual(4, ForgeTopology.CreateDiamond().Graph.ElementList.Count);

    [TestMethod]
    public void FeedforwardDiamondHasFourEdges() => Assert.AreEqual(4, ForgeTopology.CreateDiamond(feedback: false).Expand().Count);

    [TestMethod]
    public void FeedbackHasExplicitOneStepDelay() => Assert.AreEqual(1, ForgeTopology.CreateDiamond().Expand().Single(e => e.From == "O" && e.To == "I").Delay);

    [TestMethod]
    public void TransparentBoundaryPreservesIndependentSources()
    {
        var topology = new ForgeTopology(new Dictionary<string, ForgeNode>
        {
            ["A"] = new(ForgeRole.Compute), ["B"] = new(ForgeRole.Compute),
            ["O"] = new(ForgeRole.OutputBoundary), ["C"] = new(ForgeRole.Compute)
        }, new[] { "A", "B", "C" }.ToDictionary(id => id, _ => new ForgeBias(Guid.NewGuid())),
        [new(Guid.NewGuid(), ["A", "O", "C"], Guid.NewGuid(), .3), new(Guid.NewGuid(), ["B", "O", "C"], Guid.NewGuid(), .7)]);
        CollectionAssert.AreEquivalent(new[] { "A", "B" }, topology.Expand().Select(e => e.From).ToArray());
    }

    [TestMethod]
    public void TransparentEndpointCannotSilentlyAggregate() => Assert.ThrowsException<InvalidDataException>(() => new ForgeTopology(
        new Dictionary<string, ForgeNode> { ["I"] = new(ForgeRole.InputBoundary), ["A"] = new(ForgeRole.Compute) },
        new Dictionary<string, ForgeBias> { ["A"] = new(Guid.NewGuid()) }, [new(Guid.NewGuid(), ["I", "A"], Guid.NewGuid(), 1)]));
}
