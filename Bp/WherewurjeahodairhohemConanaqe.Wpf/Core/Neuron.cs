using Bp;

using Microsoft.Msagl.Core.Geometry.Curves;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using WherewurjeahodairhohemConanaqe.Wpf.Core.Serialization;

namespace WherewurjeahodairhohemConanaqe.Wpf.Core;

public class Runner
{
    public Runner(NeuronManager neuronManager)
    {
        NeuronManager = neuronManager;
    }

    public NeuronManager NeuronManager { get; }

    public void Run()
    {
        RunCount++;

        foreach (var neuron in NeuronManager.NeuronList)
        {
            if (neuron == NeuronManager.OutputNeuron)
            {

            }

            RunNeuron(neuron);
        }
    }

    private void RunNeuron(Neuron neuron)
    {
        neuron.Run();

        var inputArgument = neuron.OutputArgument.ToInput();

        foreach (var outputNeuron in neuron.GetOutputNeuron())
        {
            outputNeuron.InputManager.SetInput(neuron.Id, inputArgument);
        }
    }

    /// <summary>
    /// 执行次数
    /// </summary>
    public ulong RunCount { get; private set; }
}

public class NeuronManager : INeuronSerialization
{
    public NeuronManager()
    {
        var inputNeuron = new InputNeuron(this);
        NeuronList.Add(inputNeuron);
        var neuron1 = CreateNeuron();
        NeuronList.Add(neuron1);
        var neuron2 = CreateNeuron();
        NeuronList.Add(neuron2);

        inputNeuron.AddOutputNeuron(neuron1);
        neuron1.AddOutputNeuron(neuron2);

        InputNeuron = inputNeuron;
        OutputNeuron = neuron2;
    }

    public InputNeuron InputNeuron { get; }

    public Neuron OutputNeuron { get; }

    public List<Neuron> NeuronList { get; } = new List<Neuron>();

    public Neuron CreateNeuron()
    {
        var count = Interlocked.Increment(ref _neuronCount);
        return new Neuron(new NeuronId(count), this);
    }

    public void Learning(double learningRate)
    {
        foreach (var neuron in NeuronList)
        {
            neuron.Learning(learningRate);
        }
    }

    private ulong _neuronCount = 0;

    public void Serialize(SerializeContext context)
    {
    }

    public void Deserialize(DeserializeContext context)
    {
    }
}

public class InputNeuron : Neuron
{
    public InputNeuron(NeuronManager neuronManager) : base(new NeuronId(0), neuronManager)
    {

    }

    public void SetInput(InputArgument input)
    {
        InputManager.SetInput(Id, input);
    }

    protected override OutputArgument RunCore(Span<InputArgument> inputList)
    {
        // 先使用任意的输入方式
        if (inputList.Length > 0)
        {
            return new OutputArgument(inputList[0].Value);
        }
        return new OutputArgument(new double[] { Random.Shared.NextDouble() });
    }
}

/// <summary>
/// 执行状态
/// </summary>
public enum RunStatus
{
    Running,
}

public class Neuron
{
    public Neuron(NeuronId id, NeuronManager manager)
    {
        Id = id;
        Manager = manager;
    }

    public NeuronId Id { get; }
    public NeuronManager Manager { get; }
    public NeuronLayerIndex LayerIndex { get; private set; } = new NeuronLayerIndex(0);

    public InputManager InputManager { get; } = new InputManager();
    public OutputArgument OutputArgument { get; protected set; }
    public bool Running { private set; get; }
    /// <summary>
    /// 运行次数
    /// </summary>
    public ulong RunCount { get; protected set; }

    /// <summary>
    /// 输出列表
    /// </summary>
    private List<Neuron> OutputNeuronList { get; } = new List<Neuron>();

    /// <summary>
    /// 包含的列表
    /// </summary>
    public List<Neuron> IncludeNeuronList { get; } = new List<Neuron>();

    public void AddOutputNeuron(Neuron outputNeuron)
    {
        lock (OutputNeuronList)
        {
            OutputNeuronList.Add(outputNeuron);
        }
    }

    public List<Neuron> GetOutputNeuron()
    {
        lock (OutputNeuronList)
        {
            return OutputNeuronList.ToList();
        }
    }

    /// <summary>
    /// 执行
    /// </summary>
    public void Run()
    {
        Running = true;
        RunCount++;

        // 实现最简单的方式，可以替换为不同的方式
        using var input = InputManager.GetInput();
        Span<InputArgument> inputList = input.AsSpan();

        var outputArgument = RunCore(inputList);
        OutputArgument = outputArgument;

        Running = false;
    }

    /// <summary>
    /// 执行内容
    /// </summary>
    protected virtual OutputArgument RunCore(Span<InputArgument> inputList)
    {
        if (IncludeNeuronList.Count == 0)
        {
            return Function.RunCore(inputList);
        }

        var outputList = new List<double>();
        foreach (var inputArgument in inputList)
        {
            double sum = 0;
            foreach (var value in inputArgument.Value)
            {
                sum += value;
            }
            var output = sum / inputList.Length;

            outputList.Add(output);
        }

        return new OutputArgument(outputList);
    }

    public void Learning(double learningRate)
    {
        Function.Learning(learningRate);
    }

    public IFunction Function { get; } = new ThresholdFunction();
}

/// <summary>
/// 阈值函数
/// </summary>
public class ThresholdFunction : IFunction
{
    public OutputArgument RunCore(Span<InputArgument> inputList)
    {
        double result = 0d;
        foreach (var inputArgument in inputList)
        {
            MaxInputLength = Math.Max(MaxInputLength, inputArgument.Value.Count);
            var sum = 0d;
            for (var i = 0; i < Weights.Count && i < inputArgument.Value.Count; i++)
            {
                sum += inputArgument.Value[i] * Weights[i];
            }

            result += sum;
        }

        result -= Value;

        result = result > 0 ? 1 : 0;
        return new OutputArgument(new double[] { result });
    }

    private int MaxInputLength { set; get; } = 0;

    /// <summary>
    /// 权重
    /// </summary>
    public List<double> Weights { get; } = new List<double>();

    public double Value { set; get; }

    public void Learning(double learningRate)
    {
        Weights.Clear();
        for (int i = 0; i < MaxInputLength; i++)
        {
            Weights.Add(Random.Shared.NextDouble() * 2 - 1);
        }

        Value = Random.Shared.NextDouble() * 2 - 1;
    }
}

public interface IFunction
{
    OutputArgument RunCore(Span<InputArgument> inputList);
    void Learning(double learningRate);
}

/// <summary>
/// 输入管理
/// </summary>
/// 允许接收多个输入，处理多线程执行的问题
public class InputManager
{
    public void SetInput(NeuronId id, InputArgument argument)
    {
        lock (Locker)
        {
            InputArgumentList[id] = argument;
        }
    }

    public InputWithArrayPool GetInput()
    {
        lock (Locker)
        {
            var count = InputArgumentList.Count;

            var inputList = ArrayPool<InputArgument>.Shared.Rent(count);

            int i = 0;
            foreach (var keyValuePair in InputArgumentList)
            {
                inputList[i] = keyValuePair.Value;

                i++;
            }

            return new InputWithArrayPool(inputList, count, ArrayPool<InputArgument>.Shared);
        }
    }

    private Dictionary<NeuronId, InputArgument> InputArgumentList { get; } = new Dictionary<NeuronId, InputArgument>();

    private object Locker => InputArgumentList;
}

/// <summary>
/// 输入的内容，用于归还数组池。实际应该调用 <see cref="AsSpan"/> 方法使用
/// </summary>
/// <param name="InputList"></param>
/// <param name="Length"></param>
/// <param name="ArrayPool"></param>
public readonly record struct InputWithArrayPool(InputArgument[] InputList, int Length, ArrayPool<InputArgument> ArrayPool) : IDisposable
{
    public Span<InputArgument> AsSpan() => new Span<InputArgument>(InputList, 0, Length);

    public void Dispose()
    {
        ArrayPool.Return(InputList);
    }
}

/// <summary>
/// 进行输入的参数
/// </summary>
/// <param name="Value"></param>
public readonly record struct InputArgument(IReadOnlyList<double> Value)
{
}

public readonly record struct OutputArgument(IReadOnlyList<double> Value)
{
    public InputArgument ToInput() => new InputArgument(Value);
}

public readonly record struct NeuronId(ulong Value);

/// <summary>
/// 表示多少层
/// </summary>
/// <param name="Value"></param>
public readonly record struct NeuronLayerIndex(ulong Value);