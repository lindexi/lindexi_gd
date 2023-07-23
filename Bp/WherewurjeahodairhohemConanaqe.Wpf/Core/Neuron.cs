using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WherewurjeahodairhohemConanaqe.Wpf.Core;

public class Runner
{
    /// <summary>
    /// 执行次数
    /// </summary>
    public ulong RunCount { get; private set; }
}

class NeuronManager
{


    public Neuron CreateNeuron()
    {
        var count = Interlocked.Increment(ref _neuronCount);
        return new Neuron(new NeuronId(count));
    }

    private ulong _neuronCount;
}

internal class Neuron
{
    public Neuron(NeuronId id)
    {
        Id = id;
    }

    public NeuronId Id { get; }
    public InputManager InputManager { get; } = new InputManager();
    public OutputArgument OutputArgument { get; private set; }

    /// <summary>
    /// 执行内容
    /// </summary>
    public virtual void Run()
    {

    }
}

/// <summary>
/// 输入管理
/// </summary>
/// 允许接收多个输入，处理多线程执行的问题
class InputManager
{
    private List<InputArgument> InputArgumentList { get; } = new List<InputArgument>();
}

/// <summary>
/// 进行输入的参数
/// </summary>
/// <param name="Value"></param>
public readonly record struct InputArgument(double Value)
{
}

public readonly record struct OutputArgument(double Value);

public readonly record struct NeuronId(ulong Value);