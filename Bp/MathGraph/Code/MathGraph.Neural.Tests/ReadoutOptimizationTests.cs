using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class ReadoutOptimizationTests
{
    [DataTestMethod]
    [DataRow(1, false, 7, 5)]
    [DataRow(3, false, 7, 5)]
    [DataRow(3, true, 7, 5)]
    [DataRow(1, false, 17, 1)]
    [DataRow(2, true, 17, 3)]
    public void ReadoutGradientsMatchDictionaryReference(int internalSteps, bool masked, int vocabularySize, int readoutCount)
    {
        var five = FiveNodeGraph.Create(17);
        var model = new RecursiveNetwork(five.Graph);
        var nodes = five.Graph.ElementList.Take(readoutCount).Select(node => node.Id).ToArray();
        var encoding = Enumerable.Range(0, vocabularySize).Select(token => (IReadOnlyList<double>)
            Enumerable.Range(0, nodes.Length).Select(bit => ((token >> bit) & 1) == 0 ? -1d : 1d).ToArray()).ToArray();
        var bindings = new TokenBindings("reference", nodes, nodes, encoding, internalSteps, seed: 19);
        model.BindText(bindings);
        var tokens = new[] { 0, 3, 1, 6, 2 };
        var labels = new[] { false, true, !masked, true, !masked };
        var labelCount = labels.Skip(1).Count(value => value);
        var parameters = bindings.Parameters.ToArray();
        var gradients = parameters.ToDictionary(parameter => parameter.Id, _ => 0d);
        var predictions = model.CreateTokenExecutor().Run(tokens[..^1]).Predictions;
        var inputs = tokens[..^1].SelectMany(token => Enumerable.Repeat(
            (IReadOnlyDictionary<string, double>)nodes.Select((id, r) => (id, value: encoding[token][r]))
                .ToDictionary(pair => pair.id, pair => pair.value), internalSteps)).ToArray();
        var outputs = model.CreateExecutor().Run(inputs).Outputs;
        foreach (var t in Enumerable.Range(0, predictions.Count).Where(t => labels[t + 1]))
        {
            var logits = predictions[t].Logits;
            var maximum = logits.Max();
            var exponentials = logits.Select(value => Math.Exp(value - maximum)).ToArray();
            var total = exponentials.Sum();
            var q = (t + 1) * internalSteps - 1;
            for (var c = 0; c < bindings.VocabularySize; c++)
            {
                var gradient = (exponentials[c] / total - (c == tokens[t + 1] ? 1 : 0)) / labelCount;
                gradients[parameters[bindings.VocabularySize * nodes.Length + c].Id] += gradient;
                for (var r = 0; r < nodes.Length; r++)
                    gradients[parameters[c * nodes.Length + r].Id] += gradient * outputs[q][nodes[r]];
            }
        }
        var expected = parameters.Select(parameter => gradients[parameter.Id] * 0.5).ToArray();

        model.TrainTokens(tokens, options: new AdamWOptions(Beta1: 0.5), labelMask: labels);

        CollectionAssert.AreEqual(expected, parameters.Select(parameter => parameter.FirstMoment).ToArray());
    }
}
