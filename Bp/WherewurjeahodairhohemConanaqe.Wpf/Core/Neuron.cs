using System;
using System.Buffers;
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
public readonly record struct InputArgument(double Value)
{
}

public readonly record struct OutputArgument(double Value);

public readonly record struct NeuronId(ulong Value);