using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class LayoutCancellationTests
{
    [TestMethod]
    public void CancelledFractalizationPreservesEntireCheckpoint()
    {
        var model = Create();
        var before = model.ExportCheckpoint();
        using var cancellation = Cancelled();
        Assert.ThrowsException<OperationCanceledException>(() => model.Fractalize("n", cancellationToken: cancellation.Token));
        CollectionAssert.AreEqual(before, model.ExportCheckpoint());
    }

    [TestMethod]
    public void CancelledFractalizationDoesNotPublishMeasurement()
    {
        var model = Create();
        using var cancellation = Cancelled();
        Assert.ThrowsException<OperationCanceledException>(() => model.Fractalize("n", cancellationToken: cancellation.Token));
        Assert.IsNull(model.LastStructureMeasurement);
    }

    [TestMethod]
    public void CancelledSnapshotIsRejectedBeforeGraphValidation()
    {
        var model = Create();
        var node = model.Graph.ElementList[0];
        model.Graph.AddEdge(node, node, new NeuralConnection(new ScalarParameter(1)));
        using var cancellation = Cancelled();
        Assert.ThrowsException<OperationCanceledException>(() => model.CreateExecutor(cancellation.Token));
    }

    [TestMethod]
    public void CancelledTokenSnapshotDoesNotModifyModel()
    {
        var model = Create();
        var before = model.ExportCheckpoint();
        using var cancellation = Cancelled();
        Assert.ThrowsException<OperationCanceledException>(() => model.CreateTokenExecutor(cancellation.Token));
        CollectionAssert.AreEqual(before, model.ExportCheckpoint());
    }

    [TestMethod]
    public void CancelledNodeObservationIsRejected()
    {
        using var cancellation = Cancelled();
        Assert.ThrowsException<OperationCanceledException>(() => Create().GetNodeObservations(cancellation.Token));
    }

    [TestMethod]
    public void EmptyInterpreterWindowStillHonorsCancellation()
    {
        using var cancellation = Cancelled();
        Assert.ThrowsException<OperationCanceledException>(() => GraphInterpreter.Run(Create(),
            Array.Empty<IReadOnlyDictionary<string, double>>(), cancellation.Token));
    }

    [TestMethod]
    public void ActiveTokenPreservesFractalSemantics()
    {
        var model = Create();
        using var cancellation = new CancellationTokenSource();
        model.Fractalize("n", cancellationToken: cancellation.Token);
        Assert.AreEqual(6, model.GetNodeObservations(cancellation.Token).Count);
    }

    private static CancellationTokenSource Cancelled()
    {
        var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        return cancellation;
    }

    private static RecursiveNetwork Create()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        var model = new RecursiveNetwork(graph);
        model.BindText(new TokenBindings("test", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }));
        return model;
    }
}
