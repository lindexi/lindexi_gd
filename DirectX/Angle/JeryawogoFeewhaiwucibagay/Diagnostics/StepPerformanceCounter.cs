using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JeryawogoFeewhaiwucibagay.Diagnostics;

public class StepPerformanceCounter
{
    public StepPerformanceCounter(string name)
    {
        RootName = name;
    }

    private string RootName { get; }

    public static readonly StepPerformanceCounter RenderThreadCounter = new StepPerformanceCounter("Render");

    public StepStartContext StepStart(string name, bool enable = true, bool isMainStep = false)
    {
        var counterName = $"{RootName}.{name}";

        if (!_dictionary.TryGetValue(counterName, out var counter))
        {
            counter = new PerformanceCounter(name, enable)
            {
                AutoOutput = true,
            };
            _dictionary[counterName] = counter;
        }

        if (enable)
        {
            counter.StepStart();
        }

        return new StepStartContext(counter, this)
        {
            Enable = enable,
            //IsMainStep = isMainStep,
        };
    }

    private readonly Dictionary<string, PerformanceCounter> _dictionary = [];

    public void StepStop(string name)
    {
        var counterName = $"{RootName}.{name}";
        _dictionary[counterName].StepStop();
    }
}

public readonly record struct StepStartContext(PerformanceCounter Counter, StepPerformanceCounter Total) : IDisposable
{
    internal bool Enable { get; init; }

    //internal bool IsMainStep { get; init; }

    public void Dispose()
    {
        if (!Enable)
        {
            return;
        }

        Counter.StepStop();
    }
}

public class PerformanceCounter(string name, bool enable = true)
{
    public bool AutoOutput { get; init; }

    public void StepStart()
    {
        _stopwatch.Restart();
        _count++;
    }

    public void StepStop()
    {
        _stopwatch.Stop();
        _total += _stopwatch.Elapsed.TotalMilliseconds;

        if (!enable)
        {
            return;
        }

        if (!AutoOutput)
        {
            return;
        }

        if (CanOutput)
        {
            Output();
        }
    }

    internal bool CanOutput => _count > 100 && _total > 1000;

    public void Output()
    {
        var ave = _total / _count;

        Console.WriteLine($"[{name}] 平均毫秒： {ave}");

        _count = 0;
        _total = 0;
    }

    private Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _total;
    private int _count;
}
