using System.Diagnostics;
using System.Text;

namespace UnoInk.Inking.InkCore.Diagnostics;

/// <summary>
/// 线性步骤记录器
/// </summary>
class StepCounter
{
    /// <summary>
    /// 开始
    /// </summary>
    /// 开始和记录分离，开始不一定是某个步骤。这样业务方修改开始对应的步骤时，可以能够更好的被约束，明确一个开始的时机
    public void Start()
    {
        Stopwatch.Restart();
        IsStart = true;
    }

    public void Restart()
    {
        IsStart = true;
        StepDictionary.Clear();
        Stopwatch.Restart();
    }

    public Stopwatch Stopwatch => _stopwatch ??= new Stopwatch();
    private Stopwatch? _stopwatch;

    /// <summary>
    /// 记录某个步骤。默认就是一个步骤将会延续到下个步骤，两个步骤之间的耗时就是步骤耗时
    /// 实在不行，那你就加上 “Xx开始” 和 “Xx结束”好了
    /// </summary>
    /// <param name="step"></param>
    public void Record(string step)
    {
        if (!IsStart)
        {
            return;
        }

        Stopwatch.Stop();
        StepDictionary[step] = Stopwatch.ElapsedTicks;
        Stopwatch.Restart();
    }

    public void OutputToConsole()
    {
        if (!IsStart)
        {
            return;
        }
        Console.WriteLine(BuildStepResult());
    }

    /// <summary>
    /// 进行耗时对比，用于对比两个模块或者两个版本的各个步骤的耗时差
    /// </summary>
    /// <param name="other"></param>
    public void CompareToConsole(StepCounter other)
    {
        if (!IsStart)
        {
            return;
        }
        Console.WriteLine(Compare(other));
    }

    public string Compare(StepCounter other)
    {
        if (!IsStart)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        foreach (var (step, tick) in StepDictionary)
        {
            if (other.StepDictionary.TryGetValue(step, out var otherTick))
            {
                var sign = tick > otherTick ? "+" : "";
                stringBuilder.AppendLine($"{step} {TickToMillisecond(tick):0.000}ms {TickToMillisecond(otherTick):0.000}ms {sign}{TickToMillisecond(tick - otherTick):0.000}ms");
            }
            else
            {
                stringBuilder.AppendLine($"{step} {tick * 1000d / Stopwatch.Frequency}ms");
            }
        }
        return stringBuilder.ToString();
    }

    public string BuildStepResult()
    {
        if (!IsStart)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        foreach (var (step, tick) in StepDictionary)
        {
            stringBuilder.AppendLine($"{step} {TickToMillisecond(tick)}ms");
        }
        return stringBuilder.ToString();
    }

    public Dictionary<string /*Step*/, long /*耗时*/> StepDictionary => _stepDictionary ??= new Dictionary<string, long>();
    private Dictionary<string, long>? _stepDictionary;

    /// <summary>
    /// 是否开始，如果没有开始则啥都不做，用于性能优化，方便一次性注释决定是否测试性能
    /// </summary>
    public bool IsStart { get; private set; }

    private const double SecondToMillisecond = 1000d;
    private static double TickToMillisecond(long tick) => tick * SecondToMillisecond / Stopwatch.Frequency;
}
