namespace MathGraph.ReversibleFractal.Research.NodeOperators;

/// <summary>Elementwise activation applied after each affine row.</summary>
public enum Activation { Identity, Tanh }

/// <summary>An ordered affine map followed by an elementwise activation.</summary>
public sealed class AffineOperator
{
    /// <summary>Creates an operator with positive input and output arity.</summary>
    public AffineOperator(int inputCount, int outputCount, Activation activation)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inputCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outputCount);
        if (activation is not (Activation.Identity or Activation.Tanh))
            throw new ArgumentOutOfRangeException(nameof(activation));

        InputCount = inputCount;
        OutputCount = outputCount;
        ParameterCount = checked(outputCount * (inputCount + 1));
        Activation = activation;
    }

    /// <summary>Number of ordered input ports.</summary>
    public int InputCount { get; }
    /// <summary>Number of ordered output ports.</summary>
    public int OutputCount { get; }
    /// <summary>Each output row stores one weight per input, then its bias.</summary>
    public int ParameterCount { get; }
    /// <summary>The activation used for all output rows.</summary>
    public Activation Activation { get; }

    internal void Forward(int[] inputs, int firstOutput, double[] values, ReadOnlySpan<double> parameters)
    {
        for (int j = 0; j < OutputCount; j++)
        {
            int row = j * (InputCount + 1);
            double z = parameters[row + InputCount];
            for (int i = 0; i < InputCount; i++)
                z += parameters[row + i] * values[inputs[i]];
            values[firstOutput + j] = Activation == Activation.Tanh ? Math.Tanh(z) : z;
        }
    }

    internal void Backward(int[] inputs, int firstOutput, double[] values, double[] adjoints,
        ReadOnlySpan<double> parameters, Span<double> parameterGradient)
    {
        for (int j = 0; j < OutputCount; j++)
        {
            int row = j * (InputCount + 1);
            double y = values[firstOutput + j];
            double derivative = Activation == Activation.Tanh ? 1 - y * y : 1;
            double delta = adjoints[firstOutput + j] * derivative;
            parameterGradient[row + InputCount] = delta;
            for (int i = 0; i < InputCount; i++)
            {
                parameterGradient[row + i] = delta * values[inputs[i]];
                adjoints[inputs[i]] += delta * parameters[row + i];
            }
        }
    }
}
