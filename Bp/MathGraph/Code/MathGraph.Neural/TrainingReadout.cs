using System.Numerics;

namespace MathGraphs.Neural;

internal sealed class TrainingReadout
{
    private readonly double[][] _weights;
    private readonly double[] _biases;

    internal TrainingReadout(TokenBindings bindings, CancellationToken cancellationToken)
    {
        _weights = new double[bindings.ReadoutNodes.Count][];
        _biases = new double[bindings.VocabularySize];
        for (var r = 0; r < _weights.Length; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var column = _weights[r] = new double[bindings.VocabularySize];
            for (var c = 0; c < column.Length; c++) column[c] = bindings.Weights[c][r].Value;
        }
        for (var c = 0; c < _biases.Length; c++) _biases[c] = bindings.Biases[c].Value;
    }

    internal void Forward(double[] readout, double[] logits, int position, CancellationToken cancellationToken)
    {
        _biases.CopyTo(logits, 0);
        for (var r = 0; r < _weights.Length; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddScaled(logits, _weights[r], readout[r]);
        }
        for (var c = 0; c < logits.Length; c++)
            if (!double.IsFinite(logits[c])) Numeric.RequireFinite(logits[c], $"token={position}, class={c}");
    }

    internal static void NormalizeGradients(double[] gradients, double total, int target, int labelCount,
        double[] biasGradients)
    {
        var targetGradient = (gradients[target] / total - 1) / labelCount;
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            var denominator = new Vector<double>(total);
            var labels = new Vector<double>(labelCount);
            for (; i <= gradients.Length - Vector<double>.Count; i += Vector<double>.Count)
                (new Vector<double>(gradients, i) / denominator / labels).CopyTo(gradients, i);
        }
        for (; i < gradients.Length; i++) gradients[i] = gradients[i] / total / labelCount;
        gradients[target] = targetGradient;
        i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            for (; i <= gradients.Length - Vector<double>.Count; i += Vector<double>.Count)
                (new Vector<double>(biasGradients, i) + new Vector<double>(gradients, i)).CopyTo(biasGradients, i);
        }
        for (; i < gradients.Length; i++) biasGradients[i] += gradients[i];
    }

    internal void Backward(double[] gradients, double[] readout, double[][] weightGradients,
        double[] readoutGradients, CancellationToken cancellationToken)
    {
        for (var r = 0; r < _weights.Length; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddScaled(weightGradients[r], gradients, readout[r]);
            // Keep the class reduction order identical to the scalar reference.
            double sum = 0;
            var weights = _weights[r];
            for (var c = 0; c < gradients.Length; c++) sum += gradients[c] * weights[c];
            readoutGradients[r] = sum;
        }
    }

    private static void AddScaled(double[] destination, double[] source, double scale)
    {
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            var factor = new Vector<double>(scale);
            for (; i <= destination.Length - Vector<double>.Count; i += Vector<double>.Count)
            {
                var product = new Vector<double>(source, i) * factor;
                (new Vector<double>(destination, i) + product).CopyTo(destination, i);
            }
        }
        for (; i < destination.Length; i++) destination[i] += source[i] * scale;
    }
}
